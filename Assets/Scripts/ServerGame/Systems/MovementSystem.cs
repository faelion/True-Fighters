using System;
using UnityEngine;

namespace ServerGame.Systems
{
    public class MovementSystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            foreach (var entity in world.EntityRepo.AllEntities)
            {
                if (!entity.Health.IsAlive) continue;
                var move = entity.Movement;
                if (!move.hasDestination) continue;
                float dx = move.destX - entity.Transform.posX;
                float dy = move.destY - entity.Transform.posY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist < 0.05f)
                {
                    entity.Transform.posX = move.destX; entity.Transform.posY = move.destY; move.hasDestination = false;
                }
                else
                {
                    float step = move.moveSpeed * dt;
                    if (step > dist) step = dist;
                    entity.Transform.posX += dx / dist * step;
                    entity.Transform.posY += dy / dist * step;
                    entity.Transform.rotZ = (float)(Math.Atan2(dy, dx) * (180.0 / Math.PI));
                }
            }
        }
    }
}
