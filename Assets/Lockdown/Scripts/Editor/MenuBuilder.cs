using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lockdown.EditorTools
{
    // security booth used as the main menu
    public static class MenuBuilder
    {
        public const string ScenePath = "Assets/Lockdown/Scenes/MainMenu.unity";

        [MenuItem("Lockdown/Build/Main Menu")]
        public static void Build()
        {
            BuildKit.Reset();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.1f, 0.12f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.03f);
            RenderSettings.fogStartDistance = 5f;
            RenderSettings.fogEndDistance = 20f;
            RenderSettings.skybox = null;

            var env = new GameObject("Booth").transform;
            BuildKit.Slab(env, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(5f, 0.1f, 5f), "Rubber004", 1f, 0.3f, 0f, true);
            BuildKit.Slab(env, "Ceiling", new Vector3(0f, 2.85f, 0f), new Vector3(5f, 0.1f, 5f), "OfficeCeiling001", 1.2f, 0.1f);
            BuildKit.Slab(env, "WallN", new Vector3(0f, 1.4f, 2.6f), new Vector3(5f, 2.8f, 0.2f), "Concrete034", 2f);
            BuildKit.Slab(env, "WallS", new Vector3(0f, 1.4f, -2.6f), new Vector3(5f, 2.8f, 0.2f), "Concrete034", 2f);
            BuildKit.Slab(env, "WallE", new Vector3(2.6f, 1.4f, 0f), new Vector3(0.2f, 2.8f, 5f), "Concrete034", 2f);
            BuildKit.Slab(env, "WallW", new Vector3(-2.6f, 1.4f, 0f), new Vector3(0.2f, 2.8f, 5f), "Concrete034", 2f);

            // monitor wall behind the menu
            var screen = BuildKit.Custom("warden_screen", "Lockdown/MonitorNoise", m => { m.SetColor("_Tint", new Color(1f, 0.22f, 0.15f)); m.SetFloat("_Static", 0.22f); });
            for (int i = -1; i <= 1; i++)
            {
                var s = BuildKit.Node(env, "Monitor", new Vector3(i * 1.25f, 2.05f, 2.47f));
                BuildKit.Box(s, "Bezel", new Vector3(0f, 0f, 0.02f), new Vector3(1.15f, 0.68f, 0.04f), BuildKit.Paint("bezel", new Color(0.05f, 0.05f, 0.06f), 0.6f), false);
                BuildKit.Prim(s, PrimitiveType.Quad, "Screen", new Vector3(0f, 0f, -0.001f), new Vector3(1.08f, 0.62f, 1f), screen);
            }
            BuildKit.Model(env, "metal_office_desk", new Vector3(0f, 0f, 1.9f), 180f);
            BuildKit.Model(env, "classic_laptop", new Vector3(-0.6f, 0.79f, 1.85f), 180f);
            BuildKit.Model(env, "desk_lamp_arm_01", new Vector3(0.7f, 0.79f, 2.1f), 160f, 0.8f, false);
            BuildKit.Model(env, "potted_plant_02", new Vector3(-2.1f, 0f, -2.1f), 0f);
            BuildKit.Model(env, "steel_frame_shelves_01", new Vector3(2.2f, 0f, -1.2f), -90f);
            var core = BuildKit.Prim(env, PrimitiveType.Sphere, "WardenCore", new Vector3(-1.7f, 1.6f, 1.6f), Vector3.one * 0.5f,
                BuildKit.Custom("warden_core", "Lockdown/Hologram", m => { m.SetColor("_Color", new Color(1f, 0.2f, 0.15f, 1f)); m.SetFloat("_Alpha", 0.8f); }));
            var sp = core.AddComponent<Spinner>();
            sp.bobHeight = 0.05f;
            BuildKit.Particles(env, "Dust", new Vector3(0f, 1.4f, 0f), BuildKit.Fx.Dust);

            var lights = new GameObject("Lights").transform;
            BuildKit.PointLight(lights, "Desk", new Vector3(0.6f, 1.6f, 1.6f), new Color(1f, 0.85f, 0.6f), 1.6f, 5f, true);
            BuildKit.PointLight(lights, "Red", new Vector3(-1.7f, 2.2f, 1.2f), new Color(1f, 0.15f, 0.1f), 1.2f, 5f);
            BuildKit.PointLight(lights, "Fill", new Vector3(0f, 2.5f, -1.5f), new Color(0.5f, 0.6f, 0.9f), 0.8f, 6f);

            var anchor = BuildKit.Node(env, "MenuAnchor", new Vector3(0f, 1.45f, 1.0f));
            var mc = anchor.gameObject.AddComponent<MenuController>();
            mc.anchor = anchor;
            Sfx.Loop(BuildKit.Node(env, "Hum", new Vector3(0f, 2f, 2f)).gameObject, "room_hum_loop", 0.2f, 8f);

            RigBuilder.Build(new Vector3(0f, 0f, -0.05f), 0f, false);
            ProjectSetup.EnsureFolder("Assets/Lockdown/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[build] menu saved " + ScenePath);
        }
    }
}
