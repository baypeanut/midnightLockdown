using UnityEngine;
using UnityEngine.InputSystem;

namespace Lockdown
{
    public enum Hand { Left = 0, Right = 1 }

    // controller input read straight from the input system
    public class HandInput
    {
        public readonly Hand hand;
        readonly InputAction gripPos, gripRot, aimPos, aimRot, tracked;
        readonly InputAction grip, trigger, stick, stickClick, primary, secondary;

        bool gripWas, triggerWas, primaryWas, secondaryWas, clickWas;
        public bool GripDown, GripUp, TriggerDown, TriggerUp, PrimaryDown, SecondaryDown, StickClickDown;

        public HandInput(Hand h)
        {
            hand = h;
            string d = h == Hand.Left ? "<XRController>{LeftHand}" : "<XRController>{RightHand}";
            gripPos = Make(d + "/devicePosition", "Vector3");
            gripRot = Make(d + "/deviceRotation", "Quaternion");
            aimPos = Make(d + "/pointerPosition", "Vector3");
            aimRot = Make(d + "/pointerRotation", "Quaternion");
            tracked = Make(d + "/isTracked", null);
            grip = Make(d + "/{Grip}", "Axis");
            trigger = Make(d + "/{Trigger}", "Axis");
            stick = Make(d + "/{Primary2DAxis}", "Vector2");
            stickClick = Make(d + "/{Primary2DAxisClick}", "Button");
            primary = Make(d + "/{PrimaryButton}", "Button");
            secondary = Make(d + "/{SecondaryButton}", "Button");
        }

        static InputAction Make(string path, string type)
        {
            var a = new InputAction(type: InputActionType.PassThrough, binding: path, expectedControlType: type);
            a.Enable();
            return a;
        }

        // read the bound control itself so action phases never matter
        static T Read<T>(InputAction a) where T : struct
        {
            var list = a.controls;
            for (int i = 0; i < list.Count; i++)
                if (list[i] is InputControl<T> c && c.device.added) return c.ReadValue();
            return default;
        }

        static bool Bound(InputAction a) => a.controls.Count > 0;

        public Vector3 GripPosition => Read<Vector3>(gripPos);
        public Quaternion GripRotation => Norm(Read<Quaternion>(gripRot));
        public Vector3 AimPosition => Bound(aimPos) ? Read<Vector3>(aimPos) : GripPosition;
        public Quaternion AimRotation => Bound(aimRot) ? Norm(Read<Quaternion>(aimRot)) : GripRotation;
        public bool Tracked => Bound(gripPos) && (!Bound(tracked) || Read<float>(tracked) > 0.5f);
        public float Grip => Read<float>(grip);
        public float Trigger => Read<float>(trigger);
        public Vector2 Stick => Read<Vector2>(stick);
        public bool GripHeld => Grip > (gripWas ? 0.35f : 0.6f);
        public bool TriggerHeld => Trigger > (triggerWas ? 0.35f : 0.6f);

        static Quaternion Norm(Quaternion q)
        {
            float m = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            return m < 1e-4f ? Quaternion.identity : new Quaternion(q.x / m, q.y / m, q.z / m, q.w / m);
        }

        // edge detection once per frame
        public void Tick()
        {
            bool g = GripHeld, t = TriggerHeld;
            bool p = Read<float>(primary) > 0.5f, s = Read<float>(secondary) > 0.5f, c = Read<float>(stickClick) > 0.5f;
            GripDown = g && !gripWas; GripUp = !g && gripWas;
            TriggerDown = t && !triggerWas; TriggerUp = !t && triggerWas;
            PrimaryDown = p && !primaryWas; SecondaryDown = s && !secondaryWas; StickClickDown = c && !clickWas;
            gripWas = g; triggerWas = t; primaryWas = p; secondaryWas = s; clickWas = c;
        }
    }

    public static class XrInput
    {
        static HandInput left, right;
        static int lastTick = -1;

        public static HandInput Get(Hand h)
        {
            if (left == null) { left = new HandInput(Hand.Left); right = new HandInput(Hand.Right); }
            if (lastTick != Time.frameCount)
            {
                lastTick = Time.frameCount;
                left.Tick(); right.Tick();
            }
            return h == Hand.Left ? left : right;
        }
    }
}
