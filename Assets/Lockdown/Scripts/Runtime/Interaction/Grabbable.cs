using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lockdown
{
    // free object that can be picked up, thrown and socketed
    [RequireComponent(typeof(Rigidbody))]
    public class Grabbable : MonoBehaviour, IGrabHandle
    {
        public string itemId = "";
        public string displayName = "";
        [Tooltip("0 is feather light, 1 lags far behind the hand")]
        [Range(0f, 1f)] public float weight = 0f;
        public bool allowDistanceGrab = true;
        public bool snapToHand = false;
        public bool startKinematic = false;
        public Vector3 handPositionOffset;
        public Vector3 handRotationOffset;
        public UnityEvent onGrab = new UnityEvent();
        public UnityEvent onRelease = new UnityEvent();

        public bool IsHeld => holder != null;
        public bool Locked { get; set; }
        public HandController Holder => holder;
        public ItemSocket Socket { get; set; }
        public int Priority => 3;
        public Vector3 HomePosition { get; private set; }
        public Quaternion HomeRotation { get; private set; }

        Rigidbody body;
        HandController holder;
        Vector3 localPos;
        Quaternion localRot;
        Renderer[] renderers;
        MaterialPropertyBlock block;
        bool highlighted;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly List<Grabbable> all = new List<Grabbable>();
        public static IReadOnlyList<Grabbable> All => all;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            renderers = GetComponentsInChildren<Renderer>();
            block = new MaterialPropertyBlock();
            HomePosition = transform.position;
            HomeRotation = transform.rotation;
            if (startKinematic) body.isKinematic = true;
        }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public bool CanGrab(HandController hand) => !Locked && isActiveAndEnabled;

        public void BeginGrab(HandController hand)
        {
            if (Socket) Socket.Detach(this);
            transform.SetParent(null, true);
            holder = hand;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            if (snapToHand)
            {
                localRot = Quaternion.Euler(handRotationOffset);
                localPos = handPositionOffset;
            }
            else
            {
                localRot = Quaternion.Inverse(hand.transform.rotation) * transform.rotation;
                localPos = hand.transform.InverseTransformPoint(transform.position);
            }
            SetHighlight(false);
            Sfx.Play("grab", transform.position, 0.5f);
            onGrab.Invoke();
            GameEvents.ItemGrabbed(this);
        }

        public void UpdateGrab(HandController hand)
        {
            Vector3 targetPos = hand.transform.TransformPoint(localPos);
            Quaternion targetRot = hand.transform.rotation * localRot;
            if (weight <= 0.01f)
            {
                transform.SetPositionAndRotation(targetPos, targetRot);
                return;
            }
            // pseudo haptic weight: heavy things trail the real hand
            float rate = Mathf.Lerp(30f, 3.5f, weight);
            float k = 1f - Mathf.Exp(-rate * Time.deltaTime);
            Vector3 p = Vector3.Lerp(transform.position, targetPos, k);
            // gravity sag grows with weight
            p += Vector3.down * (weight * 0.04f * k);
            transform.SetPositionAndRotation(p, Quaternion.Slerp(transform.rotation, targetRot, k));
            if ((p - targetPos).sqrMagnitude > 0.0025f && Random.value < 0.08f) Haptics.Pulse(hand.hand, 0.1f * weight, 0.03f);
        }

        public void EndGrab(HandController hand)
        {
            holder = null;
            if (ItemSocket.TryCapture(this)) return;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            float carry = 1f - weight * 0.7f;
            body.linearVelocity = hand.Velocity * carry;
            body.angularVelocity = hand.AngularVelocity * carry;
            Sfx.Play("release", transform.position, 0.35f);
            onRelease.Invoke();
        }

        public void BeginPull() { body.isKinematic = true; }
        public void EndPull() { if (!IsHeld && !Socket) body.isKinematic = false; }

        public void SetHighlight(bool on)
        {
            if (highlighted == on || renderers == null) return;
            highlighted = on;
            foreach (var r in renderers)
            {
                if (!r) continue;
                r.GetPropertyBlock(block);
                block.SetColor(EmissionId, on ? new Color(0.18f, 0.3f, 0.35f) : Color.black);
                r.SetPropertyBlock(block);
            }
        }

        public void Freeze()
        {
            body.isKinematic = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        public void ReturnHome()
        {
            if (holder) holder.Release();
            if (Socket) Socket.Detach(this);
            body.isKinematic = true;
            transform.SetPositionAndRotation(HomePosition + Vector3.up * 0.05f, HomeRotation);
            body.isKinematic = false;
        }

        // put items back if they fall out of the world
        void FixedUpdate()
        {
            if (!IsHeld && !Socket && transform.position.y < -2f) ReturnHome();
        }
    }
}
