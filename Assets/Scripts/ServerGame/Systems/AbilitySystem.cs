using System;
using System.Collections.Generic;
using ClientContent;
using UnityEngine;

namespace ServerGame.Systems
{

    public class AbilitySystem : ISystem
    {

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
            // Update Cooldown Components
            foreach (var entity in world.HeroEntities)
            {
                if (entity.TryGetComponent(out ServerGame.Entities.CooldownComponent cd))
                {
                    if (cd.cdQ > 0) cd.cdQ -= dt;
                    if (cd.cdW > 0) cd.cdW -= dt;
                    if (cd.cdE > 0) cd.cdE -= dt;
                    if (cd.cdR > 0) cd.cdR -= dt;
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
                        // Grace Period: Don't interrupt in the first 0.15s to allow velocity to zero out from Stop command or jitter
                        float elapsedTime = casting.TotalTime - casting.Timer;
                        
                        if ((castAbility.interruptOnMove || castAbility.stopWhileCasting) && elapsedTime > 0.15f)
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
            
            // Ensure Cooldown Component Exists (Lazy Init)
            if (!caster.TryGetComponent(out ServerGame.Entities.CooldownComponent cdComp))
            {
                cdComp = new ServerGame.Entities.CooldownComponent();
                caster.AddComponent(cdComp);
            }

            // If already casting, ignore new input (or queue it)
            if (casting.IsCasting) return false;

            if (!world.AbilityBooks.TryGetValue(playerId, out var book) || book == null || !book.TryGetValue(key, out var ability))
                return false;

            // Block input if dead
            if (caster.TryGetComponent(out ServerGame.Entities.HealthComponent h) && h.IsDead)
                return false;

            if (!ClientContent.ContentAssetRegistry.Abilities.ContainsKey(ability.id))
                ClientContent.ContentAssetRegistry.Abilities[ability.id] = ability;

            // Check for silence/stun/disable
            if (caster.TryGetComponent(out ServerGame.Entities.CombatComponent combat) && !combat.IsActive)
            {
                return false;
            }

            // Check Cooldown
            if (cdComp.GetCooldown(key) > 0f) return false;

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

            // Set Cooldown
            var caster = world.EnsurePlayer(playerId);
            if (caster.TryGetComponent(out ServerGame.Entities.CooldownComponent cdComp))
            {
                cdComp.SetCooldown(key, ability.cooldown);
            }
            return true;
        }
    }
}
