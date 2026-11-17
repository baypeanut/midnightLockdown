using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lockdown
{
    // radio voice lines with captions, played one at a time
    public class Narrator : MonoBehaviour
    {
        public static Narrator I { get; private set; }

        [Serializable] class Line { public string key, speaker, text; }
        [Serializable] class LineFile { public Line[] lines; }

        AudioSource source;
        readonly Dictionary<string, Line> lines = new Dictionary<string, Line>();
        readonly Queue<string> queue = new Queue<string>();
        readonly HashSet<string> played = new HashSet<string>();
        float busyUntil;

        void Awake()
        {
            I = this;
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            source.volume = 0.95f;
            var asset = Resources.Load<TextAsset>("Audio/vo/lines");
            if (asset)
            {
                var f = JsonUtility.FromJson<LineFile>(asset.text);
                if (f?.lines != null) foreach (var l in f.lines) lines[l.key] = l;
            }
        }

        void OnDestroy() { if (I == this) I = null; }

        // play once per run
        public void Say(string key, bool once = true)
        {
            if (once && played.Contains(key)) return;
            played.Add(key);
            queue.Enqueue(key);
        }

        public void SayNow(string key)
        {
            queue.Clear();
            source.Stop();
            busyUntil = 0f;
            played.Add(key);
            queue.Enqueue(key);
        }

        public void StopAll()
        {
            queue.Clear();
            source.Stop();
        }

        void Update()
        {
            if (Time.unscaledTime < busyUntil || queue.Count == 0) return;
            string key = queue.Dequeue();
            lines.TryGetValue(key, out var line);
            var clip = Resources.Load<AudioClip>("Audio/vo/" + key);
            float len = clip ? clip.length : 3.5f;
            if (clip && GameSettings.Narration)
            {
                source.clip = clip;
                source.Play();
            }
            else if (line != null)
            {
                Sfx.Play2D("hint_chime", 0.4f);
                len = Mathf.Max(3f, line.text.Length * 0.06f);
            }
            if (line != null && Hud.I)
            {
                string color = line.speaker == "WARDEN" ? "#FF6A5A" : "#6FE3FF";
                Hud.I.SetCaption("<color=" + color + ">" + line.speaker + ":</color> " + line.text, len + 0.6f);
            }
            busyUntil = Time.unscaledTime + len + 0.4f;
        }
    }
}
