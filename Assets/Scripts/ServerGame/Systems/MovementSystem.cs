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
                if (!entity.TryGetComponent(out MovementComponent movement)) continue;
                if (!entity.TryGetComponent(out TransformComponent transform)) continue;

                // Delegate to strategy
                if (movement.strategy != null)
                {
                    movement.strategy.UpdateMovement(world, entity, dt);
                }
            }
        }
    }
}
