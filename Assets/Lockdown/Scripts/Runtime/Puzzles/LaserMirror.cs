using UnityEngine;

namespace Lockdown
{
    // mirror on a turntable, its forward is the reflective face
    public class LaserMirror : MonoBehaviour
    {
        public Transform face;
        public RotaryControl turntable;

        public Vector3 Normal => (face ? face : transform).forward;

        void Start()
        {
            if (turntable) turntable.onStep.AddListener(_ =>
            {
                if (GameDirector.I) { GameDirector.I.MirrorSteps++; GameDirector.I.Engage(); }
            });
        }
    }
}
