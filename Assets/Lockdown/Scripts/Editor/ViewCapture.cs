using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lockdown.EditorTools
{
    // renders fixed viewpoints of the built scenes to png for quick visual checks
    public static class ViewCapture
    {
        struct Shot
        {
            public string name; public Vector3 eye, target; public bool dark;
            public Shot(string n, Vector3 e, Vector3 t, bool d = false) { name = n; eye = e; target = t; dark = d; }
        }

        static readonly Shot[] LevelShots =
        {
            new Shot("01_start_dark", new Vector3(8.6f, 1.6f, 4.2f), new Vector3(0f, 1.3f, 4f), true),
            new Shot("02_start_lit", new Vector3(8.6f, 1.6f, 4.2f), new Vector3(0f, 1.3f, 4f)),
            new Shot("03_overview", new Vector3(11.5f, 4.6f, 0.4f), new Vector3(4f, 0f, 7f)),
            new Shot("04_fusebox", new Vector3(1.2f, 1.5f, 4.0f), new Vector3(0.1f, 1.35f, 4.0f), true),
            new Shot("05_ladder", new Vector3(6.1f, 1.6f, 6.6f), new Vector3(6.1f, 2.6f, 9.5f)),
            new Shot("06_catwalk", new Vector3(6.4f, 4.3f, 9.4f), new Vector3(11f, 2.7f, 8f)),
            new Shot("07_office", new Vector3(8.6f, 1.6f, 6.6f), new Vector3(11f, 1.0f, 9.5f)),
            new Shot("08_clock", new Vector3(9.0f, 1.6f, 8.6f), new Vector3(9.0f, 1.65f, 9.9f)),
            new Shot("09_safe", new Vector3(10.5f, 1.3f, 7.4f), new Vector3(11.6f, 0.5f, 7.4f)),
            new Shot("10_laser", new Vector3(9.5f, 2.2f, 6.0f), new Vector3(5f, 1f, 3f)),
            new Shot("11_vault", new Vector3(3.5f, 1.7f, 7.4f), new Vector3(1.5f, 1.2f, 9.2f)),
            new Shot("12_shutter", new Vector3(2.5f, 1.6f, 4.8f), new Vector3(2.2f, 1.6f, 7f)),
        };

        [MenuItem("Lockdown/Capture Views")]
        public static void Run()
        {
            Directory.CreateDirectory("Captures");
            EditorSceneManager.OpenScene(LevelBuilder.ScenePath, OpenSceneMode.Single);
            HideRuntimeOnly();
            var grid = Object.FindAnyObjectByType<PowerGrid>();
            foreach (var s in LevelShots)
            {
                SetDark(grid, s.dark);
                Render(s);
            }
            EditorSceneManager.OpenScene(MenuBuilder.ScenePath, OpenSceneMode.Single);
            HideRuntimeOnly();
            Render(new Shot("20_menu", new Vector3(0f, 1.6f, -0.4f), new Vector3(0f, 1.6f, 2.5f)));
            Render(new Shot("21_menu_corner", new Vector3(-2.2f, 2.5f, -2.2f), new Vector3(0.5f, 0.8f, 1.5f)));
            Debug.Log("[capture] done");
        }

        static void HideRuntimeOnly()
        {
            var rig = Object.FindAnyObjectByType<PlayerRig>();
            if (rig) rig.gameObject.SetActive(false);
        }

        static void SetDark(PowerGrid g, bool dark)
        {
            if (!g) return;
            foreach (var l in g.mainLights) if (l) l.enabled = !dark;
            foreach (var l in g.emergencyLights) if (l) l.enabled = dark;
            RenderSettings.ambientLight = dark ? g.ambientDark : g.ambientLit;
        }

        public static void Snap(string name, Vector3 eye, Vector3 target)
        {
            Directory.CreateDirectory("Captures");
            Render(new Shot(name, eye, target));
        }

        // top down plan of the floor with the ceilings hidden
        public static void Map(string name)
        {
            var hidden = new System.Collections.Generic.List<GameObject>();
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                string n = go.name;
                bool roof = n == "Ceiling" || n == "OfficeCeiling" || n == "Hanger" || n == "Tube" || n == "HalonGas" || n.StartsWith("mounted_fluorescent") || n.StartsWith("Dust") || n.EndsWith("Dust") || n == "Hud";
                if (roof && go.activeSelf) { go.SetActive(false); hidden.Add(go); }
            }
            var camGo = new GameObject("MapCam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.6f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 30f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.06f);
            camGo.transform.position = new Vector3(6f, 12f, 4.6f);
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var rt = new RenderTexture(1400, 1260, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.aspect = 1400f / 1260f;
            cam.targetTexture = rt;
            cam.Render(); cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            Directory.CreateDirectory("Captures");
            File.WriteAllBytes("Captures/" + name + ".png", tex.EncodeToJPG(88));
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); Object.DestroyImmediate(camGo);
            foreach (var go in hidden) go.SetActive(true);
        }

        static void Render(Shot s)
        {
            var go = new GameObject("CaptureCam");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            go.transform.position = s.eye;
            go.transform.LookAt(s.target);
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes("Captures/" + s.name + ".png", tex.EncodeToJPG(85));
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(go);
        }
    }
}
