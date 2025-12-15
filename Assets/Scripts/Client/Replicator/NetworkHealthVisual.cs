using UnityEngine;
using System.IO;
using ServerGame.Entities;

namespace Client.Replicator
{
    public class NetworkHealthVisual : MonoBehaviour, INetworkComponentVisual
    {
        public int TargetComponentType => (int)ComponentType.Health;

        private readonly HealthComponent healthComp = new HealthComponent();
        private bool isDead = false;

        public void OnNetworkUpdate(BinaryReader reader)
        {
            healthComp.Deserialize(reader);

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
        }
    }
}
