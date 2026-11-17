using UnityEngine;
using UnityEngine.Events;

namespace Lockdown
{
    // optical lock that needs a steady beam for a moment
    public class LaserSensor : MonoBehaviour
    {
        public float holdSeconds = 0.6f;
        public Renderer lens;
        public UnityEvent onUnlocked = new UnityEvent();
        public bool Unlocked { get; private set; }
        float charge;
        int lastHitFrame = -10;
        MaterialPropertyBlock block;

        void Awake() => block = new MaterialPropertyBlock();

        public void Hit() => lastHitFrame = Time.frameCount;

        void Update()
        {
            if (Unlocked) return;
            bool lit = Time.frameCount - lastHitFrame <= 1;
            charge = Mathf.Clamp01(charge + (lit ? 1f : -2f) * Time.deltaTime / holdSeconds);
            if (lens)
            {
                lens.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", Color.Lerp(new Color(0.6f, 0.05f, 0.05f), new Color(0.3f, 3f, 0.8f), charge));
                lens.SetPropertyBlock(block);
            }
            if (charge >= 1f)
            {
                Unlocked = true;
                Sfx.Play("beep_ok", transform.position, 0.9f);
                onUnlocked.Invoke();
            }
        }
    }
}
