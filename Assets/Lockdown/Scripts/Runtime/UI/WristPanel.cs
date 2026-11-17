using TMPro;
using UnityEngine;

namespace Lockdown
{
    // watch style panel on the left wrist: timer, objective, hint and menu
    public class WristPanel : MonoBehaviour
    {
        public Transform hand;
        TextMeshPro timer, objective, progress;
        Transform content;
        string lastHint = "";
        float hintShownUntil;

        void Start()
        {
            content = new GameObject("Content").transform;
            content.SetParent(transform, false);
            UiKit.Panel(content, new Vector2(0.16f, 0.11f), Vector3.zero, "WristBg");
            timer = UiKit.Text(content, "10:00", new Vector3(-0.035f, 0.036f, -0.001f), new Vector2(0.085f, 0.03f), 0.024f, TextAlignmentOptions.Left, UiKit.Accent);
            progress = UiKit.Text(content, "0/5", new Vector3(0.05f, 0.036f, -0.001f), new Vector2(0.05f, 0.03f), 0.014f, TextAlignmentOptions.Right, UiKit.Dim);
            objective = UiKit.Text(content, "", new Vector3(0f, 0.003f, -0.001f), new Vector2(0.15f, 0.04f), 0.011f, TextAlignmentOptions.Center, UiKit.Ink);
            UiKit.Button(content, "HINT", new Vector3(-0.038f, -0.036f, -0.004f), new Vector2(0.065f, 0.026f), ShowHint, 0.012f);
            UiKit.Button(content, "MENU", new Vector3(0.038f, -0.036f, -0.004f), new Vector2(0.065f, 0.026f), () => PauseMenu.I?.Toggle(), 0.012f);
        }

        void ShowHint()
        {
            var gd = GameDirector.I;
            if (!gd) return;
            lastHint = gd.UseHint();
            hintShownUntil = Time.unscaledTime + 10f;
            Hud.I?.SetCaption("<color=#FFD24D>Hint:</color> " + lastHint, 10f);
        }

        void Update()
        {
            var gd = GameDirector.I;
            if (!gd || !timer) return;
            timer.text = Leaderboard.Format(gd.Remaining);
            timer.color = gd.Remaining < 60f ? UiKit.Warn : UiKit.Accent;
            progress.text = gd.SolvedCount + "/" + gd.PuzzleCount;
            objective.text = gd.CurrentStage == Stage.Done ? "Escaped" : GameText.Objective(gd.CurrentStage);
            // floats above the wrist and turns toward the eyes when raised
            var rig = PlayerRig.I;
            if (rig && hand)
            {
                var head = rig.head.transform;
                transform.position = hand.TransformPoint(new Vector3(0f, 0.06f, -0.06f));
                Vector3 toPanel = transform.position - head.position;
                transform.rotation = Quaternion.LookRotation(toPanel, Vector3.up);
                bool visible = toPanel.magnitude < 0.7f && Vector3.Angle(head.forward, toPanel) < 38f;
                if (content.gameObject.activeSelf != visible) content.gameObject.SetActive(visible);
            }
        }
    }
}
