using TMPro;
using UnityEngine;

namespace Lockdown
{
    // puzzle 1: three rated fuses go in matching slots, then the main lever
    public class FuseBox : MonoBehaviour
    {
        public ItemSocket[] slots = new ItemSocket[3];
        public TextMeshPro[] slotLabels = new TextMeshPro[3];
        public Renderer[] slotLamps = new Renderer[3];
        public LeverControl lever;
        public ParticleSystem sparks;
        public TextMeshPro status;
        MaterialPropertyBlock block;
        static readonly string[] AllFuses = { "fuse10", "fuse20", "fuse30" };

        void Start()
        {
            block = new MaterialPropertyBlock();
            var k = GameDirector.I ? GameDirector.I.Secrets : new RunSecrets(1);
            for (int i = 0; i < slots.Length; i++)
            {
                string want = "fuse" + k.fuseSlots[i];
                slots[i].acceptIds = new[] { want };
                slots[i].rejectIds = AllFuses;
                if (slotLabels[i]) slotLabels[i].text = k.fuseSlots[i] + "A";
                int idx = i;
                slots[i].onInserted.AddListener(_ => OnFilled(idx));
                slots[i].onRejected.AddListener(_ => OnWrong(idx));
                Lamp(i, false);
            }
            lever.Armed = false;
            lever.onPulled.AddListener(PowerUp);
            lever.onBlocked.AddListener(() =>
            {
                Sfx.Play("beep_error", lever.transform.position, 0.6f);
                Hud.I?.Toast("The lever will not move. Fuses missing.", 2.5f, UiKit.Warn);
            });
            SetStatus("NO FUSES", UiKit.Warn);
        }

        void OnFilled(int i)
        {
            Lamp(i, true);
            GameDirector.I?.Engage();
            int n = 0;
            foreach (var s in slots) if (s.Filled) n++;
            if (n == slots.Length)
            {
                lever.Armed = true;
                SetStatus("READY: PULL LEVER", UiKit.Good);
                Sfx.Play("beep_ok", transform.position, 0.6f);
            }
            else SetStatus(n + " / 3 FUSES", UiKit.Accent);
        }

        void OnWrong(int i)
        {
            if (sparks)
            {
                sparks.transform.position = slots[i].transform.position;
                sparks.Play();
            }
            Sfx.Play("spark", slots[i].transform.position, 1f);
            Haptics.Both(0.6f, 0.12f);
            SetStatus("WRONG RATING", UiKit.Warn);
        }

        void PowerUp()
        {
            SetStatus("POWER RESTORED", UiKit.Good);
            PowerGrid.I?.PowerOn();
            GameDirector.I?.Solve(PuzzleId.Power);
        }

        void Lamp(int i, bool on)
        {
            if (i >= slotLamps.Length || !slotLamps[i]) return;
            slotLamps[i].GetPropertyBlock(block);
            block.SetColor("_EmissionColor", on ? new Color(0.2f, 2.2f, 0.5f) : new Color(1.2f, 0.12f, 0.08f));
            slotLamps[i].SetPropertyBlock(block);
        }

        void SetStatus(string s, Color c)
        {
            if (!status) return;
            status.text = s;
            status.color = c;
        }
    }
}
