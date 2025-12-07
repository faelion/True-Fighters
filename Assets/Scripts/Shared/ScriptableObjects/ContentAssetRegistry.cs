using System.Collections.Generic;
using UnityEngine;

namespace ClientContent
{
    public static class ContentAssetRegistry
    {
        private static bool loaded = false;
        public static readonly Dictionary<string, AbilityAsset> Abilities = new Dictionary<string, AbilityAsset>();
        public static readonly Dictionary<string, HeroSO> Heroes = new Dictionary<string, HeroSO>();
        public static readonly Dictionary<string, NeutralEntitySO> Neutrals = new Dictionary<string, NeutralEntitySO>();
        public static string DefaultHeroId = "default";
        public static string DefaultNeutralId = "neutral_default";
        
        public static Shared.ScriptableObjects.MovementStrategySO DefaultMovementStrategy
        {
            get
            {
                if (_defaultMovementStrategy == null)
                {
                    _defaultMovementStrategy = ScriptableObject.CreateInstance<Shared.ScriptableObjects.DestinationMovementSO>();
                }
                return _defaultMovementStrategy;
            }
        }
        private static Shared.ScriptableObjects.MovementStrategySO _defaultMovementStrategy;

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
            DefaultNeutralId = string.IsNullOrEmpty(db.defaultNeutralId) ? DefaultNeutralId : db.defaultNeutralId;
            Abilities.Clear();
            if (db.abilities != null)
                foreach (var a in db.abilities)
                    if (a != null && !string.IsNullOrEmpty(a.id)) Abilities[a.id] = a;
            Heroes.Clear();
            if (db.heroes != null)
                foreach (var h in db.heroes)
                    if (h != null && !string.IsNullOrEmpty(h.id)) Heroes[h.id] = h;
            Neutrals.Clear();
            if (db.neutrals != null)
                foreach (var n in db.neutrals)
                    if (n != null && !string.IsNullOrEmpty(n.id)) Neutrals[n.id] = n;
            Debug.Log($"[AbilityAssetRegistry] Loaded {Abilities.Count} abilities, {Heroes.Count} heroes, {Neutrals.Count} neutrals. DefaultHeroId='{DefaultHeroId}' DefaultNeutralId='{DefaultNeutralId}'");
            loaded = true;
        }

        public static Dictionary<string, AbilityAsset> GetDefaultBindings()
        {
            return GetBindingsForHero(DefaultHeroId);
        }

        public static Dictionary<string, AbilityAsset> GetBindingsForHero(string heroId)
        {
            EnsureLoaded();
            HeroSO hero = null;
            if (!string.IsNullOrEmpty(heroId))
                Heroes.TryGetValue(heroId, out hero);

            // If default hero not found, pick the first available hero as fallback
            if (hero == null && Heroes.Count > 0)
            {
                foreach (var h in Heroes.Values) { hero = h; break; }
                Debug.LogWarning($"[AbilityAssetRegistry] HeroId '{heroId}' not found. Using hero '{hero.id}' as fallback.");
            }

            if (hero != null)
            {
                var map = new Dictionary<string, AbilityAsset>();
                if (hero.bindings != null)
                {
                    foreach (var b in hero.bindings)
                    {
                        if (b.ability != null && !string.IsNullOrEmpty(b.ability.id))
                            map[b.key ?? "Q"] = b.ability;
                        else
                            Debug.LogWarning($"[AbilityAssetRegistry] Binding on hero '{hero.id}' has missing ability or id (key '{b.key}').");
                    }
                }
                if (map.Count > 0)
                    return map;
            }
            // Fallback minimal binding
            var fallback = ScriptableObject.CreateInstance<ProjectileAbilityAsset>();
            fallback.id = "fallback_proj_q"; fallback.defaultKey = "Q"; fallback.range = 20f; fallback.cooldown = 1.0f; fallback.projectileSpeed = 9f; fallback.projectileLifeMs = 1400;
            
            // Default damage
            var dmgEffect = ScriptableObject.CreateInstance<Shared.Effects.InstantDamageEffect>();
            dmgEffect.Amount = 10f;
            fallback.onHitEffects = new System.Collections.Generic.List<Shared.Effects.Effect> { dmgEffect };

            if (!Abilities.ContainsKey(fallback.id)) Abilities[fallback.id] = fallback;
            Debug.LogWarning("[AbilityAssetRegistry] Using fallback projectile ability on key Q (no valid hero bindings found)." );
            return new Dictionary<string, AbilityAsset> { ["Q"] = fallback };
        }

        public static NeutralEntitySO GetNeutral(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id)) id = DefaultNeutralId;
            if (Neutrals.TryGetValue(id, out var n) && n != null) return n;
            if (Neutrals.TryGetValue(DefaultNeutralId, out var def)) return def;
            return null;
        }
        public static Shared.ScriptableObjects.IEntityLogic GetEntityLogic(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id)) return null;

            if (Abilities.TryGetValue(id, out var ability)) return ability;
            if (Heroes.TryGetValue(id, out var hero)) return hero;
            if (Neutrals.TryGetValue(id, out var neutral)) return neutral;
            
            return null;
        }
    }
}
