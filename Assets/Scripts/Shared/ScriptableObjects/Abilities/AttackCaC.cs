using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability Assets/AttackCaC", fileName = "AttackCaCAbilityAsset")]
    public class AttackCaCAbilityAsset : AbilityAsset
    {
        [Header("Lifetime")]
        public int projectileLifeMs = 1000;

        [Header("Effects")]
        public System.Collections.Generic.List<Shared.Effects.Effect> onHitEffects;

        [Header("Collision")]
        public float collisionRadius = 1.0f;
        public bool isTrigger = true;

        [Header("View")]
        public GameObject CaCPrefab;

        [Header("Spawn Distance")]
        public float spawnDistance = 1.5f;

        private bool hasHit = false;

        public override bool ServerTryCast(ServerGame.ServerWorld world, int playerId, float targetX, float targetY)
        {            
            var caster = world.EnsurePlayer(playerId);
            if (!caster.TryGetComponent(out ServerGame.Entities.TransformComponent casterTransform)) return false;

            var melee = world.EntityRepo.CreateEntity(ServerGame.Entities.EntityType.Melee);
            hasHit = false;
            melee.OwnerPlayerId = playerId;
            melee.ArchetypeId = id;

            float angleRad = casterTransform.rotZ * Mathf.Deg2Rad;
            float dirX = Mathf.Sin(angleRad);
            float dirY = Mathf.Cos(angleRad);

            var t = new ServerGame.Entities.TransformComponent
            {
                posX = casterTransform.posX + (dirX * spawnDistance),
                posY = casterTransform.posY + (dirY * spawnDistance),
                rotZ = casterTransform.rotZ
            };
            melee.AddComponent(t);

            melee.AddComponent(new ServerGame.Entities.LifetimeComponent 
            { 
                remainingTime = projectileLifeMs / 1000f 
            });

            melee.AddComponent(new ServerGame.Entities.CollisionComponent { radius = collisionRadius, isTrigger = isTrigger });
            
            if (caster.TryGetComponent(out ServerGame.Entities.TeamComponent team))
            {
                melee.AddComponent(new ServerGame.Entities.TeamComponent { teamId = team.teamId, friendlyFire = team.friendlyFire });
            }

            // Optimization/Hack: Pass effects to the projectile entity? 
            // Ideally projectile knows its Archetype which leads back to this Asset, 
            // so we look up the effects from the Asset when collision happens (which is what we do below).
            
            world.EnqueueEvent(new EntitySpawnEvent
            {
                CasterId = melee.Id,
                PosX = t.posX,
                PosY = t.posY,
                ArchetypeId = melee.ArchetypeId,
                TeamId = team != null ? team.teamId : 0
            });

            return true;
        }

        public override void OnEntityCollision(ServerGame.ServerWorld world, ServerGame.Entities.GameEntity me, ServerGame.Entities.GameEntity other)
        {
            if (other.TryGetComponent(out ServerGame.Entities.TeamComponent otherTeam))
            {
                if (me.TryGetComponent(out ServerGame.Entities.TeamComponent myTeam))
                {
                    if (myTeam.IsEnemyTo(otherTeam))
                    {
                        // Apply Effects
                        if(hasHit) return; // Prevent multiple hits
                        if (onHitEffects != null)
                        {
                            foreach (var effect in onHitEffects)
                            {
                                if (effect != null)
                                {
                                    effect.Apply(world, me, other);
                                    hasHit = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void ClientHandleEvent(IGameEvent evt, GameObject contextRoot)
        {
            // Projectiles are now spawned via generic EntitySpawnEvent handled by NetEntitySpawner.
            // This method is no longer used for projectile spawning but kept for inheritance.
        }
    }
}
