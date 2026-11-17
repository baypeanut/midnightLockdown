using System.Collections;
using TMPro;
using UnityEngine;

namespace Lockdown
{
    // puzzle 2: set the stopped wall clock to the last good backup time
    public class ClockPuzzle : MonoBehaviour
    {
        public RotaryControl hourHand;
        public RotaryControl minuteHand;
        public Transform face;
        public Vector3 faceOpenEuler = new Vector3(0f, -100f, 0f);
        public Grabbable reward;
        public TextMeshPro clueNote;
        public bool Solved { get; private set; }

        int targetHour, targetMinute;

        void Start()
        {
            var k = GameDirector.I ? GameDirector.I.Secrets : new RunSecrets(2);
            targetHour = k.clockHour % 12;
            targetMinute = k.clockMinute;
            if (clueNote) clueNote.text = BuildLog(k);
            if (reward) { reward.Locked = true; reward.gameObject.SetActive(false); }
            hourHand.onReleased.AddListener(Check);
            minuteHand.onReleased.AddListener(Check);
            hourHand.onStep.AddListener(_ => Tick());
            minuteHand.onStep.AddListener(_ => Tick());
        }

        static string BuildLog(RunSecrets k)
        {
            int badH = (k.clockHour % 12) + 1;
            return "<b>NIGHTLY BACKUP LOG</b>\n" +
                   "23:00  started\n" +
                   "00:" + (k.clockMinute / 2).ToString("00") + "  partial\n" +
                   "<b>" + k.clockHour.ToString("00") + ":" + k.clockMinute.ToString("00") + "  VERIFIED GOOD</b>\n" +
                   badH.ToString("00") + ":15  failed\n\n" +
                   "<i>K: keep my clock on the last good backup. it is the only thing i trust.</i>";
        }

        void Tick()
        {
            GameDirector.I?.Engage();
        }

        // hand at value v points at index round(-v / 30) mod 12
        public static int Index(float degrees, float step)
        {
            int n = Mathf.RoundToInt(-degrees / step);
            int m = Mathf.RoundToInt(360f / step);
            return ((n % m) + m) % m;
        }

        public int HourShown => Index(hourHand.Value, 30f);
        public int MinuteShown => Index(minuteHand.Value, 30f) * 5;

        void Check()
        {
            if (Solved) return;
            if (HourShown == targetHour && MinuteShown == targetMinute) StartCoroutine(Open());
        }

        IEnumerator Open()
        {
            Solved = true;
            hourHand.Locked = true;
            minuteHand.Locked = true;
            foreach (var h in HandController.All) if (h.Held == (IGrabHandle)hourHand || h.Held == (IGrabHandle)minuteHand) h.Release();
            Sfx.Play("clock_open", transform.position, 1f);
            if (reward) reward.gameObject.SetActive(true);
            if (face)
            {
                Quaternion from = face.localRotation, to = from * Quaternion.Euler(faceOpenEuler);
                for (float t = 0; t < 1f; t += Time.deltaTime / 0.7f)
                {
                    face.localRotation = Quaternion.Slerp(from, to, 1f - Mathf.Pow(1f - t, 3f));
                    yield return null;
                }
                face.localRotation = to;
            }
            if (reward) reward.Locked = false;
            GameDirector.I?.Solve(PuzzleId.Clock);
        }

        public void DebugSet(int hour, int minute)
        {
            hourHand.SetValue(-(hour % 12) * 30f, false);
            minuteHand.SetValue(-(minute / 5) * 30f, false);
            Check();
        }
    }
}
