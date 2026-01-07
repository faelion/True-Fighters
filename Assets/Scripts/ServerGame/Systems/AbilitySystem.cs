using System;
using System.Collections.Generic;
using ClientContent;
using UnityEngine;

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

            // Update Casting Components
            foreach (var entity in world.HeroEntities)
            {
                if (entity.TryGetComponent(out ServerGame.Entities.CastingComponent casting) && casting.IsCasting)
                {
                    casting.Timer -= dt;
                    
                    // Fetch ability to check interruption rules
                    if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(casting.AbilityId, out var castAbility))
                    {
                        // Interruption Logic (e.g. Movement)
                        // Only interrupt if the ability explicitly says so
                        if (castAbility.interruptOnMove)
                        {
                            if (entity.TryGetComponent(out ServerGame.Entities.MovementComponent move))
                            {
                                if (move.velX != 0 || move.velY != 0)
                                {
                                    // Interrupt!
                                    casting.IsCasting = false;
                                    Debug.Log($"[AbilitySystem] Casting Interrupted by Movement for {entity.Id}");
                                    continue;
                                }
                            }
                        }
                    }

                    if (casting.Timer <= 0f)
                    {
                        // Cast Finished!
                        casting.IsCasting = false;
                        ExecuteCast(world, entity.Id, casting.AbilityId, casting.Key, casting.TargetX, casting.TargetY);
                    }
                }
            }
        }

        public bool TryCast(ServerGame.ServerWorld world, int playerId, string key, float targetX, float targetY)
        {
            var caster = world.GetHeroEntity(playerId) ?? world.EnsurePlayer(playerId);

            // Ensure Casting Component Exists (Lazy Init)
            if (!caster.TryGetComponent(out ServerGame.Entities.CastingComponent casting))
            {
                casting = new ServerGame.Entities.CastingComponent();
                caster.AddComponent(casting);
            }

            // If already casting, ignore new input (or queue it, but ignoring is simpler)
            if (casting.IsCasting) return false;

            if (!world.AbilityBooks.TryGetValue(playerId, out var book) || book == null || !book.TryGetValue(key, out var ability))
                return false;

            // Block input if dead
            if (caster.TryGetComponent(out ServerGame.Entities.HealthComponent h) && h.IsDead)
                return false;

            if (!ClientContent.ContentAssetRegistry.Abilities.ContainsKey(ability.id))
                ClientContent.ContentAssetRegistry.Abilities[ability.id] = ability;

            if (!cooldowns.TryGetValue(playerId, out var cdByKey))
            {
                cdByKey = new Dictionary<string, float>();
                cooldowns[playerId] = cdByKey;
            }

            // Check for silence/stun/disable
            if (caster.TryGetComponent(out ServerGame.Entities.CombatComponent combat) && !combat.IsActive)
            {
                return false;
            }

            if (cdByKey.TryGetValue(key, out float cd) && cd > 0f)
                return false;

            // Check Cast Time
            if (ability.castTime > 0f)
            {
                // Start Casting
                casting.IsCasting = true;
                casting.AbilityId = ability.id;
                casting.Key = key;
                casting.TotalTime = ability.castTime;
                casting.Timer = ability.castTime;
                casting.TargetX = targetX;
                casting.TargetY = targetY;

                // Stop Movement only if the ability demands it
                if (ability.interruptOnMove || ability.stopWhileCasting)
                {
                    if (caster.TryGetComponent(out ServerGame.Entities.MovementComponent move))
                    {
                        move.velX = 0;
                        move.velY = 0;
                        move.destX = caster.TryGetComponent(out ServerGame.Entities.TransformComponent t) ? t.posX : move.destX;
                        move.destY = caster.TryGetComponent(out ServerGame.Entities.TransformComponent t2) ? t2.posY : move.destY;
                        move.hasDestination = false;
                        move.pathCorners = null;
                        
                        Debug.Log($"[AbilitySystem] Casting {ability.id} started. Movement stopped (interruptOnMove=true).");
                    }
                }

                return true;
            }
            else
            {
                // Instant Cast
                return ExecuteCast(world, playerId, ability.id, key, targetX, targetY);
            }
        }

        private bool ExecuteCast(ServerGame.ServerWorld world, int playerId, string abilityId, string key, float targetX, float targetY)
        {
            if (!ClientContent.ContentAssetRegistry.Abilities.TryGetValue(abilityId, out var ability)) return false;
            // var ability = ClientContent.ContentAssetRegistry.Abilities[abilityId]; // Redundant re-fetch removed

            if (!ability.ServerTryCast(world, playerId, targetX, targetY))
                return false;
            
            // Raise Event
            world.EnqueueEvent(new AbilityCastedEvent
            {
                SourceId = ability.id,
                CasterId = playerId,
                TargetX = targetX,
                TargetY = targetY
            });

            if (cooldowns.TryGetValue(playerId, out var cdByKey))
            {
                cdByKey[key] = ability.cooldown;
            }
            return true;
        }
    }
}
