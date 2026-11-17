using System.Collections.Generic;
using UnityEngine;

namespace Lockdown
{
    // puzzle 4: battery powered laser traced through mirrors to the vault sensor
    public class LaserEmitter : MonoBehaviour
    {
        public ItemSocket batterySocket;
        public Transform muzzle;
        public LineRenderer beam;
        public ParticleSystem impact;
        public LaserSensor sensor;
        public DissolveShutter shutter;
        public Renderer powerLamp;
        public int maxBounces = 8;
        public float maxDistance = 30f;

        public bool Powered { get; private set; }
        public int LastBounceCount { get; private set; }
        readonly List<Vector3> pts = new List<Vector3>();
        AudioSource hum;
        float sparkTimer;

        void Start()
        {
            if (batterySocket) batterySocket.onInserted.AddListener(_ => SetPowered(true));
            if (sensor) sensor.onUnlocked.AddListener(OnUnlocked);
            hum = Sfx.Loop(gameObject, "laser_hum_loop", 0.35f, 6f);
            hum.Stop();
            SetPowered(false);
        }

        public void SetPowered(bool on)
        {
            Powered = on;
            if (beam) beam.enabled = on;
            if (on) { hum.Play(); Sfx.Play("power_up", transform.position, 0.6f); GameDirector.I?.Engage(); }
            else hum.Stop();
            if (powerLamp) powerLamp.material.SetColor("_EmissionColor", on ? new Color(0.3f, 2.5f, 0.6f) : new Color(0.5f, 0.05f, 0.05f));
            if (!on && impact) impact.Stop();
        }

        void Update()
        {
            if (!Powered) return;
            Trace(out bool sensorHit);
            if (sensorHit && sensor) sensor.Hit();
        }

        // follows reflections, returns the polyline
        public List<Vector3> Trace(out bool sensorHit)
        {
            sensorHit = false;
            pts.Clear();
            Vector3 o = muzzle.position, d = muzzle.forward;
            pts.Add(o);
            int bounces = 0;
            Vector3 end = o + d * maxDistance;
            for (int i = 0; i <= maxBounces; i++)
            {
                if (!Physics.Raycast(o, d, out var hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
                {
                    end = o + d * maxDistance;
                    pts.Add(end);
                    break;
                }
                pts.Add(hit.point);
                end = hit.point;
                var mirror = hit.collider.GetComponentInParent<LaserMirror>();
                if (mirror && Vector3.Dot(d, mirror.Normal) < -0.05f && hit.collider.transform == mirror.face)
                {
                    d = Vector3.Reflect(d, mirror.Normal);
                    d.y = 0f;
                    d.Normalize();
                    o = hit.point + d * 0.01f;
                    bounces++;
                    continue;
                }
                var s = hit.collider.GetComponentInParent<LaserSensor>();
                if (s) sensorHit = true;
                break;
            }
            LastBounceCount = bounces;
            if (beam)
            {
                beam.positionCount = pts.Count;
                for (int i = 0; i < pts.Count; i++) beam.SetPosition(i, pts[i]);
            }
            if (impact)
            {
                impact.transform.position = end;
                if (!impact.isPlaying) impact.Play();
            }
            sparkTimer -= Time.deltaTime;
            if (sparkTimer <= 0f && !sensorHit) { sparkTimer = 1.2f; Sfx.Play("laser_hit", end, 0.25f); }
            return pts;
        }

        void OnUnlocked()
        {
            if (shutter) shutter.Dissolve();
            GameDirector.I?.Solve(PuzzleId.Laser);
        }
    }
}
