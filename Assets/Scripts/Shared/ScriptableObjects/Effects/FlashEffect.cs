using System;
using UnityEngine;
using ServerGame;
using ServerGame.Entities;

namespace Shared.Effects
{
    [CreateAssetMenu(menuName = "Content/Effects/Flash Effect", fileName = "FlashEffect")]
    public class FlashEffect : Effect
    {
        public float JumpUnits = 15f;
        public float Duration = 0.0f;
        public bool disableCombat = true;

        public override void Apply(ServerWorld world, GameEntity source, GameEntity target)
        {
            if (target.TryGetComponent(out StatusEffectComponent status))
            {
                status.AddEffect(this, Duration, source);
                
                Debug.Log($"[DashEffect] Applied Flash to {target.Id}. JumpUnits: {JumpUnits}");
            }
        }

        public override void OnTick(ServerWorld world, ActiveEffect runtime, GameEntity target, float dt)
        {
            // Move entity forward based on its current rotation
            if (target.TryGetComponent(out TransformComponent t))
            {
                float angleRad = t.rotZ * Mathf.Deg2Rad;

                float dx = Mathf.Sin(angleRad) * JumpUnits;
                float dy = Mathf.Cos(angleRad) * JumpUnits;

                t.posX += dx;
                t.posY += dy;
            }
        }

        public override void OnRemove(ServerWorld world, ActiveEffect runtime, GameEntity target)
        {
            Debug.Log($"[FlashEffect] Removed Flash from {target.Id}");
        }
    }
}
