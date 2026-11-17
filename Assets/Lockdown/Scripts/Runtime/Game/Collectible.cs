using UnityEngine;

namespace Lockdown
{
    // hidden override chip, collected by touching it with a hand
    public class Collectible : MonoBehaviour
    {
        public float touchRadius = 0.1f;
        public ParticleSystem burst;
        public Transform spinner;
        bool taken;

        void Update()
        {
            if (taken) return;
            if (spinner) spinner.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            foreach (var h in HandController.All)
            {
                if (Vector3.Distance(h.GripPoint, transform.position) < touchRadius || Vector3.Distance(h.Fingertip, transform.position) < touchRadius)
                {
                    Take(h);
                    return;
                }
            }
        }

        void Take(HandController h)
        {
            taken = true;
            Sfx.Play("chip_pickup", transform.position, 0.9f);
            Haptics.Pulse(h.hand, 0.5f, 0.1f);
            if (burst)
            {
                burst.transform.SetParent(null, true);
                burst.Play();
                Destroy(burst.gameObject, 3f);
            }
            GameDirector.I?.CollectChip();
            gameObject.SetActive(false);
        }
    }
}
