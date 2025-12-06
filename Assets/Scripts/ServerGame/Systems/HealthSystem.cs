using ServerGame.Entities;

namespace ServerGame.Systems
{
    public class HealthSystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            // Use a list to avoid modification during iteration if we remove entities
            var toDespawn = new System.Collections.Generic.List<int>();

            foreach (var entity in world.EntityRepo.AllEntities)
            {
                if (!entity.TryGetComponent(out HealthComponent health)) continue;
                
                if (!health.IsAlive)
                {
                    toDespawn.Add(entity.Id);
                    continue;
                }

                if (!health.recentlyHit) continue;
                health.hitTimer += dt;
                if (health.hitTimer >= ServerWorld.HitFlashDuration)
                {
                    health.hitTimer = 0f;
                    health.recentlyHit = false;
                }
            }

            foreach (var id in toDespawn)
            {
                world.DespawnEntity(id);
            }
        }
    }
}
