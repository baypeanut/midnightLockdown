using System;
using UnityEngine;

namespace Lockdown
{
    // unlockable badges saved in player prefs
    public static class Achievements
    {
        public struct Def
        {
            public string id, title, description;
            public Def(string i, string t, string d) { id = i; title = t; description = d; }
        }

        public static readonly Def[] All =
        {
            new Def("lights", "Let There Be Light", "Restore power to floor four."),
            new Def("climber", "Up and Over", "Reach the maintenance catwalk."),
            new Def("safecracker", "Safecracker", "Open the safe without a single reset."),
            new Def("mirrors", "Clean Beam", "Solve the laser in 24 mirror steps or fewer."),
            new Def("collector", "Chip Collector", "Find all five override chips in one run."),
            new Def("escape", "Out By Morning", "Escape the lockdown."),
            new Def("unassisted", "Unassisted", "Escape without using a hint."),
            new Def("nightowl", "Night Owl", "Escape in under five minutes."),
        };

        public static event Action<Def> Unlocked;

        public static bool Has(string id) => PlayerPrefs.GetInt("ml.ach." + id, 0) == 1;

        public static int Count
        {
            get { int n = 0; foreach (var d in All) if (Has(d.id)) n++; return n; }
        }

        public static void Unlock(string id)
        {
            if (Has(id)) return;
            PlayerPrefs.SetInt("ml.ach." + id, 1);
            PlayerPrefs.Save();
            foreach (var d in All) if (d.id == id) Unlocked?.Invoke(d);
        }

        public static void ResetAll()
        {
            foreach (var d in All) PlayerPrefs.DeleteKey("ml.ach." + d.id);
            PlayerPrefs.Save();
        }
    }
}
