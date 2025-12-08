using UnityEngine;
using ServerGame.Entities;

namespace Shared.ScriptableObjects
{
    [CreateAssetMenu(menuName = "Content/Strategies/Destination Movement")]
    public class DestinationMovementSO : MovementStrategySO
    {
        public override void UpdateMovement(ServerGame.ServerWorld world, GameEntity entity, float dt)
        {
            if (!entity.TryGetComponent(out TransformComponent t)) return;
            if (!entity.TryGetComponent(out MovementComponent m)) return;

            if (!m.hasDestination) return;

            float dx = m.destX - t.posX;
            float dy = m.destY - t.posY;
            float distSq = dx * dx + dy * dy;
            float step = m.moveSpeed * dt;

            // If close enough, snap and stop
            if (distSq <= step * step)
            {
                t.posX = m.destX;
                t.posY = m.destY;
                m.hasDestination = false;
                m.velX = 0;
                m.velY = 0;
            }
            else
            {
                float dist = Mathf.Sqrt(distSq);
                float nx = dx / dist;
                float ny = dy / dist;

                // Update velocity for other systems to read (e.g. animation)
                m.velX = nx * m.moveSpeed;
                m.velY = ny * m.moveSpeed;

                t.posX += m.velX * dt;
                t.posY += m.velY * dt;

                float angle = Mathf.Atan2(nx, ny) * Mathf.Rad2Deg;
                t.rotZ = angle;
            }
        }
    }
}
