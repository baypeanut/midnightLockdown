using TMPro;
using UnityEngine;

namespace Lockdown
{
    // puzzle 5: card in the reader, then a long two second plus hold on the reboot button
    public class RebootConsole : MonoBehaviour
    {
        public ItemSocket cardReader;
        public Pressable rebootButton;
        public Transform progressFill;
        public TextMeshPro screen;
        public Renderer hologram;
        public SlidingDoor exitDoor;
        AudioSource charge;
        bool done;

        void Start()
        {
            rebootButton.Interactable = false;
            rebootButton.holdSeconds = 3f;
            rebootButton.onHoldProgress.AddListener(OnProgress);
            rebootButton.onHoldCancel.AddListener(OnCancel);
            rebootButton.onPress.AddListener(Reboot);
            cardReader.onInserted.AddListener(_ =>
            {
                rebootButton.Interactable = true;
                SetScreen("CARD ACCEPTED\n<size=70%>hold REBOOT</size>", UiKit.Accent);
                GameDirector.I?.Engage();
            });
            SetScreen("WARDEN v4.2\n<size=70%>insert override card</size>", UiKit.Warn);
            OnProgress(0f);
        }

        void OnProgress(float p)
        {
            if (progressFill) progressFill.localScale = new Vector3(Mathf.Max(0.001f, p), 1f, 1f);
            if (p > 0f && !done)
            {
                if (!charge || !charge.isPlaying) charge = Sfx.Play("reboot_charge", rebootButton.transform.position, 0.7f);
            }
        }

        void OnCancel()
        {
            if (charge) charge.Stop();
            if (!done) SetScreen("REBOOT ABORTED\n<size=70%>hold until complete</size>", UiKit.Warn);
            Narrator.I?.Say("warden_denied", false);
        }

        void Reboot()
        {
            if (done) return;
            done = true;
            rebootButton.Interactable = false;
            SetScreen("REBOOTING...\n<size=70%>lockdown lifted</size>", UiKit.Good);
            if (hologram) hologram.material.SetColor("_Color", new Color(0.3f, 1f, 0.5f, 1f));
            if (exitDoor) exitDoor.Open();
            GameDirector.I?.Solve(PuzzleId.Reboot);
        }

        void SetScreen(string s, Color c)
        {
            if (!screen) return;
            screen.text = s;
            screen.color = c;
        }
    }
}
