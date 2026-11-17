using UnityEngine;

namespace Lockdown
{
    // drawer pulled along its local forward axis
    public class SlideDrawer : MonoBehaviour, IGrabHandle
    {
        public Transform body;
        public float maxTravel = 0.35f;
        public bool Locked { get; set; }
        public int Priority => 2;
        public float Open01 => travel / Mathf.Max(0.001f, maxTravel);

        Vector3 closedLocal;
        Vector3 grabOffset;
        float travel, lastSoundTravel;

        void Awake()
        {
            if (!body) body = transform;
            closedLocal = body.localPosition;
        }

        Vector3 Axis => body.parent ? body.parent.TransformDirection(Vector3.forward) : Vector3.forward;

        public bool CanGrab(HandController hand) => !Locked;

        public void BeginGrab(HandController hand)
        {
            grabOffset = body.position - hand.GripPoint;
            lastSoundTravel = travel;
        }

        public void UpdateGrab(HandController hand)
        {
            Vector3 closedWorld = body.parent ? body.parent.TransformPoint(closedLocal) : closedLocal;
            float t = Vector3.Dot(hand.GripPoint + grabOffset - closedWorld, Axis);
            travel = Mathf.Clamp(t, 0f, maxTravel);
            body.localPosition = closedLocal + Vector3.forward * travel;
            if (Mathf.Abs(travel - lastSoundTravel) > 0.12f)
            {
                lastSoundTravel = travel;
                Sfx.Play("drawer_slide", body.position, 0.5f);
                Haptics.Pulse(hand.hand, 0.12f, 0.03f);
            }
        }

        public void EndGrab(HandController hand) { }
    }
}
