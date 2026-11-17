using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Lockdown
{
    // builds world space panels, labels and buttons at runtime
    public static class UiKit
    {
        public static readonly Color Ink = new Color(0.86f, 0.95f, 1f);
        public static readonly Color Dim = new Color(0.55f, 0.66f, 0.72f);
        public static readonly Color Accent = new Color(0.25f, 0.9f, 1f);
        public static readonly Color Warn = new Color(1f, 0.38f, 0.3f);
        public static readonly Color Good = new Color(0.4f, 1f, 0.55f);

        static Material panelMat, buttonMat, buttonHotMat;

        static Material Load(ref Material m, string name)
        {
            if (!m) m = Resources.Load<Material>("Ui/" + name);
            return m;
        }

        public static Material PanelMaterial => Load(ref panelMat, "Panel");
        public static Material ButtonMaterial => Load(ref buttonMat, "Button");
        public static Material ButtonHotMaterial => Load(ref buttonHotMat, "ButtonHot");
        static Material hudPanelMat, hudTextMat;
        public static Material HudPanelMaterial => Load(ref hudPanelMat, "HudPanel");
        public static Material HudTextMaterial => Load(ref hudTextMat, "HudText");

        public static Transform Root(Transform parent, string name, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        public static Transform Panel(Transform parent, Vector2 size, Vector3 localPos, string name = "Panel")
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = name;
            q.transform.SetParent(parent, false);
            q.transform.localPosition = localPos + new Vector3(0f, 0f, 0.004f);
            q.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = PanelMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return q.transform;
        }

        public static TextMeshPro Text(Transform parent, string text, Vector3 localPos, Vector2 size, float heightMeters,
            TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = heightMeters * 10f;
            tmp.alignment = align;
            tmp.color = color ?? Ink;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.rectTransform.sizeDelta = size;
            tmp.richText = true;
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return tmp;
        }

        public static Pressable Button(Transform parent, string label, Vector3 localPos, Vector2 size, UnityAction onClick, float textHeight = 0.022f)
        {
            var root = new GameObject("Button " + label);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "Cap";
            cap.transform.SetParent(root.transform, false);
            cap.transform.localScale = new Vector3(size.x, size.y, 0.012f);
            var r = cap.GetComponent<Renderer>();
            r.sharedMaterial = ButtonMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var col = (BoxCollider)cap.GetComponent<Collider>();
            col.size = new Vector3(1f, 1f, 2.5f);
            var t = Text(cap.transform, label, new Vector3(0f, 0f, -0.55f), new Vector2(size.x * 0.94f, size.y), textHeight);
            t.transform.localScale = new Vector3(1f / size.x, 1f / size.y, 1f);
            t.rectTransform.sizeDelta = new Vector2(size.x * 0.94f, size.y);
            var p = root.AddComponent<Pressable>();
            p.cap = cap.transform;
            p.capTravel = 0.006f;
            if (onClick != null) p.onPress.AddListener(onClick);
            var hover = root.AddComponent<ButtonGlow>();
            hover.target = r;
            return p;
        }

        public static void SetLabel(Pressable p, string label)
        {
            var t = p.GetComponentInChildren<TextMeshPro>();
            if (t) t.text = label;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                Object.Destroy(c);
            }
        }

        static readonly float[] TryAngles = { 0f, -40f, 40f, -80f, 80f, -120f, 120f, 180f };

        // place a panel near the head in the first spot where the whole panel fits and can be seen
        public static void PlaceInFront(Transform panel, float distance, float drop = 0.15f)
        {
            var rig = PlayerRig.I;
            if (!rig) return;
            var head = rig.head.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.ProjectOnPlane(head.up, Vector3.up);
            fwd.Normalize();
            var half = new Vector3(0.4f, 0.38f, 0.08f);
            float angle = 0f, dist = 0.5f;
            bool found = false;
            foreach (float d in new[] { distance, distance * 0.7f })
            {
                foreach (float a in TryAngles)
                {
                    Vector3 dir = Quaternion.AngleAxis(a, Vector3.up) * fwd;
                    Vector3 center = head.position + dir * d + Vector3.down * drop;
                    if (Physics.CheckBox(center, half, Quaternion.LookRotation(dir), ~0, QueryTriggerInteraction.Ignore)) continue;
                    // the middle and both side edges must be visible from the head
                    Vector3 side = Vector3.Cross(Vector3.up, dir) * 0.36f;
                    if (Physics.Linecast(head.position, center, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (Physics.Linecast(head.position, center + side, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (Physics.Linecast(head.position, center - side, ~0, QueryTriggerInteraction.Ignore)) continue;
                    angle = a; dist = d; found = true;
                    break;
                }
                if (found) break;
            }
            Vector3 best = Quaternion.AngleAxis(angle, Vector3.up) * fwd;
            panel.position = head.position + best * dist + Vector3.down * drop;
            panel.rotation = Quaternion.LookRotation(panel.position - head.position, Vector3.up);
            if (Mathf.Abs(angle) > 60f && Hud.I)
                Hud.I.Toast(Mathf.Abs(angle) > 150f ? "The panel is behind you" : angle > 0f ? "The panel is on your right" : "The panel is on your left", 4f);
        }
    }
}
