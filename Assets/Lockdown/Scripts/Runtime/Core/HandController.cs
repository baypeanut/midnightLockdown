using System.Collections.Generic;
using UnityEngine;

namespace Lockdown
{
    // one tracked controller: grab, climb, point, poke
    [DefaultExecutionOrder(-40)]
    public class HandController : MonoBehaviour
    {
        public Hand hand;
        public Transform aim;
        public LineRenderer ray;
        public Transform reticle;
        public float grabRadius = 0.075f;
        public float rayLength = 6f;
        public float distanceGrabRange = 4f;

        public HandInput Input => XrInput.Get(hand);
        public Vector3 GripPoint => transform.TransformPoint(new Vector3(0f, -0.01f, 0.025f));
        public Vector3 Fingertip => transform.TransformPoint(new Vector3(hand == Hand.Left ? 0.012f : -0.012f, -0.02f, 0.11f));
        public Vector3 Velocity { get; private set; }
        public Vector3 AngularVelocity { get; private set; }
        public IGrabHandle Held { get; private set; }
        public bool IsClimbing { get; private set; }
        public Vector3 ClimbAnchor => climbAnchor;
        public bool PointerActive { get; set; } = true;
        public bool LocomotionAiming { get; set; }
        public HandVisual visual;
        // editor preview pose, unused on device
        [System.NonSerialized] public bool simulatePose;
        [System.NonSerialized] public Vector3 simLocalPosition;

        Vector3 climbAnchor, lastPos;
        Quaternion lastRot;
        IGrabHandle hover;
        Pressable rayTarget, pokeTarget, rayPressed;
        Grabbable pulling;
        float pullT;
        Vector3 pullFrom;
        readonly Collider[] hits = new Collider[24];
        static readonly List<HandController> all = new List<HandController>();
        public static IReadOnlyList<HandController> All => all;

        void OnEnable() { all.Add(this); }
        void OnDisable()
        {
            all.Remove(this);
            ReleaseAll();
        }

        void Start()
        {
            lastPos = transform.position;
            lastRot = transform.rotation;
        }

        void Update()
        {
            UpdatePose();
            var inp = Input;
            if (!inp.Tracked) { ReleaseAll(); SetRay(false, Vector3.zero, Vector3.zero, false); return; }

            UpdateVelocity();
            UpdateHover();
            if (Held != null) Held.UpdateGrab(this);
            UpdatePull();

            if (inp.GripDown) OnGripDown();
            if (inp.GripUp || (!inp.GripHeld && (Held != null || IsClimbing))) OnGripUp();

            UpdatePoke();
            UpdateRay(inp);
            if (visual) visual.SetCurl(Mathf.Max(inp.Grip, Held != null || IsClimbing ? 1f : 0f), inp.Trigger, pokeTarget != null);
        }

        void UpdatePose()
        {
            var inp = Input;
            if (simulatePose) { transform.localPosition = simLocalPosition; return; }
            transform.localPosition = inp.GripPosition;
            transform.localRotation = inp.GripRotation;
            if (aim)
            {
                aim.localPosition = transform.InverseTransformPoint(transform.parent.TransformPoint(inp.AimPosition));
                aim.rotation = transform.parent.rotation * inp.AimRotation;
            }
        }

        void UpdateVelocity()
        {
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            Vector3 v = (transform.position - lastPos) / dt;
            Velocity = Vector3.Lerp(Velocity, v, 0.5f);
            Quaternion dq = transform.rotation * Quaternion.Inverse(lastRot);
            dq.ToAngleAxis(out float ang, out Vector3 axis);
            if (ang > 180f) ang -= 360f;
            AngularVelocity = float.IsNaN(axis.x) ? Vector3.zero : axis * (ang * Mathf.Deg2Rad / dt);
            lastPos = transform.position;
            lastRot = transform.rotation;
        }

        // nearest handle in reach
        void UpdateHover()
        {
            IGrabHandle best = null;
            if (Held == null && !IsClimbing)
            {
                int n = Physics.OverlapSphereNonAlloc(GripPoint, grabRadius, hits, ~0, QueryTriggerInteraction.Collide);
                float bestScore = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    var h = hits[i].GetComponentInParent<IGrabHandle>();
                    if (h == null || !h.CanGrab(this)) continue;
                    float d = (hits[i].ClosestPoint(GripPoint) - GripPoint).sqrMagnitude - h.Priority * 0.01f;
                    if (d < bestScore) { bestScore = d; best = h; }
                }
            }
            if (best != hover)
            {
                if (hover is Grabbable og) og.SetHighlight(false);
                hover = best;
                if (hover is Grabbable ng) ng.SetHighlight(true);
                if (hover != null) Haptics.Pulse(hand, 0.08f, 0.02f);
            }
        }

