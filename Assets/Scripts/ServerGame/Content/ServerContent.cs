using System.Collections.Generic;
using UnityEngine;

namespace ServerGame.Content
{
    public static class ServerContent
    {
        public static readonly Dictionary<string, AbilityDef> Abilities = new Dictionary<string, AbilityDef>();
        public static readonly Dictionary<string, ServerHeroDef> Heroes = new Dictionary<string, ServerHeroDef>();
        public static string DefaultHeroId = "default";

        public static Dictionary<string, AbilityDef> GetDefaultBindings()
        {
            // If we have a real hero, use its bindings
            if (Heroes.TryGetValue(DefaultHeroId, out var hero))
            {
                var map = new Dictionary<string, AbilityDef>();
                foreach (var kv in hero.bindings)
                {
                    if (Abilities.TryGetValue(kv.Value, out var def))
                        map[kv.Key] = def;
                }
                return map;
            }
            // Fallback minimal binding: Q projectile
            return new Dictionary<string, AbilityDef>
            {
                ["Q"] = new AbilityDef
                {
                    id = "fallback_proj_q",
                    key = "Q",
                    kind = AbilityKind.Projectile,
                    range = 20f,
                    cooldown = 1.0f,
                    projectileSpeed = 9f,
                    projectileLifeMs = 1400
                }
            };
        }
    }
}

