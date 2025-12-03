using System;
using System.Collections.Generic;
using ClientContent;

namespace ServerGame.Systems
{

    public class AbilitySystem : ISystem
    {

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


        }

        public bool TryCast(ServerWorld world, int playerId, string key, float targetX, float targetY)
        {
            var caster = world.GetHeroEntity(playerId) ?? world.EnsurePlayer(playerId);

            if (!world.AbilityBooks.TryGetValue(playerId, out var book) || book == null || !book.TryGetValue(key, out var ability))
                return false;


            if (!ClientContent.ContentAssetRegistry.Abilities.ContainsKey(ability.id))
                ClientContent.ContentAssetRegistry.Abilities[ability.id] = ability;

            if (!cooldowns.TryGetValue(playerId, out var cdByKey))
            {
                cdByKey = new Dictionary<string, float>();
                cooldowns[playerId] = cdByKey;
            }

            if (cdByKey.TryGetValue(key, out float cd) && cd > 0f)
                return false;


            if (!ability.ServerTryCast(world, playerId, targetX, targetY))
                return false;
            

            cdByKey[key] = ability.cooldown;
            return true;
        }
    }
}
