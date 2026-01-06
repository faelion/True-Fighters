using ServerGame.Entities;

namespace ServerGame.Systems
{
    public class AISystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            foreach (var entity in world.EntityRepo.AllEntities)
            {
                if (!entity.TryGetComponent(out AIControllerComponent aiComp)) continue;
                if (!entity.TryGetComponent(out HealthComponent health) || !health.IsAlive) continue;

                if (aiComp.Behavior != null)
                {
                    aiComp.Behavior.Tick(world, entity, dt);
                }
            }
        }
    }
}
