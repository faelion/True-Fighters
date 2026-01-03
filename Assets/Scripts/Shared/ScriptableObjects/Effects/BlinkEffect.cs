using UnityEngine;
using UnityEngine.AI;
using ServerGame;
using ServerGame.Entities;

namespace Shared.Effects
{
    [CreateAssetMenu(menuName = "Content/Effects/Blink Effect", fileName = "BlinkEffect")]
    public class BlinkEffect : Effect
    {
        public bool clearPath = true;

        public override void Apply(ServerWorld world, GameEntity source, GameEntity target, Vector3? targetPos = null)
        {
            // Instant effect, no need to add to StatusEffectComponent if Duration is 0
            // But if we want visual persistence or delays, we might usage base.Apply.
            // For pure Blink, we execute immediately.
            
            if (!targetPos.HasValue) return;
            if (!target.TryGetComponent(out TransformComponent t)) return;

            // 1. Validate Target Logic (Sample NavMesh)
            Vector3 desiredPos = targetPos.Value;
            NavMeshHit hit;
            
            // Sample closest valid point within allowance (e.g., 5.0f or more if going over walls)
            // If the user clicks DEEP inside a wall, SamplePosition finds the closest edge.
            if (NavMesh.SamplePosition(desiredPos, out hit, 10.0f, NavMesh.AllAreas))
            {
                desiredPos = hit.position;
            }
            else
            {
                // Fallback: don't move or move to max range raycast??
                // If sample fails (void), maybe we shouldn't blink.
                return; 
            }

            // 2. Teleport
            t.posX = desiredPos.x;
            t.posY = desiredPos.z;

            // 3. Clear Path to prevent snapback
            if (clearPath && target.TryGetComponent(out MovementComponent move))
            {
                move.pathCorners = null;
                move.hasDestination = false;
                move.velX = 0;
                move.velY = 0;
                move.destX = t.posX;
                move.destY = t.posY;
            }
            
            Debug.Log($"[BlinkEffect] Teleported {target.Id} to {desiredPos}");
        }
    }
}
