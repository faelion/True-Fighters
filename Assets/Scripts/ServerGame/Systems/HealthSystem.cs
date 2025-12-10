using ServerGame.Entities;

namespace ServerGame.Systems
{
    public class HealthSystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            var toDespawn = new System.Collections.Generic.List<int>();

            foreach (var entity in world.EntityRepo.AllEntities)
            {
                if (!entity.TryGetComponent(out HealthComponent health)) continue;
                
                if (entity.Type == EntityType.Hero)
                {
                    // Soft Death Logic for Heroes
                    if (health.IsDead)
                    {
                        health.RespawnTimer -= dt;
                        if (health.RespawnTimer <= 0f)
                        {
                            UnityEngine.Debug.Log($"[HealthSystem] Respawning Player {entity.Id}...");
                            health.IsDead = false;
                            health.Reset(health.maxHp);
                            world.RespawnPlayer(entity);
                        }
                    }
                    else if (!health.IsAlive)
                    {
                        UnityEngine.Debug.Log($"[HealthSystem] Player {entity.Id} died. Starting Soft Death.");
                        health.IsDead = true;
                        
                        float time = 5f;
                        if (world.GameMode != null) time = world.GameMode.playerRespawnTime;
                        health.RespawnTimer = time;
                    }
                }
                else
                {
                    // standard NPC Hard Death
                    if (!health.IsAlive)
                    {
                        toDespawn.Add(entity.Id);
                    }
                }
            }

            foreach (var id in toDespawn)
            {
                world.DespawnEntity(id);
            }
        }
    }
}
