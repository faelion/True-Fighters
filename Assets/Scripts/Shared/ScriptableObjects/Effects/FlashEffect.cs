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
        public bool disableCombat = true;

        public override void OnStart(ServerWorld world, ActiveEffect runtime, GameEntity target)
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
