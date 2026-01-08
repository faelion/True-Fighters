using UnityEngine;
using System.IO;
using ServerGame.Entities;

namespace Client.Replicator
{
    public class NetworkHealthVisual : MonoBehaviour, INetworkComponentVisual
    {
        public int TargetComponentType => (int)ComponentType.Health;

        [SerializeField] private UnityEngine.UI.Slider healthSlider;

        private readonly HealthComponent healthComp = new HealthComponent();
        private bool isDead = false;
        private Camera mainCam;

        private void Start()
        {
            mainCam = Camera.main;
        }

        private void LateUpdate()
        {
            // Billboard Effect: Make slider face the camera
            if (healthSlider != null && healthSlider.gameObject.activeInHierarchy)
            {
                if (mainCam == null) mainCam = Camera.main;
                if (mainCam != null)
                {
                    healthSlider.transform.rotation = mainCam.transform.rotation;
                }
            }
        }

        public void OnNetworkUpdate(BinaryReader reader)
        {
            healthComp.Deserialize(reader);

            // Update Slider if assigned
            if (healthSlider != null)
            {
                healthSlider.maxValue = healthComp.maxHp;
                healthSlider.value = healthComp.currentHp;
            }

            if (isDead != healthComp.IsDead)
            {
                isDead = healthComp.IsDead;
                ToggleVisuals(!isDead);
            }
        }

        private void ToggleVisuals(bool active)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = active;
            }
            
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                c.enabled = active;
            }

            // Also hide/show the canvas/slider if present
            if (healthSlider != null)
            {
                healthSlider.gameObject.SetActive(active);
            }
        }
    }
}
