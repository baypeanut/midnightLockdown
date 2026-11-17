using UnityEngine;

namespace Lockdown
{
    // rising halon fog and heartbeat as the clock runs out, confetti on a win
    public class SuppressionFx : MonoBehaviour
    {
        public ParticleSystem gas;
        public ParticleSystem celebration;
        public AudioSource heartbeat;
        public float startAtSeconds = 120f;
        bool ended;

        void Start()
        {
            if (GameDirector.I) GameDirector.I.StateChanged += OnState;
            if (gas) gas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        void OnDestroy()
        {
            if (GameDirector.I) GameDirector.I.StateChanged -= OnState;
        }

        void Update()
        {
            var gd = GameDirector.I;
            if (!gd || ended) return;
            float danger = Mathf.Clamp01(1f - gd.Remaining / startAtSeconds);
            if (gas)
            {
                var em = gas.emission;
                em.rateOverTime = Mathf.Lerp(0f, 60f, danger);
                if (danger > 0f && !gas.isPlaying) gas.Play();
            }
            if (heartbeat)
            {
                bool on = gd.Remaining < 60f && gd.State == GameState.Playing;
                if (on && !heartbeat.isPlaying) heartbeat.Play();
                if (!on && heartbeat.isPlaying) heartbeat.Stop();
                heartbeat.pitch = 1f + 0.4f * Mathf.Clamp01(1f - gd.Remaining / 60f);
                if (on && Random.value < Time.deltaTime * 1.2f) Haptics.Both(0.15f, 0.06f);
            }
        }

        void OnState(GameState s)
        {
            if (s == GameState.Won)
            {
                ended = true;
                if (gas) gas.Stop();
                if (heartbeat) heartbeat.Stop();
                if (celebration) celebration.Play();
                PowerGrid.I?.Celebrate();
            }
            else if (s == GameState.Lost)
            {
                ended = true;
                if (heartbeat) heartbeat.Stop();
                if (gas)
                {
                    var em = gas.emission;
                    em.rateOverTime = 140f;
                }
                ScreenFade.FadeTo(0.35f, 3f, new Color(0.75f, 0.85f, 0.8f));
            }
        }
    }
}
