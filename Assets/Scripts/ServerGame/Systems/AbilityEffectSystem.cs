using System.Collections.Generic;

namespace ServerGame.Systems
{
    public class AbilityEffectSystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            List<int> toRemove = null;
            foreach (var kv in world.AbilityEffects)
            {
                var eff = kv.Value;
                eff.lifeMs -= (int)(dt * 1000f);
                if (eff.lifeMs <= 0)
                {
                    (toRemove ??= new List<int>()).Add(eff.id);
                    continue;
                }

                if (!ClientContent.AbilityAssetRegistry.Abilities.TryGetValue(eff.abilityId, out var asset) || asset == null)
                    continue;
                bool alive = asset.ServerUpdateEffect(world, eff, dt);
                if (!alive)
                {
                    eff.lifeMs = 0;
                    (toRemove ??= new List<int>()).Add(eff.id);
                }
            }

            if (toRemove != null)
            {
                foreach (var id in toRemove)
                {
                    if (world.AbilityEffects.TryGetValue(id, out var eff))
                    {
                        world.AbilityEffects.Remove(id);
                        world.MarkEffectDespawned(id, eff.abilityId);
                    }
                }
            }
        }
    }
}
