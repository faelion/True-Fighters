using System;
using System.Collections.Generic;
using ClientContent;

namespace ServerGame.Systems
{
    // Minimal server-side ability processor: checks cooldown/range and triggers projectile spawns
    public class AbilitySystem : ISystem
    {
        // cooldowns[playerId][key] = seconds remaining
        private readonly Dictionary<int, Dictionary<string, float>> cooldowns = new Dictionary<int, Dictionary<string, float>>();

        public static string KeyFromInputKind(InputKind kind)
        {
            switch (kind)
            {
                case InputKind.Q: return "Q";
                case InputKind.W: return "W";
                case InputKind.E: return "E";
                case InputKind.R: return "R";
                default: return null;
            }
        }

        public void Tick(ServerWorld world, float dt)
        {
            // 1) Cooldowns
            foreach (var kv in cooldowns)
            {
                var byKey = kv.Value;
                if (byKey == null) continue;
                var keys = new List<string>(byKey.Keys);
                foreach (var key in keys)
                {
                    float v = byKey[key];
                    if (v <= 0f) continue;
                    v -= dt;
                    byKey[key] = v < 0f ? 0f : v;
                }
            }

            // Effect simulation moved to AbilityEffectSystem
        }

        public bool TryCast(ServerWorld world, int playerId, string key, float targetX, float targetY)
        {
            if (!world.Players.TryGetValue(playerId, out var caster))
                caster = world.EnsurePlayer(playerId);

            if (!world.AbilityBooks.TryGetValue(playerId, out var book) || book == null || !book.TryGetValue(key, out var ability))
                return false; // no such ability

            // Ensure registry knows about this ability id (defensive for missing content load)
            if (!ClientContent.AbilityAssetRegistry.Abilities.ContainsKey(ability.id))
                ClientContent.AbilityAssetRegistry.Abilities[ability.id] = ability;

            if (!cooldowns.TryGetValue(playerId, out var cdByKey))
            {
                cdByKey = new Dictionary<string, float>();
                cooldowns[playerId] = cdByKey;
            }

            if (cdByKey.TryGetValue(key, out float cd) && cd > 0f)
                return false; // still on cooldown

            // Delegate behavior to ability asset
            if (!ability.ServerTryCast(world, playerId, targetX, targetY))
                return false;
            
            // Set cooldown
            cdByKey[key] = ability.cooldown;
            return true;
        }
    }
}
