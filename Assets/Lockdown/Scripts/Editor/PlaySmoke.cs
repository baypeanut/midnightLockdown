using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lockdown.EditorTools
{
    // plays the level in the editor and walks through every puzzle with simulated actions
    [InitializeOnLoad]
    public static class PlaySmoke
    {
        static int step;
        static float waitUntil;
        static int passed, failed;
        static readonly List<string> errors = new List<string>();
        static bool active;
        static bool timeoutRun;

        static PlaySmoke()
        {
            if (SessionState.GetBool("ml.smoke", false)) Hook();
        }

        [MenuItem("Lockdown/Check/Play Smoke Test")]
        public static void Run()
        {
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
            EditorSceneManager.OpenScene(LevelBuilder.ScenePath, OpenSceneMode.Single);
            PlayerPrefs.DeleteAll();
            SessionState.SetBool("ml.smoke", true);
            step = 0; passed = 0; failed = 0; errors.Clear(); timeoutRun = false;
            Hook();
            EditorApplication.isPlaying = true;
        }

        static void Hook()
        {
            if (active) return;
            active = true;
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
        }

        static void OnLog(string msg, string trace, LogType type)
        {
            if ((type == LogType.Exception || type == LogType.Error) && !msg.Contains("[check]") && !trace.Contains("UnityEditor.Search")) errors.Add(msg);
        }

        static void Check(bool ok, string what)
        {
            if (ok) passed++; else failed++;
            Debug.Log("[check] " + (ok ? "PASS " : "FAIL ") + what);
        }

        static void Wait(float s) => waitUntil = Time.realtimeSinceStartup + s;

        static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>();

        static Grabbable Item(string id) => Object.FindObjectsByType<Grabbable>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(g => g.itemId == id);

        static void Drop(Grabbable g, ItemSocket s)
        {
            g.transform.position = s.transform.position;
            ItemSocket.TryCapture(g);
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || Time.realtimeSinceStartup < waitUntil) return;
            var gd = GameDirector.I;
            try
            {
                switch (step++)
                {
                    case 0: Wait(1.5f); break;
                    case 1:
                        Check(gd != null, "game director exists");
                        Check(PlayerRig.I != null, "player rig exists");
                        Check(gd.State == GameState.Briefing || gd.State == GameState.Playing, "starts in briefing or playing");
                        Check(Mathf.Abs(gd.Remaining - 600f) < 5f, "default timer is ten minutes");
                        Check(Hud.I != null && Narrator.I != null, "hud and narrator present");
                        break;
                    case 2:
                    {
                        var fb = Find<FuseBox>();
                        var k = gd.Secrets;
                        // wrong fuse first
                        int wrongRating = k.fuseSlots[1];
                        int before = gd.Mistakes;
                        Drop(Item("fuse" + wrongRating), fb.slots[0]);
                        Check(!fb.slots[0].Filled, "wrong rated fuse is rejected");
                        Check(gd.Mistakes == before + 1, "rejection counts a mistake");
                        fb.lever.DebugPull();
                        Check(!PowerGrid.I.On, "lever does nothing without fuses");
                        for (int i = 0; i < 3; i++) Drop(Item("fuse" + k.fuseSlots[i]), fb.slots[i]);
                        Wait(0.5f);
                        break;
                    }
                    case 3:
                    {
                        var fb = Find<FuseBox>();
                        Check(fb.slots.All(s => s.Filled), "all three fuses seated");
                        Check(fb.lever.Armed, "lever armed after fuses");
                        fb.lever.DebugPull();
                        Wait(2f);
                        break;
                    }
                    case 4:
                        Check(PowerGrid.I.On, "power restored");
                        Check(gd.IsSolved(PuzzleId.Power), "power puzzle solved");
                        Check(gd.CurrentStage == Stage.Catwalk, "stage moves to catwalk");
                        Check(Achievements.Has("lights"), "lights achievement unlocked");
                        gd.DebugVisitOffice();
                        Check(gd.CurrentStage == Stage.Office, "office stage after entering");
                        Find<ClockPuzzle>().DebugSet((gd.Secrets.clockHour + 1) % 12, gd.Secrets.clockMinute);
                        Wait(0.3f);
                        break;
                    case 5:
                    {
                        var cp = Find<ClockPuzzle>();
                        Check(!cp.Solved, "wrong clock time does not open");
                        cp.DebugSet(gd.Secrets.clockHour, gd.Secrets.clockMinute);
                        Wait(1.2f);
                        break;
                    }
                    case 6:
                    {
                        var cp = Find<ClockPuzzle>();
                        Check(cp.Solved, "correct clock time opens the face");
                        var card = Item("keycard");
                        Check(card && card.gameObject.activeInHierarchy && !card.Locked, "override card revealed");
                        var bat = Item("battery");
                        Check(bat.Locked, "battery locked before the safe opens");
                        // a reversal on the wrong number resets progress
                        var lockLogic = new SafeLock(new[] { 5, 20, 9 });
                        for (int n = 1; n <= 5; n++) lockLogic.Step(n, 1);
                        Check(lockLogic.Step(4, -1) == SafeLock.Result.Locked, "safe locks a number on reversal");
                        for (int n = 3; n >= 0; n--) lockLogic.Step(n, -1);
                        Check(lockLogic.Step(1, 1) == SafeLock.Result.Reset, "wrong reversal resets the safe");
                        Find<SafePuzzle>().DebugSolve();
                        Wait(2f);
                        break;
                    }
                    case 7:
                    {
                        Check(Find<SafePuzzle>().Opened, "safe opens with the real combination");
                        Check(gd.IsSolved(PuzzleId.Safe), "safe puzzle solved");
                        Check(gd.CurrentStage == Stage.Laser, "stage moves to laser");
                        var em = Find<LaserEmitter>();
                        var bat = Item("battery");
                        Check(!bat.Locked, "battery free after safe");
                        Drop(bat, em.batterySocket);
                        Wait(0.6f);
                        break;
                    }
                    case 8:
                    {
                        var em = Find<LaserEmitter>();
                        Check(em.Powered, "battery powers the emitter");
                        Check(!Find<LaserSensor>().Unlocked, "scrambled mirrors do not unlock");
                        var mirrors = Object.FindObjectsByType<LaserMirror>(FindObjectsSortMode.None).OrderBy(m => m.name).ToArray();
                        var solution = new Dictionary<string, float> { { "MirrorA", 45f }, { "MirrorB", -135f }, { "MirrorC", 45f } };
                        foreach (var m in mirrors) m.turntable.SetValue(solution[m.transform.parent.name], true);
                        Physics.SyncTransforms();
                        Wait(1.5f);
                        break;
                    }
                    case 9:
                    {
                        var em = Find<LaserEmitter>();
                        Check(em.LastBounceCount == 3, "beam bounces off all three mirrors (" + em.LastBounceCount + ")");
                        Check(Find<LaserSensor>().Unlocked, "sensor unlocks with the solved mirrors");
                        Check(gd.IsSolved(PuzzleId.Laser), "laser puzzle solved");
                        Wait(3f);
                        break;
                    }
                    case 10:
                    {
                        Check(Find<DissolveShutter>().Open, "vault shutter dissolved");
                        var con = Find<RebootConsole>();
                        Check(!con.rebootButton.Interactable, "reboot locked without card");
                        Drop(Item("keycard"), con.cardReader);
                        Wait(0.5f);
                        break;
                    }
                    case 11:
                    {
                        var con = Find<RebootConsole>();
                        Check(con.rebootButton.Interactable, "card enables reboot");
                        con.rebootButton.RayPress(null);
                        Wait(1f);
                        break;
                    }
                    case 12:
                    {
                        var con = Find<RebootConsole>();
                        con.rebootButton.RayRelease(null);
                        Check(gd.State != GameState.Won, "short press does not reboot");
                        con.rebootButton.RayPress(null);
                        Wait(3.4f);
                        break;
                    }
                    case 13:
                        Check(gd.State == GameState.Won, "holding reboot wins the game");
                        Check(gd.LastScore > 0, "score computed (" + gd.LastScore + ")");
                        Check(Leaderboard.Load().Count == 1, "run saved to leaderboard");
                        Check(Achievements.Has("escape") && Achievements.Has("unassisted"), "escape achievements unlocked");
                        Wait(4.5f);
                        break;
                    case 14:
                        Check(GameObject.Find("EndScreen") != null, "end screen shown");
                        // second scenario: run out of time
                        gd.Restart();
                        Wait(1.5f);
                        break;
                    case 15:
                        gd = GameDirector.I;
                        Check(gd != null && gd.SolvedCount == 0, "restart gives a fresh run");
                        string hint = gd.UseHint();
                        Check(!string.IsNullOrEmpty(hint) && gd.HintsUsed == 1, "hint costs and returns text");
                        gd.Engage();
                        gd.DebugSetRemaining(1f);
                        Wait(2f);
                        break;
                    case 16:
                        Check(gd.State == GameState.Lost, "timer reaching zero loses");
                        Wait(0.5f);
                        break;
                    default:
                        Finish();
                        break;
                }
            }
            catch (System.Exception e)
            {
                Check(false, "exception at step " + (step - 1) + ": " + e.Message);
                Finish();
            }
        }

        static void Finish()
        {
            Check(errors.Count == 0, "no runtime errors (" + errors.Count + ")" + (errors.Count > 0 ? " first: " + errors[0] : ""));
            Debug.Log("[check] smoke finished passed=" + passed + " failed=" + failed);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            active = false;
            SessionState.SetBool("ml.smoke", false);
            EditorApplication.isPlaying = false;
            // leave the editor on its normal play mode settings
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.None;
            PlayerPrefs.DeleteAll();
            if (Application.isBatchMode) EditorApplication.Exit(failed == 0 ? 0 : 1);
        }
    }
}
