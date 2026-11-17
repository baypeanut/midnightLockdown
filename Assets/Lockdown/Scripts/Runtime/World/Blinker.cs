using UnityEngine;

namespace Lockdown
{
    // server led that blinks at its own rate
    public class Blinker : MonoBehaviour
    {
        public float period = 0.8f;
        public float duty = 0.6f;
        Renderer r;
        float offset;

        void Awake()
        {
            r = GetComponent<Renderer>();
            offset = Random.value * period;
        }

        void Update()
        {
            if (!r) return;
            if (GameSettings.ReduceFlashing) { r.enabled = true; return; }
            r.enabled = Mathf.Repeat(Time.time + offset, period) < period * duty;
        }
    }
}
