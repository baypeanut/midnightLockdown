using UnityEngine;

namespace Lockdown
{
    // idle rotation and bob for decorative props
    public class Spinner : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0f, 30f, 0f);
        public float bobHeight = 0f;
        public float bobSpeed = 1f;
        Vector3 start;

        void Awake() => start = transform.localPosition;

        void Update()
        {
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
            if (bobHeight > 0f) transform.localPosition = start + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        }
    }
}
