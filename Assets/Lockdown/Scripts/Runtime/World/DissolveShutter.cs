using System.Collections;
using UnityEngine;

namespace Lockdown
{
    // drives the dissolve shader on the vault shutter then removes its collider
    public class DissolveShutter : MonoBehaviour
    {
        public Renderer target;
        public Collider blocker;
        public ParticleSystem embers;
        public float seconds = 2.4f;
        public bool Open { get; private set; }
        MaterialPropertyBlock block;
        static readonly int Cut = Shader.PropertyToID("_Cutoff");

        void Awake()
        {
            block = new MaterialPropertyBlock();
            Set(0f);
        }

        public void Dissolve()
        {
            if (Open) return;
            Open = true;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            Sfx.Play("shutter_dissolve", transform.position, 1f);
            if (embers) embers.Play();
            for (float t = 0; t < 1f; t += Time.deltaTime / seconds)
            {
                Set(t);
                yield return null;
            }
            Set(1.05f);
            if (blocker) blocker.enabled = false;
            if (target) target.enabled = false;
            if (embers) embers.Stop();
        }

        void Set(float v)
        {
            if (!target) return;
            target.GetPropertyBlock(block);
            block.SetFloat(Cut, v);
            target.SetPropertyBlock(block);
        }
    }
}
