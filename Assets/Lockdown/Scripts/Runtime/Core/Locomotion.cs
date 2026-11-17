using System.Collections.Generic;
using UnityEngine;

namespace Lockdown
{
    // right stick: snap turn and teleport arc, left stick: optional smooth walk
    public class Locomotion : MonoBehaviour
    {
        public PlayerRig rig;
        public LineRenderer arc;
        public Transform marker;
        public float arcSpeed = 7.5f;
        public float walkSpeed = 1.5f;
        [Tooltip("teleport cannot change floor height more than this")]
        public float maxStepHeight = 0.45f;
        [Tooltip("ignore stick noise right after a teleport so gravity and aim do not fight")]
        public float postTeleportLock = 0.12f;
        float teleportLockUntil;

        bool aiming, turnReady = true;
        bool validTarget;
        Vector3 target;
        readonly List<Vector3> points = new List<Vector3>();

        public bool Enabled { get; set; } = true;

        void Update()
        {
            if (!rig || rig.Frozen) { StopAim(); return; }
            var r = XrInput.Get(Hand.Right);
            Vector2 rs = r.Stick;
            if (Time.time < teleportLockUntil)
            {
                StopAim();
                ScreenFade.SetVignette(0f);
                return;
            }

            // snap turn
            if (!aiming && Mathf.Abs(rs.x) > 0.75f && Mathf.Abs(rs.y) < 0.6f)
            {
                if (turnReady && Enabled)
                {
                    turnReady = false;
                    rig.Turn(Mathf.Sign(rs.x) * GameSettings.SnapAngle);
                    ScreenFade.Blink(0.12f);
                }
            }
            else if (Mathf.Abs(rs.x) < 0.3f) turnReady = true;

            // push forward to aim, the jump happens once the stick is back at rest
            bool allowed = Enabled && !rig.Climbing;
            if (!aiming && allowed && rs.y > 0.65f) aiming = true;
            if (aiming)
            {
                if (!allowed || rs.y < -0.5f) StopAim();
                else if (rs.magnitude < 0.25f)
                {
                    if (validTarget) Go();
                    StopAim();
                }
                else Aim();
            }

            // smooth walk on the left stick
            if (Enabled && GameSettings.SmoothMove && !rig.Climbing)
            {
                Vector2 ls = XrInput.Get(Hand.Left).Stick;
                if (ls.sqrMagnitude > 0.04f)
                {
                    Vector3 fwd = Vector3.ProjectOnPlane(rig.head.transform.forward, Vector3.up).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, fwd);
                    Vector3 move = (fwd * ls.y + right * ls.x) * walkSpeed * Time.deltaTime;
                    rig.MoveHorizontal(move);
                    if (GameSettings.Vignette) ScreenFade.SetVignette(Mathf.Clamp01(ls.magnitude));
                }
                else ScreenFade.SetVignette(0f);
            }
            else ScreenFade.SetVignette(0f);
        }

        void Aim()
        {
            aiming = true;
            rig.right.LocomotionAiming = true;
            Transform src = rig.right.aim ? rig.right.aim : rig.right.transform;
            points.Clear();
            Vector3 p = src.position;
            Vector3 v = src.forward * arcSpeed;
            validTarget = false;
            points.Add(p);
            for (int i = 0; i < 40; i++)
            {
                float dt = 0.04f;
                Vector3 nextP = p + v * dt;
                v += Physics.gravity * dt;
                if (Physics.Linecast(p, nextP, out var hit, ~0, QueryTriggerInteraction.Ignore))
                {
                    points.Add(hit.point);
                    validTarget = IsValid(hit);
                    target = hit.point;
                    break;
                }
                points.Add(nextP);
                p = nextP;
                if (p.y < rig.transform.position.y - 3f) break;
            }
            if (arc)
            {
                arc.enabled = true;
                arc.positionCount = points.Count;
                arc.SetPositions(points.ToArray());
                Color c = validTarget ? new Color(0.3f, 1f, 0.75f, 0.9f) : new Color(1f, 0.3f, 0.25f, 0.6f);
                arc.startColor = c; arc.endColor = c;
            }
            if (marker)
            {
                marker.gameObject.SetActive(validTarget);
                if (validTarget) marker.position = target + Vector3.up * 0.01f;
            }
        }

        bool IsValid(RaycastHit hit)
        {
            if (hit.normal.y < 0.75f) return false;
            if (!hit.collider.GetComponentInParent<TeleportSurface>()) return false;
            if (Mathf.Abs(hit.point.y - rig.transform.position.y) > maxStepHeight) return false;
            // body must fit at the target
            Vector3 bottom = hit.point + Vector3.up * 0.35f;
            Vector3 top = hit.point + Vector3.up * 1.6f;
            if (Physics.CheckCapsule(bottom, top, 0.2f, ~0, QueryTriggerInteraction.Ignore)) return false;
            return true;
        }

        void Go()
        {
            Sfx.Play("teleport_whoosh", target, 0.5f);
            ScreenFade.Blink(0.18f);
            rig.TeleportFeet(target);
            teleportLockUntil = Time.time + postTeleportLock;
        }

        void StopAim()
        {
            if (!aiming) return;
            aiming = false;
            if (rig && rig.right) rig.right.LocomotionAiming = false;
            if (arc) arc.enabled = false;
            if (marker) marker.gameObject.SetActive(false);
        }
    }
}
