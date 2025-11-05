using System.Collections.Generic;
using UnityEngine;

// Attach this to a GameObject in the Server scene to load content from ScriptableObjects (if present)
namespace ServerGame.Content
{
    public class ServerContentLoader : MonoBehaviour
    {
        public string databaseResourcePath = "ContentDatabase"; // Resources/ContentDatabase

        void Awake()
        {
            TryLoadFromResources();
        }

        void TryLoadFromResources()
        {
            var db = Resources.Load<ClientContent.ContentDatabaseSO>(databaseResourcePath);
            if (db == null)
            {
                Debug.Log("[ServerContentLoader] No ContentDatabaseSO found in Resources. Using fallback content.");
                return;
            }

            ServerContent.DefaultHeroId = string.IsNullOrEmpty(db.defaultHeroId) ? ServerContent.DefaultHeroId : db.defaultHeroId;

            // Abilities
            ServerContent.Abilities.Clear();
            foreach (var ability in db.abilities)
            {
                if (ability == null || string.IsNullOrEmpty(ability.id)) continue;
                var def = ConvertAbility(ability);
                ServerContent.Abilities[def.id] = def;
            }

            // Heroes
            ServerContent.Heroes.Clear();
            foreach (var hero in db.heroes)
            {
                if (hero == null || string.IsNullOrEmpty(hero.id)) continue;
                var h = new ServerHeroDef { id = hero.id, displayName = hero.displayName, baseHp = hero.baseHp, baseMoveSpeed = hero.baseMoveSpeed };
                if (hero.bindings != null)
                {
                    foreach (var b in hero.bindings)
                    {
                        if (b.ability != null && !string.IsNullOrEmpty(b.ability.id))
                        {
                            h.bindings[b.key ?? "Q"] = b.ability.id;
                        }
                    }
                }
                ServerContent.Heroes[h.id] = h;
            }
            Debug.Log($"[ServerContentLoader] Loaded {ServerContent.Abilities.Count} abilities and {ServerContent.Heroes.Count} heroes");
        }

        AbilityDef ConvertAbility(ClientContent.BaseAbilitySO so)
        {
            var def = new AbilityDef
            {
                id = so.id,
                key = so.defaultKey,
                range = so.range,
                castTime = so.castTime,
                cooldown = so.cooldown,
                targeting = (AbilityTargeting)so.targeting
            };

            switch (so.kind)
            {
                case ClientContent.AbilityKind.Projectile:
                    def.kind = AbilityKind.Projectile;
                    var proj = so as ClientContent.ProjectileAbilitySO;
                    if (proj != null)
                    {
                        def.projectileSpeed = proj.projectileSpeed;
                        def.projectileLifeMs = proj.projectileLifeMs;
                        def.projectileDamage = proj.damage;
                    }
                    break;
                case ClientContent.AbilityKind.Area:
                    def.kind = AbilityKind.Area;
                    var area = so as ClientContent.AreaAbilitySO;
                    if (area != null)
                    {
                        def.areaRadius = area.radius;
                        def.areaLifeMs = area.lifeMs;
                    }
                    break;
                case ClientContent.AbilityKind.Dash:
                    def.kind = AbilityKind.Dash;
                    var dash = so as ClientContent.DashAbilitySO;
                    if (dash != null)
                    {
                        def.dashDistance = dash.distance;
                        def.dashSpeed = dash.speed;
                        def.dashDamage = dash.damageOnHit;
                    }
                    break;
                case ClientContent.AbilityKind.Heal:
                    def.kind = AbilityKind.Heal;
                    var heal = so as ClientContent.HealAbilitySO;
                    if (heal != null)
                    {
                        def.healAmount = heal.amount;
                    }
                    break;
            }
            return def;
        }
    }
}

