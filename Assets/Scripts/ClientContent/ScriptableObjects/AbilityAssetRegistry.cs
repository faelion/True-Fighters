using System.Collections.Generic;
using UnityEngine;

namespace ClientContent
{
    public static class AbilityAssetRegistry
    {
        private static bool loaded = false;
        public static readonly Dictionary<string, AbilityAsset> Abilities = new Dictionary<string, AbilityAsset>();
        public static readonly Dictionary<string, HeroSO> Heroes = new Dictionary<string, HeroSO>();
        public static string DefaultHeroId = "default";

        public static void EnsureLoaded(string databaseResourcePath = "ContentDatabase")
        {
            if (loaded) return;
            var db = Resources.Load<ContentDatabaseSO>(databaseResourcePath);
            if (db == null)
            {
                Debug.LogWarning("[AbilityAssetRegistry] No ContentDatabaseSO found in Resources.");
                loaded = true; return;
            }
            DefaultHeroId = string.IsNullOrEmpty(db.defaultHeroId) ? DefaultHeroId : db.defaultHeroId;
            Abilities.Clear();
            if (db.abilities != null)
                foreach (var a in db.abilities)
                    if (a != null && !string.IsNullOrEmpty(a.id)) Abilities[a.id] = a;
            Heroes.Clear();
            if (db.heroes != null)
                foreach (var h in db.heroes)
                    if (h != null && !string.IsNullOrEmpty(h.id)) Heroes[h.id] = h;
            Debug.Log($"[AbilityAssetRegistry] Loaded {Abilities.Count} abilities and {Heroes.Count} heroes. DefaultHeroId='{DefaultHeroId}'");
            loaded = true;
        }

        public static Dictionary<string, AbilityAsset> GetDefaultBindings()
        {
            EnsureLoaded();
            HeroSO hero = null;
            if (!string.IsNullOrEmpty(DefaultHeroId))
                Heroes.TryGetValue(DefaultHeroId, out hero);

            // If default hero not found, pick the first available hero as fallback
            if (hero == null && Heroes.Count > 0)
            {
                foreach (var h in Heroes.Values) { hero = h; break; }
                Debug.LogWarning($"[AbilityAssetRegistry] DefaultHeroId '{DefaultHeroId}' not found. Using hero '{hero.id}' as fallback.");
            }

            if (hero != null)
            {
                var map = new Dictionary<string, AbilityAsset>();
                if (hero.bindings != null)
                {
                    foreach (var b in hero.bindings)
                        if (b.ability != null && !string.IsNullOrEmpty(b.ability.id))
                            map[b.key ?? "Q"] = b.ability;
                        else
                            Debug.LogWarning($"[AbilityAssetRegistry] Binding on hero '{hero.id}' has missing ability or id (key '{b.key}').");
                }
                if (map.Count > 0)
                    return map;
            }
            // Fallback minimal binding
            var fallback = ScriptableObject.CreateInstance<ProjectileAbilityAsset>();
            fallback.id = "fallback_proj_q"; fallback.defaultKey = "Q"; fallback.range = 20f; fallback.cooldown = 1.0f; fallback.projectileSpeed = 9f; fallback.projectileLifeMs = 1400;
            if (!Abilities.ContainsKey(fallback.id)) Abilities[fallback.id] = fallback;
            Debug.LogWarning("[AbilityAssetRegistry] Using fallback projectile ability on key Q (no valid hero bindings found)." );
            return new Dictionary<string, AbilityAsset> { ["Q"] = fallback };
        }
    }
}
