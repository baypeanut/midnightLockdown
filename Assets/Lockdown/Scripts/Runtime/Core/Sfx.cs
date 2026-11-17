using System.Collections.Generic;
using UnityEngine;

namespace Lockdown
{
    // pooled positional sound effects loaded from resources
    public class Sfx : MonoBehaviour
    {
        static Sfx inst;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly List<AudioSource> pool = new List<AudioSource>();
        int next;
        public static float Volume = 1f;

        static Sfx Inst
        {
            get
            {
                if (!inst)
                {
                    var go = new GameObject("Sfx");
                    inst = go.AddComponent<Sfx>();
                    for (int i = 0; i < 20; i++)
                    {
                        var s = new GameObject("voice" + i).AddComponent<AudioSource>();
                        s.transform.SetParent(go.transform);
                        s.playOnAwake = false;
                        s.spatialBlend = 1f;
                        s.rolloffMode = AudioRolloffMode.Linear;
                        s.minDistance = 0.6f;
                        s.maxDistance = 14f;
                        s.dopplerLevel = 0f;
                        inst.pool.Add(s);
                    }
                }
                return inst;
            }
        }

        public static AudioClip Clip(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (!clips.TryGetValue(name, out var c))
            {
                c = Resources.Load<AudioClip>("Audio/sfx/" + name);
                clips[name] = c;
                if (!c) Debug.LogWarning("missing sfx " + name);
            }
            return c;
        }

        public static AudioSource Play(string name, Vector3 at, float volume = 1f, float pitch = 1f)
        {
            var clip = Clip(name);
            if (!clip) return null;
            var s = Inst.pool[Inst.next];
            Inst.next = (Inst.next + 1) % Inst.pool.Count;
            s.transform.position = at;
            s.clip = clip;
            s.volume = volume * Volume;
            s.pitch = pitch * Random.Range(0.97f, 1.03f);
            s.spatialBlend = 1f;
            s.Play();
            return s;
        }

        public static AudioSource Play2D(string name, float volume = 1f)
        {
            var s = Play(name, Vector3.zero, volume);
            if (s) s.spatialBlend = 0f;
            return s;
        }

        // looping emitter placed in the scene
        public static AudioSource Loop(GameObject host, string name, float volume, float maxDistance = 10f)
        {
            var s = host.GetComponent<AudioSource>();
            if (!s) s = host.AddComponent<AudioSource>();
            s.clip = Clip(name);
            s.loop = true;
            s.volume = volume;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 0.8f;
            s.maxDistance = maxDistance;
            s.dopplerLevel = 0f;
            s.playOnAwake = true;
            if (Application.isPlaying && s.clip) s.Play();
            return s;
        }
    }
}
