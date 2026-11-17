using UnityEngine;
using UnityEngine.Events;

namespace Lockdown
{
    // big hinged lever pulled from one end stop to the other
    public class LeverControl : MonoBehaviour, IGrabHandle
    {
        public Transform hinge;
        public Vector3 localAxis = Vector3.right;
        public float upAngle = -50f;
        public float downAngle = 50f;
        public bool returnsUp = false;
        public UnityEvent onPulled = new UnityEvent();
        public UnityEvent onBlocked = new UnityEvent();

        public bool Locked { get; set; }
        public bool Armed { get; set; } = true;
        public bool Pulled { get; private set; }
        public int Priority => 2;

        HandController holder;
        Quaternion baseRot;
        float angle;
        bool warned;

        void Awake()
        {
            if (!hinge) hinge = transform;
            baseRot = hinge.localRotation;
            SetAngle(upAngle);
        }

        public bool CanGrab(HandController hand) => !Locked && !Pulled;

        public void BeginGrab(HandController hand)
        {
            holder = hand;
            warned = false;
        }

        public void UpdateGrab(HandController hand)
        {
            Vector3 axis = (hinge.parent ? hinge.parent.rotation : Quaternion.identity) * baseRot * localAxis.normalized;
            Vector3 rest = (hinge.parent ? hinge.parent.rotation : Quaternion.identity) * baseRot * Vector3.up;
            Vector3 dir = Vector3.ProjectOnPlane(hand.GripPoint - hinge.position, axis);
            if (dir.sqrMagnitude < 0.0004f) return;
            float a = Vector3.SignedAngle(rest, dir, axis);
            float lo = Mathf.Min(upAngle, downAngle), hi = Mathf.Max(upAngle, downAngle);
            a = Mathf.Clamp(a, lo, hi);
            if (!Armed)
            {
                // stiff when the panel is not ready
                float limit = upAngle + Mathf.Sign(downAngle - upAngle) * 12f;
                a = Mathf.Clamp(a, Mathf.Min(upAngle, limit), Mathf.Max(upAngle, limit));
                if (!warned && Mathf.Abs(a - limit) < 1f) { warned = true; onBlocked.Invoke(); Haptics.Pulse(hand.hand, 0.5f, 0.08f); }
            }
            SetAngle(a);
            if (Random.value < 0.15f) Haptics.Pulse(hand.hand, 0.06f, 0.02f);
            if (Armed && Mathf.Abs(a - downAngle) < 4f)
            {
                Pulled = true;
                SetAngle(downAngle);
                Haptics.Pulse(hand.hand, 0.8f, 0.15f);
                Sfx.Play("lever_clunk", hinge.position, 1f);
                hand.Release();
                onPulled.Invoke();
            }
        }

        public void EndGrab(HandController hand)
        {
            holder = null;
            if (!Pulled) SetAngle(upAngle);
        }

        void SetAngle(float a)
        {
            angle = a;
            hinge.localRotation = baseRot * Quaternion.AngleAxis(a, localAxis);
        }

        // editor check helper: same result as a full pull
        public void DebugPull()
        {
            if (!Armed || Pulled) return;
            Pulled = true;
            SetAngle(downAngle);
            Sfx.Play("lever_clunk", hinge.position, 1f);
            onPulled.Invoke();
        }

        public void ResetLever()
        {
            Pulled = false;
            SetAngle(upAngle);
        }
    }
}
