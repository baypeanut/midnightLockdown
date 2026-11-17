using UnityEngine;

namespace Lockdown
{
    // box volume that reports when the player head enters it
    public class ZoneTrigger : MonoBehaviour
    {
        public string zoneId;
        public Vector3 size = Vector3.one;
        public bool once = true;
        bool inside, fired;

        void Update()
        {
            var rig = PlayerRig.I;
            if (!rig || (once && fired)) return;
            Vector3 local = transform.InverseTransformPoint(rig.head.transform.position);
            bool now = Mathf.Abs(local.x) < size.x * 0.5f && Mathf.Abs(local.y) < size.y * 0.5f && Mathf.Abs(local.z) < size.z * 0.5f;
            if (now && !inside)
            {
                fired = true;
                GameEvents.ZoneEntered(zoneId);
            }
            inside = now;
        }

        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
