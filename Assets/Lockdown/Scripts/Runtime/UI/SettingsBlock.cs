using UnityEngine;

namespace Lockdown
{
    // shared settings toggles used by the pause menu and the main menu
    public static class SettingsBlock
    {
        public static void Build(Transform parent, Vector3 origin, bool includeTimer)
        {
            float y = origin.y;
            Row(parent, origin.x, ref y, () => "Smooth walk: " + OnOff(GameSettings.SmoothMove), () => GameSettings.SmoothMove = !GameSettings.SmoothMove);
            Row(parent, origin.x, ref y, () => "Snap turn: " + GameSettings.SnapAngle + " deg", () => GameSettings.SnapAngle = GameSettings.SnapAngle == 45 ? 30 : GameSettings.SnapAngle == 30 ? 60 : 45);
            Row(parent, origin.x, ref y, () => "Comfort vignette: " + OnOff(GameSettings.Vignette), () => GameSettings.Vignette = !GameSettings.Vignette);
            Row(parent, origin.x, ref y, () => "Radio voice: " + OnOff(GameSettings.Narration), () => GameSettings.Narration = !GameSettings.Narration);
            Row(parent, origin.x, ref y, () => "Reduce flashing: " + OnOff(GameSettings.ReduceFlashing), () => GameSettings.ReduceFlashing = !GameSettings.ReduceFlashing);
            Row(parent, origin.x, ref y, () => "Vibration: " + OnOff(GameSettings.Haptic), () => GameSettings.Haptic = !GameSettings.Haptic);
            if (includeTimer)
                Row(parent, origin.x, ref y, () => "Timer: " + GameSettings.TimerMinutes + " min", () => GameSettings.TimerMinutes = GameSettings.TimerMinutes == 10 ? 8 : GameSettings.TimerMinutes == 8 ? 5 : 10);
        }

        static string OnOff(bool b) => b ? "on" : "off";

        static void Row(Transform parent, float x, ref float y, System.Func<string> label, System.Action toggle)
        {
            Pressable b = null;
            b = UiKit.Button(parent, label(), new Vector3(x, y, -0.006f), new Vector2(0.42f, 0.05f), () =>
            {
                toggle();
                UiKit.SetLabel(b, label());
            }, 0.02f);
            y -= 0.062f;
        }
    }
}
