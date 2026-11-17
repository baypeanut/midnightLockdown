using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lockdown
{
    // local top five runs
    public static class Leaderboard
    {
        [Serializable]
        public class Entry
        {
            public int score;
            public float seconds;
            public int chips;
            public int hints;
            public string date;
        }

        [Serializable]
        class Table { public List<Entry> entries = new List<Entry>(); }

        const string Key = "ml.board";
        public const int Size = 5;

        public static List<Entry> Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new List<Entry>();
            try
            {
                var t = JsonUtility.FromJson<Table>(json);
                return t?.entries ?? new List<Entry>();
            }
            catch { return new List<Entry>(); }
        }

        // returns rank starting at 1, or 0 if it did not place
        public static int Submit(Entry e)
        {
            var list = Load();
            list.Add(e);
            list.Sort((a, b) => b.score != a.score ? b.score.CompareTo(a.score) : a.seconds.CompareTo(b.seconds));
            int rank = list.IndexOf(e) + 1;
            if (list.Count > Size) list.RemoveRange(Size, list.Count - Size);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(new Table { entries = list }));
            PlayerPrefs.Save();
            return rank <= Size ? rank : 0;
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        public static string Format(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }
    }
}
