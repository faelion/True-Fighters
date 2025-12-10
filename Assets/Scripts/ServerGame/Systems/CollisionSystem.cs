using System;
using System.Collections.Generic;
using ServerGame.Entities;
using ClientContent;

namespace ServerGame.Systems
{
    public class CollisionSystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            var entities = world.EntityRepo.AllEntities;
            // TODO: Optimization: Replace O(N^2) check with Spatial Partitioning (Grid/Quadtree).
            // TODO: Optimization: Filter mostly static entities to reduce checks.
            
            var all = new List<GameEntity>(entities);
            int count = all.Count;
            
            for (int i = 0; i < count; i++)
            {
                var us = all[i];
                if (!us.TryGetComponent(out CollisionComponent myCol)) continue;
                if (!us.TryGetComponent(out TransformComponent myTrans)) continue;
                
                for (int j = i + 1; j < count; j++)
                {
                    var them = all[j];
                    if (!them.TryGetComponent(out CollisionComponent theirCol)) continue;
                    if (!them.TryGetComponent(out TransformComponent theirTrans)) continue;

                    float dx = myTrans.posX - theirTrans.posX;
                    float dy = myTrans.posY - theirTrans.posY;
                    float distSq = dx * dx + dy * dy;

                    float r = myCol.radius + theirCol.radius;
                    if (distSq <= r * r)
                    {
                        ResolveCollision(world, us, them);
                        ResolveCollision(world, them, us);
                    }
                }
            }
        }

        private void ResolveCollision(ServerWorld world, GameEntity me, GameEntity other)
        {
            if (!string.IsNullOrEmpty(me.ArchetypeId))
            {
                var logic = ContentAssetRegistry.GetEntityLogic(me.ArchetypeId);
                if (logic != null)
                {
                    logic.OnEntityCollision(world, me, other);
                }
            }
        }
    }
}
