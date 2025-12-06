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
            // Naive O(N^2) collision for now. Can optimize with spatial partition later.
            // We iterate all entities. If they have CollisionComponent + Transform, we check against others.
            
            // Note: We need a safe way to iterate as we might despawn things.
            // Use snapshot of IDs?
            // Actually, we can collect events and process them, or just carefully iterate.
            // A simple way is to convert to list.
            var all = new List<GameEntity>(entities);
            int count = all.Count;
            
            for (int i = 0; i < count; i++)
            {
                var us = all[i];
                if (!us.TryGetComponent(out HealthComponent myHealth) || !myHealth.IsAlive) continue;
                if (!us.TryGetComponent(out CollisionComponent myCol)) continue;
                if (!us.TryGetComponent(out TransformComponent myTrans)) continue;
                
                // Only "Active" colliders (like projectiles) usually initiate collision checks?
                // Or do we check everything vs everything? 
                // The user says "when 2 things collide... logic in specific SO".
                // If Projectile hits Hero, we want ProjectileSO.OnEntityCollision(Hero).
                // If Hero bumps into Hero, we might want HeroSO.OnEntityCollision(Hero)?
                // Let's check everyone.

                // Optimization: Maybe only entities with "isTrigger" or similar check?
                // For now, check all.
                
                for (int j = i + 1; j < count; j++)
                {
                    var them = all[j];
                    if (!them.TryGetComponent(out HealthComponent theirHealth) || !theirHealth.IsAlive) continue;
                    if (!them.TryGetComponent(out CollisionComponent theirCol)) continue;
                    if (!them.TryGetComponent(out TransformComponent theirTrans)) continue;

                    float dx = myTrans.posX - theirTrans.posX;
                    float dy = myTrans.posY - theirTrans.posY;
                    float distSq = dx * dx + dy * dy;

                    float r = myCol.radius + theirCol.radius;
                    if (distSq <= r * r)
                    {
                        // Collision!
                        ResolveCollision(world, us, them);
                        ResolveCollision(world, them, us);
                    }
                }
            }
        }

        private void ResolveCollision(ServerWorld world, GameEntity me, GameEntity other)
        {
            // Find logic for 'me'
            if (!string.IsNullOrEmpty(me.ArchetypeId))
            {
                // Try to find Ability with this ID (for projectiles)
                // ProjectileAbilityAsset sets ArchetypeId = AbilityId.
                if (ContentAssetRegistry.Abilities.TryGetValue(me.ArchetypeId, out var ability) && ability != null)
                {
                    ability.OnEntityCollision(world, me, other);
                    return;
                }
                
                // Try to find Hero/Neutral (for units)
                // If units have collision logic (e.g. repulsor?)
                // Assuming currently only Abilities (Projectiles) have active collision logic.
                // But structure supports others.
            }
        }
    }
}
