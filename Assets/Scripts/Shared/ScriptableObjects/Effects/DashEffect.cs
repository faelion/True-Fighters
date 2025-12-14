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

                t.posX += dx;
                t.posY += dy;
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
