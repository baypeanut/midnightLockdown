using UnityEngine;

namespace Lockdown
{
    // in game menu opened with y or b
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu I { get; private set; }
        public bool IsOpen { get; private set; }
        Transform root;

        void Awake() => I = this;
        void OnDestroy() { if (I == this) I = null; }

        void Update()
        {
            var gd = GameDirector.I;
            if (gd && (gd.State == GameState.Won || gd.State == GameState.Lost)) { if (IsOpen) Close(); return; }
            if (XrInput.Get(Hand.Left).SecondaryDown || XrInput.Get(Hand.Right).SecondaryDown) Toggle();
        }

        public void Toggle()
        {
            if (IsOpen) Close(); else Open();
        }

        void Open()
        {
            IsOpen = true;
            UiFocus.Active = true;
            GameDirector.I?.Pause(true);
            if (PlayerRig.I) PlayerRig.I.Frozen = true;
            root = new GameObject("PauseMenu").transform;
            UiKit.PlaceInFront(root, 0.9f, 0.1f);
            UiKit.Panel(root, new Vector2(0.5f, 0.78f), Vector3.zero);
            UiKit.Text(root, "PAUSED", new Vector3(0f, 0.34f, -0.002f), new Vector2(0.45f, 0.05f), 0.035f, TMPro.TextAlignmentOptions.Center, UiKit.Accent);
            UiKit.Button(root, "Resume", new Vector3(0f, 0.27f, -0.006f), new Vector2(0.42f, 0.055f), Close);
            UiKit.Button(root, "Restart run", new Vector3(0f, 0.205f, -0.006f), new Vector2(0.42f, 0.055f), () => { Close(); GameDirector.I?.Restart(); });
            UiKit.Button(root, "Main menu", new Vector3(0f, 0.14f, -0.006f), new Vector2(0.42f, 0.055f), () => { Close(); GameDirector.I?.ToMenu(); });
            SettingsBlock.Build(root, new Vector3(0f, 0.06f, 0f), false);
            Sfx.Play2D("ui_click", 0.5f);
        }

        void Close()
        {
            IsOpen = false;
            UiFocus.Active = false;
            GameDirector.I?.Pause(false);
            if (PlayerRig.I) PlayerRig.I.Frozen = false;
            if (root) Destroy(root.gameObject);
        }
    }
}
