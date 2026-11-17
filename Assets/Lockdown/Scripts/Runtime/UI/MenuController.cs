using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lockdown
{
    // main menu terminal in the security booth
    public class MenuController : MonoBehaviour
    {
        public Transform anchor;
        Transform page;
        TextMeshPro body;

        void Start()
        {
            UiFocus.Active = true;
            GameSettings.Apply();
            if (!anchor) anchor = transform;
            UiKit.Panel(anchor, new Vector2(1.2f, 0.82f), Vector3.zero, "MenuBg");
            UiKit.Text(anchor, "MIDNIGHT LOCKDOWN", new Vector3(0f, 0.34f, -0.002f), new Vector2(1.1f, 0.09f), 0.07f, TextAlignmentOptions.Center, UiKit.Warn);
            UiKit.Text(anchor, "Kestrel Dynamics  |  floor four  |  23:59", new Vector3(0f, 0.27f, -0.002f), new Vector2(1.1f, 0.04f), 0.022f, TextAlignmentOptions.Center, UiKit.Dim);
            float y = 0.17f;
            string[] names = { "Start shift", "How to play", "Leaderboard", "Achievements", "Settings", "Credits" };
            System.Action[] acts = { StartGame, ShowHowTo, ShowBoard, ShowAchievements, ShowSettings, ShowCredits };
            for (int i = 0; i < names.Length; i++)
            {
                int k = i;
                UiKit.Button(anchor, names[i], new Vector3(-0.38f, y, -0.006f), new Vector2(0.36f, 0.075f), () => acts[k](), 0.03f);
                y -= 0.095f;
            }
            page = new GameObject("Page").transform;
            page.SetParent(anchor, false);
            ShowHowTo();
        }

        void ClearPage()
        {
            UiKit.Clear(page);
            body = null;
        }

        TextMeshPro PageText(string text, float size = 0.022f)
        {
            body = UiKit.Text(page, text, new Vector3(0.2f, -0.03f, -0.002f), new Vector2(0.72f, 0.56f), size, TextAlignmentOptions.TopLeft);
            return body;
        }

        void StartGame()
        {
            UiFocus.Active = false;
            ScreenFade.FadeTo(1f, 0.4f);
            Invoke(nameof(LoadLevel), 0.45f);
        }

        void LoadLevel() => SceneManager.LoadScene(1);

        void ShowHowTo()
        {
            ClearPage();
            PageText(
                "<b>The story</b>\n" +
                "At midnight WARDEN, the building AI, locks floor four and flags you as the intruder. Fire suppression floods the floor when the clock hits zero.\n\n" +
                "<b>Controls</b>\n" +
                "Grip: grab, climb ladders, turn dials and levers\n" +
                "Trigger: press buttons with the pointer\n" +
                "Grip while pointing at an item: pull it to your hand\n" +
                "Right stick forward: teleport arc, left or right: snap turn\n" +
                "Left stick: walk (can be turned off)\n" +
                "Raise your left wrist: timer, objective, HINT, MENU\n" +
                "Y or B: pause menu\n\n" +
                "Teleport only works on the floor you stand on. Use the ladders to change level.", 0.019f);
        }

        void ShowBoard()
        {
            ClearPage();
            var list = Leaderboard.Load();
            var sb = new StringBuilder("<b>Top runs</b>\n\n");
            if (list.Count == 0) sb.Append("No escapes yet. Be the first.");
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                sb.AppendLine("#" + (i + 1) + "   " + e.score + " pts   " + Leaderboard.Format(e.seconds) + "   chips " + e.chips + "   hints " + e.hints + "   <color=#8AA>" + e.date + "</color>");
            }
            PageText(sb.ToString());
            UiKit.Button(page, "Clear leaderboard", new Vector3(0.2f, -0.33f, -0.006f), new Vector2(0.34f, 0.055f), () => { Leaderboard.Clear(); ShowBoard(); }, 0.02f);
        }

        void ShowAchievements()
        {
            ClearPage();
            var sb = new StringBuilder("<b>Achievements " + Achievements.Count + " / " + Achievements.All.Length + "</b>\n\n");
            foreach (var d in Achievements.All)
            {
                bool has = Achievements.Has(d.id);
                sb.AppendLine((has ? "<color=#FFD24D>[x] " : "<color=#667>[ ] ") + d.title + "</color>  <size=80%>" + d.description + "</size>");
            }
            PageText(sb.ToString(), 0.02f);
            UiKit.Button(page, "Reset achievements", new Vector3(0.2f, -0.33f, -0.006f), new Vector2(0.34f, 0.055f), () => { Achievements.ResetAll(); ShowAchievements(); }, 0.02f);
        }

        void ShowSettings()
        {
            ClearPage();
            UiKit.Text(page, "<b>Settings</b>", new Vector3(0.2f, 0.2f, -0.002f), new Vector2(0.72f, 0.05f), 0.026f, TextAlignmentOptions.Left);
            SettingsBlock.Build(page, new Vector3(0.2f, 0.13f, 0f), true);
        }

        void ShowCredits()
        {
            ClearPage();
            PageText(
                "<b>Midnight Lockdown</b>\n" +
                "Assignment 6, VR escape room.\n\n" +
                "Models and textures: Poly Haven and ambientCG (CC0).\n" +
                "Sound effects: synthesized by our own python script.\n" +
                "Voices: macOS text to speech, processed by our script.\n" +
                "Shaders, puzzles and interaction code: written for this project.\n\n" +
                "Full list in SOURCES.md.", 0.021f);
        }
    }
}
