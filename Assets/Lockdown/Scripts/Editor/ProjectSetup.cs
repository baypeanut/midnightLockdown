using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace Lockdown.EditorTools
{
    // one shot project configuration for quest
    public static class ProjectSetup
    {
        const string SettingsDir = "Assets/Lockdown/Settings";

        [MenuItem("Lockdown/Setup/Configure Project")]
        public static void Run()
        {
            EnsureFolder(SettingsDir);
            ConfigurePlayer();
            ConfigureRenderPipeline();
            ConfigureXr();
            ImportTmpEssentials();
            AssetDatabase.SaveAssets();
            Debug.Log("[setup] project configured");
        }

        static void ImportTmpEssentials()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return;
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui");
            if (info == null) { Debug.LogError("[setup] ugui package missing"); return; }
            string pkg = System.IO.Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(pkg, false);
            Debug.Log("[setup] imported tmp essentials");
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Night Shift Team";
            PlayerSettings.productName = "Midnight Lockdown";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.nightshift.midnightlockdown");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            QualitySettings.vSyncCount = 0;
        }

        static void ConfigureRenderPipeline()
        {
            string rendererPath = SettingsDir + "/QuestRenderer.asset";
            string assetPath = SettingsDir + "/QuestPipeline.asset";
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (!data)
            {
                data = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(data, rendererPath);
            }
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (!rp)
            {
                rp = UniversalRenderPipelineAsset.Create(data);
                AssetDatabase.CreateAsset(rp, assetPath);
            }
            var so = new SerializedObject(rp);
            Set(so, "m_MSAA", 4);
            Set(so, "m_SupportsHDR", false);
            Set(so, "m_RenderScale", 1f);
            Set(so, "m_MainLightShadowsSupported", true);
            Set(so, "m_ShadowDistance", 12f);
            Set(so, "m_ShadowCascadeCount", 1);
            Set(so, "m_SoftShadowsSupported", false);
            Set(so, "m_AdditionalLightsRenderingMode", 1);
            Set(so, "m_AdditionalLightsPerObjectLimit", 4);
            Set(so, "m_AdditionalLightShadowsSupported", false);
            so.ApplyModifiedPropertiesWithoutUndo();

            GraphicsSettings.defaultRenderPipeline = rp;
            int levels = QualitySettings.names.Length;
            for (int i = 0; i < levels; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = rp;
            }
            EditorUtility.SetDirty(rp);
        }

        static void ConfigureXr()
        {
            var perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(SettingsDir + "/XRGeneral.asset");
            if (!perTarget)
            {
                if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget existing))
                    perTarget = existing;
                else
                {
                    perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(perTarget, SettingsDir + "/XRGeneral.asset");
                }
            }
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);

            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                var general = perTarget.SettingsForBuildTarget(group);
                if (!general)
                {
                    general = ScriptableObject.CreateInstance<XRGeneralSettings>();
                    general.name = group + " Settings";
                    AssetDatabase.AddObjectToAsset(general, perTarget);
                    perTarget.SetSettingsForBuildTarget(group, general);
                }
                if (!general.Manager)
                {
                    var mgr = ScriptableObject.CreateInstance<XRManagerSettings>();
                    mgr.name = group + " Providers";
                    AssetDatabase.AddObjectToAsset(mgr, perTarget);
                    general.Manager = mgr;
                }
                general.InitManagerOnStart = true;
                XRPackageMetadataStore.AssignLoader(general.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);
                EditorUtility.SetDirty(general);
            }
            EditorUtility.SetDirty(perTarget);
            AssetDatabase.SaveAssets();

            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            var android = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (android == null)
            {
                Debug.LogError("[setup] openxr android settings missing");
                return;
            }
            Enable<MetaQuestFeature>(android);
            Enable<OculusTouchControllerProfile>(android);
            Enable<MetaQuestTouchPlusControllerProfile>(android);
            android.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            EditorUtility.SetDirty(android);

            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Standalone);
            var desktop = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            if (desktop != null)
            {
                Enable<OculusTouchControllerProfile>(desktop);
                EditorUtility.SetDirty(desktop);
            }
        }

        static void Enable<T>(OpenXRSettings s) where T : UnityEngine.XR.OpenXR.Features.OpenXRFeature
        {
            var f = s.GetFeature<T>();
            if (f) { f.enabled = true; EditorUtility.SetDirty(f); }
            else Debug.LogWarning("[setup] feature not found " + typeof(T).Name);
        }

        static void Set(SerializedObject so, string prop, object value)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogWarning("[setup] missing property " + prop); return; }
            switch (value)
            {
                case int i: if (p.propertyType == SerializedPropertyType.Enum) p.enumValueIndex = i; else p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case bool b: p.boolValue = b; break;
            }
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
