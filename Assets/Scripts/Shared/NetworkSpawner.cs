using UnityEngine;
using ServerGame.Entities;

namespace Shared
{
    public enum SpawnerType
    {
        Player,
        Npc
    }

    public class NetworkSpawner : MonoBehaviour
    {
        public SpawnerType spawnerType = SpawnerType.Npc;
        [Tooltip("For Players: Team ID (0, 1, 2...)\nFor NPCs: Team ID (-1 = Neutral, or specific team)")]
        public int teamId = -1; 
        
        [Tooltip("Required for NPCs. Ignored for Players.")]
        public string archetypeId; 
        
        [Tooltip("Respawn time in seconds. 0 = No Respawn.")]
        public float respawnTime = 0f; 
        
        void OnDrawGizmos()
        {
            if (spawnerType == SpawnerType.Player)
            {
                Gizmos.color = GetTeamColor(teamId);
                Gizmos.DrawWireSphere(transform.position, 0.5f);
                Gizmos.DrawIcon(transform.position, "hero_icon", true);
            }
            else
            {
                Gizmos.color = teamId == -1 ? Color.yellow : GetTeamColor(teamId);
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            }
        }

        private Color GetTeamColor(int id)
        {
            switch(id)
            {
                case 0: return Color.red;
                case 1: return Color.blue;
                case 2: return Color.green;
                case 3: return Color.magenta;
                default: return Color.white;
            }
        }
    }
}
