using System;
using UnityEngine;
using ServerGame.Entities;

namespace ServerGame.Systems
{
    public class MovementSystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            foreach (var entity in world.EntityRepo.AllEntities)
            {
                if (!entity.TryGetComponent(out HealthComponent health) || !health.IsAlive) continue;
                if (!entity.TryGetComponent(out PlayerMovementComponent move)) continue;
                if (!entity.TryGetComponent(out TransformComponent transform)) continue;

                if (!move.hasDestination) continue;
                
                float dx = move.destX - transform.posX;
                float dy = move.destY - transform.posY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist < 0.05f)
                {
                    transform.posX = move.destX; transform.posY = move.destY; move.hasDestination = false;
                }
                else
                {
                    float step = move.moveSpeed * dt;
                    if (step > dist) step = dist;
                    transform.posX += dx / dist * step;
                    transform.posY += dy / dist * step;
                    transform.rotZ = (float)(Math.Atan2(dy, dx) * (180.0 / Math.PI));
                }
            }
        }
    }
}
