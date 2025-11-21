namespace ServerGame.Systems
{
    public class HealthSystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            foreach (var entity in world.EntityRepo.AllEntities)
            {
                var health = entity.Health;
                if (!health.recentlyHit) continue;
                health.hitTimer += dt;
                if (health.hitTimer >= ServerWorld.HitFlashDuration)
                {
                    health.hitTimer = 0f;
                    health.recentlyHit = false;
                }
            }
        }
    }
}
