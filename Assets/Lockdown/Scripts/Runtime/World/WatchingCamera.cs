using UnityEngine;

namespace Lockdown
{
    // security camera head that follows the player while warden is in control
    public class WatchingCamera : MonoBehaviour
    {
        public Transform yaw;
        public float speed = 1.6f;
        public float maxYaw = 70f;
        public Renderer led;
        Quaternion rest;

        void Awake()
        {
            if (!yaw) yaw = transform;
            rest = yaw.localRotation;
        }

        void Update()
        {
            var rig = PlayerRig.I;
            var gd = GameDirector.I;
            bool tracking = rig && (!gd || gd.State != GameState.Won);
            Quaternion target = rest;
            if (tracking)
            {
                Vector3 local = yaw.parent ? yaw.parent.InverseTransformPoint(rig.head.transform.position) : rig.head.transform.position;
                Quaternion look = Quaternion.LookRotation(local - yaw.localPosition, Vector3.up);
                float a = Quaternion.Angle(rest, look);
                target = a > maxYaw ? Quaternion.RotateTowards(rest, look, maxYaw) : look;
            }
            yaw.localRotation = Quaternion.Slerp(yaw.localRotation, target, 1f - Mathf.Exp(-speed * Time.deltaTime));
            if (led) led.enabled = tracking && (GameSettings.ReduceFlashing || Mathf.Repeat(Time.time, 1f) < 0.6f);
        }
    }
}
