using System.Collections;
using TMPro;
using UnityEngine;

namespace Lockdown
{
    // puzzle 3: crack the dial safe by feel and sound, no visual readout of the target
    public class SafePuzzle : MonoBehaviour
    {
        public RotaryControl dial;
        public Transform door;
        public Vector3 doorOpenEuler = new Vector3(0f, 105f, 0f);
        public Renderer[] stageLamps = new Renderer[3];
        public Grabbable reward;
        public TextMeshPro note;
        public bool Opened { get; private set; }

        SafeLock lockLogic;
        int lastStep;
        MaterialPropertyBlock block;

        void Start()
        {
            block = new MaterialPropertyBlock();
            var k = GameDirector.I ? GameDirector.I.Secrets : new RunSecrets(3);
            lockLogic = new SafeLock(k.safe);
            dial.stepDegrees = 360f / RunSecrets.SafeNumbers;
            dial.stepSound = "dial_tick";
            dial.stepHaptic = 0.05f;
            dial.onStep.AddListener(OnStep);
            lastStep = dial.Step;
            if (reward) { reward.Locked = true; }
            if (note) note.text = "R, L, R.\nslow hands.\nlisten for it.\n<size=70%>K</size>";
            UpdateLamps();
        }

        public static int Number(int step)
        {
            int n = RunSecrets.SafeNumbers;
            return ((step % n) + n) % n;
        }

        public int Current => Number(dial.Step);

        void OnStep(int step)
        {
            if (Opened) return;
            GameDirector.I?.Engage();
            int direction = step > lastStep ? 1 : -1;
            lastStep = step;
            Apply(lockLogic.Step(Number(step), direction));
        }

        void Apply(SafeLock.Result r)
        {
            var hand = dial.Holder;
            switch (r)
            {
                case SafeLock.Result.Catch:
                    Sfx.Play("dial_catch", dial.transform.position, 0.9f);
                    if (hand) Haptics.Pulse(hand.hand, 0.85f, 0.07f);
                    break;
                case SafeLock.Result.Locked:
                    Sfx.Play("beep_ok", dial.transform.position, 0.5f);
                    if (hand) Haptics.Pulse(hand.hand, 0.5f, 0.15f);
                    UpdateLamps();
                    break;
                case SafeLock.Result.Reset:
                    Sfx.Play("beep_error", dial.transform.position, 0.4f);
                    if (GameDirector.I) GameDirector.I.SafeResets++;
                    UpdateLamps();
                    break;
                case SafeLock.Result.Opened:
                    UpdateLamps();
                    StartCoroutine(Open());
                    break;
            }
        }

        void UpdateLamps()
        {
            for (int i = 0; i < stageLamps.Length; i++)
            {
                if (!stageLamps[i]) continue;
                bool on = lockLogic != null && lockLogic.Stage > i;
                stageLamps[i].GetPropertyBlock(block);
                block.SetColor("_EmissionColor", on ? new Color(0.2f, 2f, 0.5f) : new Color(0.15f, 0.03f, 0.02f));
                stageLamps[i].SetPropertyBlock(block);
            }
        }

        IEnumerator Open()
        {
            Opened = true;
            dial.Locked = true;
            if (dial.Holder) dial.Holder.Release();
            Sfx.Play("safe_open", door ? door.position : transform.position, 1f);
            if (door)
            {
                Quaternion from = door.localRotation, to = from * Quaternion.Euler(doorOpenEuler);
                for (float t = 0; t < 1f; t += Time.deltaTime / 1.2f)
                {
                    door.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                    yield return null;
                }
                door.localRotation = to;
            }
            if (reward) reward.Locked = false;
            GameDirector.I?.Solve(PuzzleId.Safe);
        }

        // editor check helper: walk the dial through the real solution
        public void DebugSolve()
        {
            var k = GameDirector.I ? GameDirector.I.Secrets : new RunSecrets(3);
            int dirSign = 1;
            int step = dial.Step;
            foreach (int target in k.safe)
            {
                int guard = 0;
                while (Number(step) != target && guard++ < 200)
                {
                    step += dirSign;
                    dial.SetValue(step * dial.stepDegrees, false);
                    OnStep(step);
                }
                dirSign = -dirSign;
                step += dirSign;
                dial.SetValue(step * dial.stepDegrees, false);
                OnStep(step);
            }
        }
    }
}
