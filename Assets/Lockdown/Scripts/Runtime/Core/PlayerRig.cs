using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Lockdown
{
    // root of the player, owns body movement and gravity
    [DefaultExecutionOrder(-50)]
    public class PlayerRig : MonoBehaviour
    {
        public static PlayerRig I { get; private set; }

        public Transform trackingSpace;
        public Camera head;
        public HandController left, right;
        public float fallSpeedMax = 6f;
        public LayerMask groundMask = ~0;

        public bool Frozen { get; set; }
        public bool Climbing => climbHand != null;
        public bool Grounded { get; private set; } = true;
        public Vector3 Feet => new Vector3(head.transform.position.x, transform.position.y, head.transform.position.z);
        public float HeadHeight => head.transform.position.y - transform.position.y;
        public event System.Action<Vector3> Teleported;
        public event System.Action Landed;

        HandController climbHand;
        Vector3 climbAnchor;
        float fallVelocity, fallStartY;
        int originTries;
        readonly List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();

        void Awake() => I = this;
        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            if (originTries < 120) { originTries++; TrySetFloorOrigin(); }
            if (Frozen) return;
            if (climbHand != null) UpdateClimb();
            else ApplyGravity(Time.deltaTime);
        }

        void TrySetFloorOrigin()
        {
            subsystems.Clear();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var s in subsystems)
                if (s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor)) originTries = 999;
        }

        public HandController Other(HandController h) => h == left ? right : left;

        // body moves so the gripping hand stays fixed in the world
        public void BeginClimb(HandController hand, Vector3 anchor)
        {
            climbHand = hand;
            climbAnchor = anchor;
            fallVelocity = 0f;
        }

        public void EndClimb(HandController hand)
        {
            if (climbHand != hand) return;
            var other = Other(hand);
            if (other && other.IsClimbing) { climbHand = other; climbAnchor = other.ClimbAnchor; return; }
            climbHand = null;
            fallStartY = transform.position.y;
            TryMantle();
        }

        // after letting go of a ladder top, step onto a nearby ledge instead of falling
        void TryMantle()
        {
            Vector3 h = head.transform.position;
            float below = Physics.Raycast(h, Vector3.down, out var direct, 6f, groundMask, QueryTriggerInteraction.Ignore) ? direct.point.y : h.y - 6f;
            Vector3 fwd = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) return;
            for (int i = 1; i <= 7; i++)
            {
                Vector3 probe = h + fwd * (0.15f * i);
                if (!Physics.Raycast(probe, Vector3.down, out var hit, 1.9f, groundMask, QueryTriggerInteraction.Ignore)) continue;
                if (!hit.collider.GetComponentInParent<TeleportSurface>()) continue;
                if (hit.point.y < below + 0.4f) continue;
                ScreenFade.Blink(0.2f);
                TeleportFeet(hit.point + fwd * 0.1f);
                return;
            }
        }

        void UpdateClimb()
        {
            Vector3 delta = climbAnchor - climbHand.GripPoint;
            if (delta.sqrMagnitude > 0.25f) delta = delta.normalized * 0.5f;
            transform.position += delta;
            Grounded = false;
            climbAnchor = climbHand.ClimbAnchor;
        }

        static readonly Vector3[] FootSamples =
        {
            Vector3.zero, new Vector3(0.2f, 0f, 0f), new Vector3(-0.2f, 0f, 0f), new Vector3(0f, 0f, 0.2f), new Vector3(0f, 0f, -0.2f)
        };
        readonly RaycastHit[] groundHits = new RaycastHit[16];

        // highest walkable surface under the head, sampled around it so edges are forgiving
        bool FindGround(out float ground)
        {
            ground = float.MinValue;
            Vector3 h = head.transform.position;
            foreach (var o in FootSamples)
            {
                int n = Physics.RaycastNonAlloc(h + o, Vector3.down, groundHits, 8f, groundMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    var hit = groundHits[i];
                    if (hit.normal.y < 0.7f || hit.point.y <= ground) continue;
                    if (!hit.collider.GetComponentInParent<TeleportSurface>()) continue;
                    ground = hit.point.y;
                }
            }
            return ground > float.MinValue;
        }

        void ApplyGravity(float dt)
        {
            // never fall when there is no floor below, just hold position
            if (!FindGround(out float ground)) { Land(); return; }
            float y = transform.position.y;
            if (y > ground + 0.02f) Fall(dt, ground);
            else
            {
                if (y < ground - 0.02f)
                {
                    // the head is above this floor, so the feet belong on it
                    if (ground - y > 0.45f) ScreenFade.Blink(0.2f);
                    SetY(ground);
                }
                Land();
            }
        }

        void Fall(float dt, float ground)
        {
            if (Grounded) fallStartY = transform.position.y;
            Grounded = false;
            fallVelocity = Mathf.Min(fallVelocity + 9.81f * dt, fallSpeedMax);
            float y = transform.position.y - fallVelocity * dt;
            if (y <= ground) { SetY(ground); Land(); }
            else SetY(y);
        }

        void Land()
        {
            if (!Grounded)
            {
                Grounded = true;
                if (fallStartY - transform.position.y > 0.8f)
                {
                    ScreenFade.Blink(0.25f);
                    Sfx.Play("lever_clunk", Feet, 0.5f);
                    Haptics.Both(0.4f, 0.12f);
                }
                Landed?.Invoke();
            }
            fallVelocity = 0f;
        }

        void SetY(float y)
        {
            var p = transform.position; p.y = y; transform.position = p;
        }

        bool IsPlayerPart(Collider c) => c.transform.IsChildOf(transform);

        // place feet at target, keep facing
        public void TeleportFeet(Vector3 target)
        {
            Vector3 offset = target - Feet;
            transform.position += new Vector3(offset.x, 0f, offset.z);
            SetY(target.y);
            Grounded = true;
            fallVelocity = 0f;
            Teleported?.Invoke(target);
        }

        // spawn helper, also aligns view direction
        public void PlaceAt(Vector3 feet, float yaw)
        {
            float headYaw = head.transform.eulerAngles.y;
            transform.Rotate(0f, yaw - headYaw, 0f, Space.World);
            TeleportFeet(feet);
        }

        public void Turn(float degrees)
        {
            transform.RotateAround(head.transform.position, Vector3.up, degrees);
        }

        public void MoveHorizontal(Vector3 delta)
        {
            delta.y = 0f;
            if (delta.sqrMagnitude < 1e-8f) return;
            Vector3 chest = Feet + Vector3.up * Mathf.Clamp(HeadHeight * 0.6f, 0.5f, 1.2f);
            if (Physics.SphereCast(chest, 0.22f, delta.normalized, out var hit, delta.magnitude + 0.05f, groundMask, QueryTriggerInteraction.Ignore) && !IsPlayerPart(hit.collider))
            {
                // slide along walls
                Vector3 slide = Vector3.ProjectOnPlane(delta, hit.normal);
                slide.y = 0f;
                if (Physics.SphereCast(chest, 0.22f, slide.normalized, out _, slide.magnitude + 0.05f, groundMask, QueryTriggerInteraction.Ignore)) return;
                delta = slide;
            }
            transform.position += delta;
        }
    }
}