        Climbable FindClimbable(out Vector3 point)
        {
            point = GripPoint;
            int n = Physics.OverlapSphereNonAlloc(GripPoint, grabRadius + 0.02f, hits, ~0, QueryTriggerInteraction.Collide);
            Climbable best = null;
            float bd = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var c = hits[i].GetComponentInParent<Climbable>();
                if (!c || !c.enabledForClimb) continue;
                Vector3 p = hits[i].ClosestPoint(GripPoint);
                float d = (p - GripPoint).sqrMagnitude;
                if (d < bd) { bd = d; best = c; point = p; }
            }
            return best;
        }

        void OnGripDown()
        {
            if (hover != null && hover.CanGrab(this))
            {
                Grab(hover);
                return;
            }
            var climb = FindClimbable(out _);
            if (climb)
            {
                IsClimbing = true;
                climbAnchor = GripPoint;
                PlayerRig.I?.BeginClimb(this, climbAnchor);
                Sfx.Play("climb_grip", GripPoint, 0.7f);
                Haptics.Pulse(hand, 0.35f, 0.06f);
                return;
            }
            // distance grab along the pointer
            if (aim && Physics.Raycast(aim.position, aim.forward, out var hit, distanceGrabRange, ~0, QueryTriggerInteraction.Ignore))
            {
                var g = hit.collider.GetComponentInParent<Grabbable>();
                if (g && g.allowDistanceGrab && g.CanGrab(this))
                {
                    pulling = g;
                    pullT = 0f;
                    pullFrom = g.transform.position;
                    g.BeginPull();
                    Sfx.Play("teleport_whoosh", g.transform.position, 0.4f);
                }
            }
        }

        void UpdatePull()
        {
            if (!pulling) return;
            if (!Input.GripHeld || pulling.IsHeld) { pulling.EndPull(); pulling = null; return; }
            pullT += Time.deltaTime / 0.22f;
            pulling.transform.position = Vector3.Lerp(pullFrom, GripPoint, Mathf.SmoothStep(0f, 1f, pullT));
            if (pullT >= 1f)
            {
                var g = pulling;
                pulling = null;
                g.EndPull();
                if (g.CanGrab(this)) Grab(g);
            }
        }

        public void Grab(IGrabHandle h)
        {
            if (Held != null) Release();
            // steal from the other hand
            foreach (var other in all)
                if (other != this && other.Held == h) other.Release();
            Held = h;
            h.BeginGrab(this);
            Haptics.Pulse(hand, 0.25f, 0.05f);
        }

        public void Release()
        {
            if (Held == null) return;
            var h = Held;
            Held = null;
            h.EndGrab(this);
        }

        void OnGripUp()
        {
            if (Held != null) Release();
            if (IsClimbing)
            {
                IsClimbing = false;
                PlayerRig.I?.EndClimb(this);
            }
        }

        public void ReleaseAll()
        {
            if (pulling) { pulling.EndPull(); pulling = null; }
            if (Held != null) Release();
            if (IsClimbing) { IsClimbing = false; PlayerRig.I?.EndClimb(this); }
            if (pokeTarget) { pokeTarget.PokeExit(this); pokeTarget = null; }
            if (rayPressed) { rayPressed.RayRelease(this); rayPressed = null; }
        }

        void UpdatePoke()
        {
            Pressable found = null;
            if (Held == null && !IsClimbing)
            {
                int n = Physics.OverlapSphereNonAlloc(Fingertip, 0.012f, hits, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < n && !found; i++)
                {
                    var p = hits[i].GetComponentInParent<Pressable>();
                    if (p && p.allowPoke && p.Interactable) found = p;
                }
            }
            if (found != pokeTarget)
            {
                if (pokeTarget) pokeTarget.PokeExit(this);
                pokeTarget = found;
                if (pokeTarget) pokeTarget.PokeEnter(this);
            }
        }

        void UpdateRay(HandInput inp)
        {
            rayTarget = null;
            if (!aim || !PointerActive || Held != null || IsClimbing || LocomotionAiming)
            {
                if (rayPressed) { rayPressed.RayRelease(this); rayPressed = null; }
                SetRay(false, Vector3.zero, Vector3.zero, false);
                return;
            }
            bool hitSomething = Physics.Raycast(aim.position, aim.forward, out var hit, rayLength, ~0, QueryTriggerInteraction.Collide);
            bool interesting = false;
            if (hitSomething)
            {
                var p = hit.collider.GetComponentInParent<Pressable>();
                if (p && p.allowRay && p.Interactable) { rayTarget = p; interesting = true; }
                else if (hit.collider.isTrigger)
                {
                    // look past trigger volumes
                    hitSomething = Physics.Raycast(aim.position, aim.forward, out hit, rayLength, ~0, QueryTriggerInteraction.Ignore);
                }
                if (!interesting && hitSomething)
                {
                    var g = hit.collider.GetComponentInParent<Grabbable>();
                    interesting = g && g.allowDistanceGrab && g.CanGrab(this) && hit.distance <= distanceGrabRange;
                }
            }
            if (rayTarget && inp.TriggerDown) { rayPressed = rayTarget; rayTarget.RayPress(this); Haptics.Pulse(hand, 0.2f, 0.03f); }
            if (rayPressed && (!inp.TriggerHeld || rayPressed != rayTarget)) { rayPressed.RayRelease(this); rayPressed = null; }
            bool show = interesting || inp.Trigger > 0.2f || UiFocus.Active;
            Vector3 end = hitSomething ? hit.point : aim.position + aim.forward * rayLength;
            SetRay(show, aim.position, end, interesting);
        }

        void SetRay(bool on, Vector3 a, Vector3 b, bool hot)
        {
            if (ray)
            {
                ray.enabled = on;
                if (on)
                {
                    ray.SetPosition(0, a);
                    ray.SetPosition(1, b);
                    Color c = hot ? new Color(0.35f, 0.95f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.35f);
                    ray.startColor = c;
                    ray.endColor = new Color(c.r, c.g, c.b, 0.05f);
                }
            }
            if (reticle)
            {
                reticle.gameObject.SetActive(on && hot);
                reticle.position = b;
            }
        }
    }
}
