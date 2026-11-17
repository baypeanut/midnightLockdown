using UnityEngine;
using UnityEngine.Events;

namespace Lockdown
{
    // knob, dial, clock hand or turntable turned by the hand
    public class RotaryControl : MonoBehaviour, IGrabHandle
    {
        public enum Mode { Drag, Twist }

        public Transform pivot;
        public Vector3 localAxis = Vector3.forward;
        public Mode mode = Mode.Drag;
        public float stepDegrees = 0f;
        public bool snapWhileTurning = true;
        public bool clamp;
        public float minDegrees, maxDegrees = 360f;
        public float twistGain = 1f;
        public float initialDegrees = 0f;
        public string stepSound = "mirror_step";
        public float stepHaptic = 0.12f;
        public UnityEvent<int> onStep = new UnityEvent<int>();
        public UnityEvent onGrabbed = new UnityEvent();
        public UnityEvent onReleased = new UnityEvent();

        public float Value { get; private set; }
        public int Step => stepDegrees > 0f ? Mathf.RoundToInt(Value / stepDegrees) : 0;
        public bool Locked { get; set; }
        public bool IsHeld => holder != null;
        public int Priority => 2;
        public float LastDelta { get; private set; }

        HandController holder;
        Quaternion baseRotation;
        Vector3 lastDir;
        Quaternion lastHandRot;
        int lastStep;

        void Awake()
        {
            if (!pivot) pivot = transform;
            baseRotation = pivot.localRotation;
            SetValue(initialDegrees, false);
            lastStep = Step;
        }

        Vector3 WorldAxis => (pivot.parent ? pivot.parent.rotation : Quaternion.identity) * baseRotation * localAxis.normalized;

        public bool CanGrab(HandController hand) => !Locked && isActiveAndEnabled;

        public void BeginGrab(HandController hand)
        {
            holder = hand;
            lastDir = HandDir(hand);
            lastHandRot = hand.transform.rotation;
            onGrabbed.Invoke();
        }

        Vector3 HandDir(HandController hand) => Vector3.ProjectOnPlane(hand.GripPoint - pivot.position, WorldAxis);

        public void UpdateGrab(HandController hand)
        {
            float delta;
            Vector3 axis = WorldAxis;
            if (mode == Mode.Drag)
            {
                Vector3 dir = HandDir(hand);
                if (dir.sqrMagnitude < 0.0004f) return;
                delta = Vector3.SignedAngle(lastDir, dir, axis);
                lastDir = dir;
            }
            else
            {
                Quaternion dq = hand.transform.rotation * Quaternion.Inverse(lastHandRot);
                dq.ToAngleAxis(out float ang, out Vector3 ax);
                if (ang > 180f) ang -= 360f;
                delta = float.IsNaN(ax.x) ? 0f : ang * Vector3.Dot(ax, axis) * twistGain;
                lastHandRot = hand.transform.rotation;
            }
            if (Mathf.Abs(delta) > 90f) delta = 0f;
            SetValue(Value + delta, true);
            LastDelta = delta;
        }

        public void EndGrab(HandController hand)
        {
            holder = null;
            if (stepDegrees > 0f) SetValue(Step * stepDegrees, false);
            onReleased.Invoke();
        }

        public void SetValue(float v, bool notify)
        {
            if (clamp) v = Mathf.Clamp(v, minDegrees, maxDegrees);
            Value = v;
            float shown = stepDegrees > 0f && snapWhileTurning ? Step * stepDegrees : Value;
            pivot.localRotation = baseRotation * Quaternion.AngleAxis(shown, localAxis);
            int s = Step;
            if (s != lastStep)
            {
                lastStep = s;
                if (notify)
                {
                    if (!string.IsNullOrEmpty(stepSound)) Sfx.Play(stepSound, pivot.position, 0.5f);
                    if (holder && stepHaptic > 0f) Haptics.Pulse(holder.hand, stepHaptic, 0.02f);
                    onStep.Invoke(s);
                }
            }
        }

        public HandController Holder => holder;
    }
}
