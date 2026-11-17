using UnityEngine;

namespace Lockdown
{
    // simple low poly glove built from primitives, curls with grip
    public class HandVisual : MonoBehaviour
    {
        public Material material;
        public bool isLeft;
        Transform[] fingers = new Transform[4];
        Transform index, thumb;
        Quaternion[] rest = new Quaternion[6];

        void Awake() => Build();

        void Build()
        {
            if (transform.childCount > 0) return;
            float side = isLeft ? 1f : -1f;
            var palm = Part(PrimitiveType.Cube, transform, new Vector3(0f, -0.012f, -0.01f), new Vector3(0.075f, 0.025f, 0.085f));
            palm.name = "palm";
            for (int i = 0; i < 4; i++)
            {
                var root = new GameObject("finger" + i).transform;
                root.SetParent(transform, false);
                root.localPosition = new Vector3((i - 1.5f) * 0.018f * -side, -0.005f, 0.035f);
                var seg = Part(PrimitiveType.Capsule, root, new Vector3(0f, 0f, 0.03f), new Vector3(0.016f, 0.03f, 0.016f));
                seg.localRotation = Quaternion.Euler(90f, 0f, 0f);
                fingers[i] = root;
                rest[i] = root.localRotation;
            }
            index = fingers[0];
            thumb = new GameObject("thumb").transform;
            thumb.SetParent(transform, false);
            thumb.localPosition = new Vector3(0.035f * side, -0.012f, 0.0f);
            thumb.localRotation = Quaternion.Euler(0f, -40f * side, 0f);
            var t = Part(PrimitiveType.Capsule, thumb, new Vector3(0f, 0f, 0.025f), new Vector3(0.018f, 0.026f, 0.018f));
            t.localRotation = Quaternion.Euler(90f, 0f, 0f);
            rest[4] = thumb.localRotation;
        }

        Transform Part(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col) Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (material) r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        // grip curls three fingers, trigger curls the index, poke points it
        public void SetCurl(float grip, float trigger, bool pointing)
        {
            for (int i = 1; i < 4; i++)
                if (fingers[i]) fingers[i].localRotation = rest[i] * Quaternion.Euler(grip * 85f, 0f, 0f);
            if (index) index.localRotation = rest[0] * Quaternion.Euler((pointing ? 0f : Mathf.Max(trigger, grip * 0.6f)) * 80f, 0f, 0f);
            if (thumb) thumb.localRotation = rest[4] * Quaternion.Euler(0f, 0f, grip * 30f * (isLeft ? -1f : 1f));
        }
    }
}
