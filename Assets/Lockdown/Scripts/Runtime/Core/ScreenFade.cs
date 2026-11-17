using System.Collections;
using UnityEngine;

namespace Lockdown
{
    // full view fade plus comfort vignette, lives on the head camera
    public class ScreenFade : MonoBehaviour
    {
        static ScreenFade inst;
        public Renderer fadeQuad;
        public Renderer vignette;
        public float vignetteStrength = 0.75f;

        MaterialPropertyBlock block;
        float fade, fadeTarget, fadeSpeed = 4f, vignetteAmount;
        Color fadeColor = Color.black;
        float headBlock;
        static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            inst = this;
            block = new MaterialPropertyBlock();
            Apply();
        }

        public static void FadeTo(float alpha, float seconds, Color color)
        {
            if (!inst) return;
            inst.fadeColor = color;
            inst.fadeTarget = alpha;
            inst.fadeSpeed = seconds <= 0f ? 1000f : 1f / seconds;
        }

        public static void FadeTo(float alpha, float seconds) => FadeTo(alpha, seconds, Color.black);

        public static void Blink(float seconds)
        {
            if (!inst) return;
            inst.StartCoroutine(inst.BlinkRoutine(seconds));
        }

        IEnumerator BlinkRoutine(float seconds)
        {
            fadeColor = Color.black;
            fadeTarget = 1f; fadeSpeed = 2f / seconds;
            yield return new WaitForSecondsRealtime(seconds * 0.5f);
            fadeTarget = 0f;
        }

        public static void SetVignette(float amount)
        {
            if (inst) inst.vignetteAmount = amount;
        }

        void LateUpdate()
        {
            fade = Mathf.MoveTowards(fade, fadeTarget, fadeSpeed * Time.unscaledDeltaTime);
            // darken when the head pushes into geometry
            headBlock = Mathf.MoveTowards(headBlock, HeadInside() ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            Apply();
        }

        readonly Collider[] overlap = new Collider[8];

        bool HeadInside()
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position, 0.07f, overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = overlap[i];
                if (h.transform.IsChildOf(transform.root)) continue;
                if (h.GetComponentInParent<Grabbable>()) continue;
                return true;
            }
            return false;
        }

        void Apply()
        {
            if (fadeQuad)
            {
                float a = Mathf.Max(fade, headBlock * 0.92f);
                fadeQuad.enabled = a > 0.001f;
                Color c = fade >= headBlock ? fadeColor : Color.black;
                c.a = a;
                fadeQuad.GetPropertyBlock(block);
                block.SetColor(ColorId, c);
                fadeQuad.SetPropertyBlock(block);
            }
            if (vignette)
            {
                float v = vignetteAmount * vignetteStrength;
                vignette.enabled = v > 0.01f;
                vignette.GetPropertyBlock(block);
                block.SetColor(ColorId, new Color(0f, 0f, 0f, v));
                vignette.SetPropertyBlock(block);
            }
        }
    }
}
