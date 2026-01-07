using System.Collections.Generic;
using ServerGame.Entities;

namespace ServerGame.Systems
{
    public class StatusEffectSystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            var entities = world.EntityRepo.AllEntities;
            foreach (var entity in entities)
            {
                if (!entity.TryGetComponent(out StatusEffectComponent statusComp)) continue;

                for (int i = statusComp.ActiveEffects.Count - 1; i >= 0; i--)
                {
                    var active = statusComp.ActiveEffects[i];
                    
                    // Handle Start
                    bool wasJustStarted = false;
                    if (active.IsNew)
                    {
                        active.IsNew = false;
                        active.SourceEffect.OnStart(world, active, entity);
                        wasJustStarted = true;
                    }

                    // Tick
                    active.RemainingTime -= dt;
                    active.SourceEffect.OnTick(world, active, entity, dt);

                    // Expiration
                    if (active.RemainingTime <= 0f && !wasJustStarted)
                    {
                        active.SourceEffect.OnRemove(world, active, entity);
                        statusComp.ActiveEffects.RemoveAt(i);
                    }
                }
            }
        }
    }
}
