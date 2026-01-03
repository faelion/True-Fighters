using System;
using UnityEngine;
using ServerGame;
using ServerGame.Entities;

namespace Shared.Effects
{
    [CreateAssetMenu(menuName = "Content/Effects/Dash Effect", fileName = "DashEffect")]
    public class DashEffect : Effect
    {
        public float Speed = 15f;
        public bool disableCombat = true;

        public override void OnStart(ServerWorld world, ActiveEffect runtime, GameEntity target)
        {
            // Disable movement & combat
            if (target.TryGetComponent(out MovementComponent move))
            {
                move.DisabledCount++;
                // Stop existing pathfinding to prevent snap-back
                move.pathCorners = null;
                move.hasDestination = false;
                move.velX = 0; 
                move.velY = 0;
            }
            if (disableCombat && target.TryGetComponent(out CombatComponent combat))
            {
                combat.DisabledCount++;
            }
            
            Debug.Log($"[DashEffect] Started Dash on {target.Id}. Speed: {Speed}");
        }

        public override void OnTick(ServerWorld world, ActiveEffect runtime, GameEntity target, float dt)
        {
            // Move entity forward based on its current rotation
            if (target.TryGetComponent(out TransformComponent t))
            {
                float angleRad = t.rotZ * Mathf.Deg2Rad;

                float dx = Mathf.Sin(angleRad) * Speed * dt;
                float dy = Mathf.Cos(angleRad) * Speed * dt;
                
                Vector3 currentPos = new Vector3(t.posX, 0, t.posY);
                Vector3 nextPos = new Vector3(t.posX + dx, 0, t.posY + dy);

                // Raycast against NavMesh to prevent going through walls
                UnityEngine.AI.NavMeshHit hit;
                if (!UnityEngine.AI.NavMesh.Raycast(currentPos, nextPos, out hit, UnityEngine.AI.NavMesh.AllAreas))
                {
                    // No hit = clear path
                    t.posX = nextPos.x;
                    t.posY = nextPos.z;
                }
                else
                {
                    // Hit wall = stop at wall
                    t.posX = hit.position.x;
                    t.posY = hit.position.z;
                    
                    // Optional: Cancel effect? Or just slide? For now, stop.
                }
            }
        }

        public override void OnRemove(ServerWorld world, ActiveEffect runtime, GameEntity target)
        {
            // Re-enable movement & combat
            if (target.TryGetComponent(out MovementComponent move))
            {
                move.DisabledCount--;
                if (move.DisabledCount < 0) move.DisabledCount = 0;
            }
            if (target.TryGetComponent(out CombatComponent combat))
            {
                combat.DisabledCount--;
                if (combat.DisabledCount < 0) combat.DisabledCount = 0;
            }
            Debug.Log($"[DashEffect] Removed Dash from {target.Id}");
        }
    }
}
