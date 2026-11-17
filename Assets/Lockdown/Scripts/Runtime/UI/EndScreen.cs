using System.Text;
using UnityEngine;

namespace Lockdown
{
    // result panel shown after a win or a loss
    public class EndScreen : MonoBehaviour
    {
        Transform root;

        void Start()
        {
            if (GameDirector.I) GameDirector.I.StateChanged += OnState;
        }

        void OnDestroy()
        {
            if (GameDirector.I) GameDirector.I.StateChanged -= OnState;
        }

        void OnState(GameState s)
        {
            if (s == GameState.Won || s == GameState.Lost) Invoke(nameof(Show), s == GameState.Won ? 3.5f : 3f);
        }

        void Show()
        {
            var gd = GameDirector.I;
            if (!gd || root) return;
            bool won = gd.State == GameState.Won;
            UiFocus.Active = true;
            root = new GameObject("EndScreen").transform;
            UiKit.PlaceInFront(root, 1.1f, 0.05f);
            UiKit.Panel(root, new Vector2(0.72f, 0.66f), Vector3.zero);
            UiKit.Text(root, won ? "LOCKDOWN LIFTED" : "SUPPRESSION ENGAGED", new Vector3(0f, 0.27f, -0.002f), new Vector2(0.68f, 0.07f), 0.05f,
                TMPro.TextAlignmentOptions.Center, won ? UiKit.Good : UiKit.Warn);
            var sb = new StringBuilder();
            sb.AppendLine(won ? "You escaped floor four." : "WARDEN sealed the floor. Try again.");
            sb.AppendLine();
            sb.AppendLine("Time used: " + Leaderboard.Format(gd.Elapsed) + "     Time left: " + Leaderboard.Format(gd.Remaining));
            sb.AppendLine("Puzzles: " + gd.SolvedCount + "/" + gd.PuzzleCount + "     Chips: " + gd.Chips + "/" + gd.totalChips);
            sb.AppendLine("Hints: " + gd.HintsUsed + "     Mistakes: " + gd.Mistakes);
            if (won)
            {
                sb.AppendLine();
                sb.Append("<size=140%><color=#FFD24D>Score " + gd.LastScore + "</color></size>");
                if (gd.LastRank > 0) sb.Append("   leaderboard #" + gd.LastRank);
                sb.AppendLine();
            }
            if (gd.unlockedThisRun.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Unlocked: " + string.Join(", ", gd.unlockedThisRun));
            }
            UiKit.Text(root, sb.ToString(), new Vector3(0f, 0.0f, -0.002f), new Vector2(0.66f, 0.44f), 0.021f, TMPro.TextAlignmentOptions.Top);
            UiKit.Button(root, "Play again", new Vector3(-0.17f, -0.27f, -0.006f), new Vector2(0.3f, 0.065f), () => gd.Restart(), 0.026f);
            UiKit.Button(root, "Main menu", new Vector3(0.17f, -0.27f, -0.006f), new Vector2(0.3f, 0.065f), () => gd.ToMenu(), 0.026f);
        }
    }
}
