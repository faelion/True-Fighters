using ServerGame.Entities;

namespace ServerGame.Systems
{
    public class LifetimeSystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            var toDespawn = new System.Collections.Generic.List<int>();

            foreach (var entity in world.EntityRepo.AllEntities)
            {
                if (!entity.TryGetComponent(out LifetimeComponent lifetime)) continue;

                lifetime.remainingTime -= dt;
                if (lifetime.remainingTime <= 0f)
                {
                    toDespawn.Add(entity.Id);
                }
            }

            foreach (var id in toDespawn)
            {
                world.DespawnEntity(id);
            }
        }
    }
}
