using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Lockdown
{
    // receptacle that pulls a matching item into place
    public class ItemSocket : MonoBehaviour
    {
        public string[] acceptIds = new string[0];
        [Tooltip("ids that count as an attempt but get rejected")]
        public string[] rejectIds = new string[0];
        public float captureRadius = 0.14f;
        public bool lockOnInsert = true;
        public bool alignRotation = true;
        public GameObject ghost;
        public string insertSound = "fuse_insert";
        public UnityEvent<Grabbable> onInserted = new UnityEvent<Grabbable>();
        public UnityEvent<Grabbable> onRejected = new UnityEvent<Grabbable>();
        public UnityEvent onRemoved = new UnityEvent();

        public Grabbable Occupant { get; private set; }
        public bool Filled => Occupant != null;
        public bool Enabled { get; set; } = true;

        static readonly List<ItemSocket> all = new List<ItemSocket>();

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        void Update()
        {
            if (!ghost) return;
            bool show = false;
            if (!Filled && Enabled)
                foreach (var h in HandController.All)
                    if (h.Held is Grabbable g && Accepts(g.itemId) && Vector3.Distance(g.transform.position, transform.position) < 0.6f) show = true;
            if (ghost.activeSelf != show) ghost.SetActive(show);
        }

        public bool Accepts(string id)
        {
            foreach (var a in acceptIds) if (a == id) return true;
            return false;
        }

        bool Rejects(string id)
        {
            foreach (var a in rejectIds) if (a == id) return true;
            return false;
        }

        // called when any item is let go
        public static bool TryCapture(Grabbable g)
        {
            ItemSocket best = null;
            float bd = float.MaxValue;
            foreach (var s in all)
            {
                if (!s.Enabled || s.Filled) continue;
                float d = Vector3.Distance(g.transform.position, s.transform.position);
                if (d > s.captureRadius || d >= bd) continue;
                if (!s.Accepts(g.itemId) && !s.Rejects(g.itemId)) continue;
                best = s; bd = d;
            }
            if (!best) return false;
            if (!best.Accepts(g.itemId))
            {
                best.Reject(g);
                return true;
            }
            best.Insert(g);
            return true;
        }

        public void Insert(Grabbable g)
        {
            Occupant = g;
            g.Socket = this;
            g.Freeze();
            g.Locked = lockOnInsert;
            if (ghost) ghost.SetActive(false);
            StartCoroutine(SnapIn(g));
        }

        IEnumerator SnapIn(Grabbable g)
        {
            Vector3 p0 = g.transform.position;
            Quaternion r0 = g.transform.rotation;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.15f)
            {
                if (Occupant != g) yield break;
                g.transform.position = Vector3.Lerp(p0, transform.position, t);
                if (alignRotation) g.transform.rotation = Quaternion.Slerp(r0, transform.rotation, t);
                yield return null;
            }
            g.transform.position = transform.position;
            if (alignRotation) g.transform.rotation = transform.rotation;
            g.transform.SetParent(transform, true);
            Sfx.Play(insertSound, transform.position, 0.8f);
            onInserted.Invoke(g);
        }

        void Reject(Grabbable g)
        {
            g.Freeze();
            StartCoroutine(PushOut(g));
            Sfx.Play("beep_error", transform.position, 0.7f);
            onRejected.Invoke(g);
            GameEvents.Mistake();
        }

        IEnumerator PushOut(Grabbable g)
        {
            yield return null;
            var rb = g.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.linearVelocity = -transform.forward * 1.2f + Vector3.up * 0.8f;
        }

        public void Detach(Grabbable g)
        {
            if (Occupant != g) return;
            Occupant = null;
            g.Socket = null;
            g.transform.SetParent(null, true);
            onRemoved.Invoke();
        }
    }
}
