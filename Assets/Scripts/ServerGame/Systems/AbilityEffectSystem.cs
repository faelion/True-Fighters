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
                bool alive = asset.OnEffectTick(world, eff, dt);
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
                        if (ClientContent.AbilityAssetRegistry.Abilities.TryGetValue(eff.abilityId, out var asset) && asset != null)
                        {
                            if (asset.EmitDespawnEvent(world, eff, out var despawnEvt) && despawnEvt != null)
                                world.EnqueueEvent(despawnEvt);
                            asset.OnEffectExpired(world, eff);
                        }
                        world.AbilityEffects.Remove(id);
                    }
                }
            }
        }
    }
}
