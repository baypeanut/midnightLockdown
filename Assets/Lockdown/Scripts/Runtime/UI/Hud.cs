using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Lockdown
{
    // head following caption strip and toast messages
    public class Hud : MonoBehaviour
    {
        public static Hud I { get; private set; }
        public float distance = 1.15f;
        public float followSpeed = 4.2f;
        public float verticalBias = -0.08f;

        TextMeshPro caption, toast;
        Transform captionPanel, toastPanel;
        float captionUntil, toastUntil;
        readonly Queue<(string, float, Color)> toasts = new Queue<(string, float, Color)>();

        void Awake()
        {
            I = this;
            captionPanel = UiKit.Panel(transform, new Vector2(0.78f, 0.12f), new Vector3(0f, -0.32f, 0f), "CaptionBg");
            caption = UiKit.Text(transform, "", new Vector3(0f, -0.32f, -0.002f), new Vector2(0.74f, 0.11f), 0.026f);
            toastPanel = UiKit.Panel(transform, new Vector2(0.62f, 0.09f), new Vector3(0f, 0.2f, 0f), "ToastBg");
            toast = UiKit.Text(transform, "", new Vector3(0f, 0.2f, -0.002f), new Vector2(0.58f, 0.085f), 0.024f, TextAlignmentOptions.Center, UiKit.Accent);
            // overlay materials keep captions readable when a wall is close
            foreach (var panel in new[] { captionPanel, toastPanel })
                if (UiKit.HudPanelMaterial) panel.GetComponent<Renderer>().sharedMaterial = UiKit.HudPanelMaterial;
            foreach (var text in new[] { caption, toast })
                if (UiKit.HudTextMaterial) text.fontSharedMaterial = UiKit.HudTextMaterial;
            SetCaption(null, 0f);
            toastPanel.gameObject.SetActive(false);
            toast.gameObject.SetActive(false);
        }

        void OnDestroy() { if (I == this) I = null; }

        public void SetCaption(string text, float seconds)
        {
            bool on = !string.IsNullOrEmpty(text);
            caption.text = on ? text : "";
            captionPanel.gameObject.SetActive(on);
            caption.gameObject.SetActive(on);
            captionUntil = Time.unscaledTime + seconds;
        }

        public void Toast(string text, float seconds = 3f, Color? color = null)
        {
            toasts.Enqueue((text, seconds, color ?? UiKit.Accent));
        }

        void LateUpdate()
        {
            // captions step aside while the pause menu is open
            bool hidden = PauseMenu.I && PauseMenu.I.IsOpen;
            transform.localScale = hidden ? Vector3.zero : Vector3.one;
            var rig = PlayerRig.I;
            if (rig)
            {
                var head = rig.head.transform;
                Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                if (fwd.sqrMagnitude > 0.01f)
                {
                    fwd.Normalize();
                    float d = distance;
                    if (Physics.Raycast(head.position, fwd, out var wall, distance + 0.1f, ~0, QueryTriggerInteraction.Ignore))
                        d = Mathf.Clamp(wall.distance - 0.1f, 0.6f, distance);
                    Vector3 targetPos = head.position + fwd * d + Vector3.up * verticalBias;
                    Quaternion targetRot = Quaternion.LookRotation(fwd, Vector3.up);
                    float k = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
                    if (Vector3.Distance(transform.position, targetPos) > 2f) k = 1f;
                    transform.position = Vector3.Lerp(transform.position, targetPos, k);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, k);
                }
            }
            if (caption.gameObject.activeSelf && Time.unscaledTime > captionUntil) SetCaption(null, 0f);
            if (Time.unscaledTime > toastUntil)
            {
                if (toasts.Count > 0)
                {
                    var (txt, secs, col) = toasts.Dequeue();
                    toast.text = txt;
                    toast.color = col;
                    toastUntil = Time.unscaledTime + secs;
                    toastPanel.gameObject.SetActive(true);
                    toast.gameObject.SetActive(true);
                }
                else if (toast.gameObject.activeSelf)
                {
                    toastPanel.gameObject.SetActive(false);
                    toast.gameObject.SetActive(false);
                }
            }
        }
    }
}
