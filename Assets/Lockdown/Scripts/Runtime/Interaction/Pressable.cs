using UnityEngine;
using UnityEngine.Events;

namespace Lockdown
{
    // button that works by pointer trigger or by finger poke
    public class Pressable : MonoBehaviour
    {
        public bool allowRay = true;
        public bool allowPoke = true;
        [Tooltip("seconds to hold, zero for a normal click")]
        public float holdSeconds = 0f;
        public Transform cap;
        public float capTravel = 0.006f;
        public string clickSound = "ui_click";
        public UnityEvent onPress = new UnityEvent();
        public UnityEvent<float> onHoldProgress = new UnityEvent<float>();
        public UnityEvent onHoldCancel = new UnityEvent();

        public bool Interactable { get; set; } = true;
        public bool IsDown => pressers > 0;
        public float HoldProgress { get; private set; }

        int pressers;
        bool fired;
        Vector3 capRest;
        float lastClick;
        HandController holdHand;

        void Awake() { if (cap) capRest = cap.localPosition; }

        void OnDisable()
        {
            pressers = 0;
            HoldProgress = 0f;
            fired = false;
            if (cap) cap.localPosition = capRest;
        }

        public void RayPress(HandController h) => Down(h);
        public void RayRelease(HandController h) => Up(h);
        public void PokeEnter(HandController h) => Down(h);
        public void PokeExit(HandController h) => Up(h);

        void Down(HandController h)
        {
            if (!Interactable) return;
            pressers++;
            holdHand = h;
            if (cap) cap.localPosition = capRest + Vector3.forward * capTravel;
            if (holdSeconds <= 0f && !fired && Time.unscaledTime - lastClick > 0.25f)
            {
                fired = true;
                lastClick = Time.unscaledTime;
                if (!string.IsNullOrEmpty(clickSound)) Sfx.Play(clickSound, transform.position, 0.6f);
                if (h) Haptics.Pulse(h.hand, 0.3f, 0.03f);
                onPress.Invoke();
            }
        }

        void Up(HandController h)
        {
            pressers = Mathf.Max(0, pressers - 1);
            if (pressers > 0) return;
            if (cap) cap.localPosition = capRest;
            if (holdSeconds > 0f && !fired && HoldProgress > 0f) onHoldCancel.Invoke();
            HoldProgress = 0f;
            fired = false;
            if (holdSeconds > 0f) onHoldProgress.Invoke(0f);
        }

        void Update()
        {
            if (holdSeconds <= 0f || pressers == 0 || fired || !Interactable) return;
            HoldProgress = Mathf.Clamp01(HoldProgress + Time.deltaTime / holdSeconds);
            onHoldProgress.Invoke(HoldProgress);
            if (holdHand && Random.value < 0.3f) Haptics.Pulse(holdHand.hand, 0.1f + 0.5f * HoldProgress, 0.03f);
            if (HoldProgress >= 1f)
            {
                fired = true;
                onPress.Invoke();
            }
        }
    }
}
