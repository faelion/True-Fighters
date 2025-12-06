using UnityEngine;
using ServerGame.Entities;

namespace Shared.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Content/Strategies/Linear Movement")]
    public class LinearMovementSO : MovementStrategySO
    {
        public override void UpdateMovement(ServerGame.ServerWorld world, GameEntity entity, float dt)
        {
            if (!entity.TryGetComponent(out TransformComponent t)) return;
            if (!entity.TryGetComponent(out MovementComponent m)) return;

            // Apply velocity directly
            t.posX += m.velX * dt;
            t.posY += m.velY * dt;
        }
    }
}
