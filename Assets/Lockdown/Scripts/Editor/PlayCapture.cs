using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lockdown.EditorTools
{
    // plays both scenes and photographs the runtime ui, which only exists in play mode
    [InitializeOnLoad]
    public static class PlayCapture
    {
        static int step;
        static float waitUntil;
        static bool active;

        static PlayCapture()
        {
            if (SessionState.GetBool("ml.uicap", false)) Hook();
        }

        [MenuItem("Lockdown/Check/Capture Runtime UI")]
        public static void Run()
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            EditorSceneManager.OpenScene(MenuBuilder.ScenePath, OpenSceneMode.Single);
            SessionState.SetBool("ml.uicap", true);
            step = 0;
            Hook();
            EditorApplication.isPlaying = true;
        }

        static void Hook()
        {
            if (active) return;
            active = true;
            EditorApplication.update += Tick;
        }

        static void Wait(float s) => waitUntil = Time.realtimeSinceStartup + s;

        static Vector3 Head => PlayerRig.I.head.transform.position;
        static Vector3 Ahead(float d, float drop = 0f)
        {
            var h = PlayerRig.I.head.transform;
            return h.position + Vector3.ProjectOnPlane(h.forward, Vector3.up).normalized * d + Vector3.down * drop;
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || Time.realtimeSinceStartup < waitUntil) return;
            try
            {
                switch (step++)
                {
                    case 0: Wait(1.5f); break;
                    case 1:
                        ViewCapture.Snap("30_ui_menu", Head, Ahead(1.4f, 0.15f));
                        SceneManager.LoadScene(1);
                        Wait(2.5f);
                        break;
                    case 2:
                        ViewCapture.Snap("31_ui_hud", Head, Ahead(1.3f));
                        var left = PlayerRig.I.left;
                        left.simulatePose = true;
                        left.simLocalPosition = new Vector3(-0.08f, 1.38f, 0.38f);
                        Wait(0.5f);
                        break;
                    case 3:
                        ViewCapture.Snap("32_ui_wrist", Head, PlayerRig.I.left.transform.position + Vector3.up * 0.06f);
                        PlayerRig.I.left.simulatePose = false;
                        PauseMenu.I.Toggle();
                        Wait(0.5f);
                        break;
                    case 4:
                        ViewCapture.Snap("33_ui_pause", Head, Ahead(0.9f, 0.1f));
                        PauseMenu.I.Toggle();
                        // lights on and the laser running through all three mirrors
                        PowerGrid.I.PowerOn();
                        var em = Object.FindAnyObjectByType<LaserEmitter>();
                        foreach (var g in Object.FindObjectsByType<Grabbable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                            if (g.itemId == "battery") { g.Locked = false; em.batterySocket.Insert(g); }
                        var sol = new System.Collections.Generic.Dictionary<string, float> { { "MirrorA", 45f }, { "MirrorB", -135f }, { "MirrorC", 30f } };
                        foreach (var m in Object.FindObjectsByType<LaserMirror>(FindObjectsSortMode.None)) m.turntable.SetValue(sol[m.transform.parent.name], false);
                        Wait(2.5f);
                        break;
                    case 5:
                        if (Hud.I) Hud.I.gameObject.SetActive(false);
                        ViewCapture.Snap("35_laser", new Vector3(9.4f, 1.9f, 5.2f), new Vector3(4.4f, 1.0f, 3.4f));
                        ViewCapture.Snap("36_hall_lit", new Vector3(11.4f, 1.7f, 0.6f), new Vector3(3f, 1.2f, 7f));
                        ViewCapture.Snap("37_catwalk_lit", new Vector3(5.9f, 4.35f, 9.5f), new Vector3(10.5f, 2.9f, 7.5f));
                        ViewCapture.Map("38_map");
                        if (Hud.I) Hud.I.gameObject.SetActive(true);
                        // stand at the console so the end panel has to dodge it
                        PlayerRig.I.TeleportFeet(new Vector3(2.3f, 0f, 7.7f));
                        PlayerRig.I.Turn(90f);
                        // the shutter is gone by the time a real player gets here
                        Object.FindAnyObjectByType<DissolveShutter>().Dissolve();
                        foreach (PuzzleId p in System.Enum.GetValues(typeof(PuzzleId))) GameDirector.I.DebugSolve(p);
                        Wait(5f);
                        break;
                    case 6:
                        var end = FindPanel();
                        Vector3 target = end ? end.transform.position : Ahead(1f);
                        ViewCapture.Snap("34_ui_end", Head, target);
                        Debug.Log("[capture] end panel at " + target.ToString("F2") + " head " + Head.ToString("F2"));
                        break;
                    default:
                        Debug.Log("[capture] ui done");
                        EditorApplication.update -= Tick;
                        active = false;
                        SessionState.SetBool("ml.uicap", false);
                        EditorApplication.isPlaying = false;
                        EditorSettings.enterPlayModeOptionsEnabled = false;
                        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
                        if (Application.isBatchMode) EditorApplication.Exit(0);
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.Log("[capture] failed at step " + (step - 1) + ": " + e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        // the result panel root is the scene object named endscreen that has no component
        static GameObject FindPanel()
        {
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == "EndScreen" && !go.GetComponent<EndScreen>()) return go;
            return null;
        }
    }
}
