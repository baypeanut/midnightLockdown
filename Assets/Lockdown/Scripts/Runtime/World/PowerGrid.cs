using System.Collections;
using UnityEngine;

namespace Lockdown
{
    // switches the floor between emergency red and normal power
    public class PowerGrid : MonoBehaviour
    {
        public static PowerGrid I { get; private set; }

        public Light[] mainLights;
        public Renderer[] mainLightPanels;
        public Light[] emergencyLights;
        public Transform[] beacons;
        public Renderer[] beaconLenses;
        public AudioSource alarm;
        public AudioSource hum;
        public Color panelOn = new Color(1f, 0.97f, 0.9f);
        public float panelOnIntensity = 2.2f;
        public Color ambientDark = new Color(0.06f, 0.03f, 0.035f);
        public Color ambientLit = new Color(0.32f, 0.33f, 0.36f);

        public bool On { get; private set; }
        float[] mainIntensity;
        MaterialPropertyBlock block;
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            I = this;
            block = new MaterialPropertyBlock();
            mainIntensity = new float[mainLights.Length];
            for (int i = 0; i < mainLights.Length; i++) mainIntensity[i] = mainLights[i] ? mainLights[i].intensity : 1f;
            Apply(0f);
        }

        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            if (On) return;
            bool calm = GameSettings.ReduceFlashing;
            float spin = calm ? 0f : 160f * Time.deltaTime;
            foreach (var b in beacons) if (b) b.Rotate(0f, spin, 0f, Space.Self);
            float pulse = calm ? 0.7f : 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * 2.2f));
            foreach (var l in emergencyLights) if (l) l.intensity = 1.6f * pulse;
        }

        public void PowerOn()
        {
            if (On) return;
            On = true;
            StartCoroutine(Restore());
        }

        IEnumerator Restore()
        {
            Sfx.Play2D("power_up", 0.8f);
            if (alarm) alarm.Stop();
            // flicker on like old fluorescent tubes
            float[] pattern = { 0.6f, 0f, 0.9f, 0.1f, 1f };
            foreach (float v in pattern)
            {
                Apply(v);
                yield return new WaitForSeconds(GameSettings.ReduceFlashing ? 0.25f : 0.09f);
            }
            for (float t = 0; t < 1f; t += Time.deltaTime)
            {
                Apply(1f);
                foreach (var l in emergencyLights) if (l) l.intensity = Mathf.Lerp(l.intensity, 0f, t);
                yield return null;
            }
            foreach (var l in emergencyLights) if (l) l.enabled = false;
            foreach (var r in beaconLenses) if (r) SetEmission(r, Color.black);
            if (hum) hum.Play();
        }

        void Apply(float level)
        {
            for (int i = 0; i < mainLights.Length; i++)
            {
                if (!mainLights[i]) continue;
                mainLights[i].enabled = level > 0.01f;
                mainLights[i].intensity = mainIntensity[i] * level;
            }
            foreach (var r in mainLightPanels) if (r) SetEmission(r, panelOn * panelOnIntensity * level);
            RenderSettings.ambientLight = Color.Lerp(ambientDark, ambientLit, level);
        }

        void SetEmission(Renderer r, Color c)
        {
            r.GetPropertyBlock(block);
            block.SetColor(Emission, c);
            r.SetPropertyBlock(block);
        }

        // green wash used when the player escapes
        public void Celebrate()
        {
            RenderSettings.ambientLight = new Color(0.3f, 0.42f, 0.36f);
            foreach (var l in emergencyLights) if (l) l.enabled = false;
        }
    }
}
