using System.Collections;
using UnityEngine;

namespace Lockdown
{
    // panel door that slides sideways when unlocked
    public class SlidingDoor : MonoBehaviour
    {
        public Transform panel;
        public Vector3 openOffset = new Vector3(1f, 0f, 0f);
        public float seconds = 1.1f;
        public Renderer statusLight;
        public bool IsOpen { get; private set; }
        Vector3 closed;
        MaterialPropertyBlock block;

        void Awake()
        {
            if (!panel) panel = transform;
            closed = panel.localPosition;
            block = new MaterialPropertyBlock();
            SetStatus(false);
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            SetStatus(true);
            Sfx.Play("door_slide", panel.position, 0.9f);
            StartCoroutine(Slide());
        }

        IEnumerator Slide()
        {
            Vector3 from = panel.localPosition, to = closed + openOffset;
            for (float t = 0; t < 1f; t += Time.deltaTime / seconds)
            {
                panel.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            panel.localPosition = to;
        }

        void SetStatus(bool open)
        {
            if (!statusLight) return;
            statusLight.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", open ? new Color(0.2f, 2f, 0.6f) : new Color(2f, 0.2f, 0.15f));
            block.SetColor("_BaseColor", open ? Color.green : Color.red);
            statusLight.SetPropertyBlock(block);
        }
    }
}
