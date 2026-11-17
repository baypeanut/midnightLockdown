using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Lockdown.EditorTools
{
    // one entry point for the whole build: settings, ui materials, scenes, apk
    public static class Pipeline
    {
        public const string ApkPath = "Builds/MidnightLockdown.apk";

        [MenuItem("Lockdown/Build/Everything")]
        public static void BuildScenes()
        {
            ProjectSetup.Run();
            UiMaterials();
            MenuBuilder.Build();
            LevelBuilder.Build();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuBuilder.ScenePath, true),
                new EditorBuildSettingsScene(LevelBuilder.ScenePath, true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[build] scenes ready");
        }

        static void UiMaterials()
        {
            const string dir = "Assets/Lockdown/Resources/Ui";
            BuildKit.Flat("Panel", new Color(0.04f, 0.06f, 0.08f, 1f), false, dir);
            BuildKit.Flat("Button", new Color(0.08f, 0.2f, 0.25f, 1f), false, dir);
            BuildKit.Flat("ButtonHot", new Color(0.15f, 0.55f, 0.65f, 1f), false, dir);
            // hud draws over the world
            Overlay(dir + "/HudPanel.mat", () =>
            {
                var m = new Material(Shader.Find("Lockdown/ViewOverlay"));
                m.SetColor("_BaseColor", new Color(0.03f, 0.05f, 0.07f, 0.82f));
                m.renderQueue = 3995;
                return m;
            });
            Overlay(dir + "/HudText.mat", () =>
            {
                var m = new Material(TMPro.TMP_Settings.defaultFontAsset.material);
                m.shader = Shader.Find("TextMeshPro/Distance Field Overlay");
                return m;
            });
        }

        static void Overlay(string path, System.Func<Material> make)
        {
            var fresh = make();
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing)
            {
                existing.shader = fresh.shader;
                existing.CopyPropertiesFromMaterial(fresh);
                existing.renderQueue = fresh.renderQueue;
                EditorUtility.SetDirty(existing);
            }
            else AssetDatabase.CreateAsset(fresh, path);
        }

        [MenuItem("Lockdown/Build/Quest APK")]
        public static void BuildApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            Directory.CreateDirectory("Builds");
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { MenuBuilder.ScenePath, LevelBuilder.ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log("[build] apk result=" + s.result + " errors=" + s.totalErrors + " warnings=" + s.totalWarnings + " size=" + s.totalSize + " time=" + s.totalTime);
            if (s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        public static void BuildAll()
        {
            BuildScenes();
            BuildApk();
        }
    }
}
