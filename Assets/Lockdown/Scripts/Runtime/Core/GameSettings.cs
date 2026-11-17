using UnityEngine;

namespace Lockdown
{
    // player preferences kept between sessions
    public static class GameSettings
    {
        const string P = "ml.";

        public static int TimerMinutes
        {
            get => PlayerPrefs.GetInt(P + "timer", 10);
            set { PlayerPrefs.SetInt(P + "timer", value); PlayerPrefs.Save(); }
        }

        public static bool SmoothMove
        {
            get => PlayerPrefs.GetInt(P + "smooth", 1) == 1;
            set { PlayerPrefs.SetInt(P + "smooth", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static int SnapAngle
        {
            get => PlayerPrefs.GetInt(P + "snap", 45);
            set { PlayerPrefs.SetInt(P + "snap", value); PlayerPrefs.Save(); }
        }

        public static bool Vignette
        {
            get => PlayerPrefs.GetInt(P + "vignette", 1) == 1;
            set { PlayerPrefs.SetInt(P + "vignette", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool Narration
        {
            get => PlayerPrefs.GetInt(P + "voice", 1) == 1;
            set { PlayerPrefs.SetInt(P + "voice", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool ReduceFlashing
        {
            get => PlayerPrefs.GetInt(P + "calm", 0) == 1;
            set { PlayerPrefs.SetInt(P + "calm", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool Haptic
        {
            get => PlayerPrefs.GetInt(P + "haptic", 1) == 1;
            set { PlayerPrefs.SetInt(P + "haptic", value ? 1 : 0); PlayerPrefs.Save(); Haptics.Enabled = value; }
        }

        public static void Apply() => Haptics.Enabled = Haptic;
    }
}
