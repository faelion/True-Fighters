using UnityEngine;
using ServerGame.Entities;

namespace Shared
{
    public class NetworkSpawner : MonoBehaviour
    {
        public EntityType entityType = EntityType.Neutral;
        public int teamId = -1; // -1 for neutral, 0+ for players
        public string archetypeId; // e.g. "MinionMelee", "JungleCreep"
        public float respawnTime = 0f; // 0 = no respawn
        
        // Optional: Gizmos to visualize in editor
        void OnDrawGizmos()
        {
            Gizmos.color = teamId == -1 ? Color.yellow : (teamId == 0 ? Color.blue : Color.red);
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            if (entityType == EntityType.Hero)
                Gizmos.DrawIcon(transform.position, "hero_icon", true);
        }
    }
}
