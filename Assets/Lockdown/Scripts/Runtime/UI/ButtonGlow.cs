using UnityEngine;

namespace Lockdown
{
    // swaps the button material while a pointer or finger is on it
    public class ButtonGlow : MonoBehaviour
    {
        public Renderer target;
        Pressable p;
        bool hot;

        void Awake() => p = GetComponent<Pressable>();

        void Update()
        {
            if (!target || !p) return;
            bool want = p.IsDown;
            if (want == hot) return;
            hot = want;
            target.sharedMaterial = hot ? UiKit.ButtonHotMaterial : UiKit.ButtonMaterial;
        }
    }
}
