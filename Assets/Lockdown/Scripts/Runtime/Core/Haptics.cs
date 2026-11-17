using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Lockdown
{
    // controller vibration with two backends
    public static class Haptics
    {
        static InputAction leftAction, rightAction;
        static readonly List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
        public static bool Enabled = true;

        static void Init()
        {
            if (leftAction != null) return;
            leftAction = new InputAction("hapticL", InputActionType.PassThrough, "<XRController>{LeftHand}/{Haptic}");
            rightAction = new InputAction("hapticR", InputActionType.PassThrough, "<XRController>{RightHand}/{Haptic}");
            leftAction.Enable(); rightAction.Enable();
        }

        public static void Pulse(Hand hand, float amplitude, float seconds)
        {
            if (!Enabled || amplitude <= 0f) return;
            amplitude = Mathf.Clamp01(amplitude);
            Init();
            // send on both backends, a duplicate pulse is harmless
            try
            {
                var act = hand == Hand.Left ? leftAction : rightAction;
                if (act.controls.Count > 0)
                    UnityEngine.XR.OpenXR.Input.OpenXRInput.SendHapticImpulse(act, amplitude, seconds);
            }
            catch { }
            var node = hand == Hand.Left ? XRNode.LeftHand : XRNode.RightHand;
            devices.Clear();
            InputDevices.GetDevicesAtXRNode(node, devices);
            foreach (var d in devices)
                if (d.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
                    d.SendHapticImpulse(0, amplitude, seconds);
        }

        public static void Both(float amplitude, float seconds)
        {
            Pulse(Hand.Left, amplitude, seconds);
            Pulse(Hand.Right, amplitude, seconds);
        }

        public static void StopAll()
        {
            devices.Clear();
            InputDevices.GetDevices(devices);
            foreach (var d in devices) d.StopHaptics();
        }
    }
}
