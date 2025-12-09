using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability Assets/Projectile", fileName = "ProjectileAbilityAsset")]
    public class ProjectileAbilityAsset : AbilityAsset
    {
        [Header("Movement")]
        public Shared.ScriptableObjects.MovementStrategySO movementStrategy;
        public float projectileSpeed = 8f;

        [Header("Lifetime")]
        public int projectileLifeMs = 1500;

        [Header("Effects")]
        public System.Collections.Generic.List<Shared.Effects.Effect> onHitEffects;

        [Header("Collision")]
        public float collisionRadius = 0.25f;
        public bool isTrigger = true;

        [Header("View")]
        public GameObject projectilePrefab;

        public override bool ServerTryCast(ServerGame.ServerWorld world, int playerId, float targetX, float targetY)
        {
            if (!ValidateCastRange(world, playerId, targetX, targetY, out var dir)) return false;
            
            var caster = world.EnsurePlayer(playerId);
            if (!caster.TryGetComponent(out ServerGame.Entities.TransformComponent casterTransform)) return false;

            float dx = targetX - casterTransform.posX;
            float dy = targetY - casterTransform.posY;
            float distSq = dx * dx + dy * dy;

            float dist = Mathf.Sqrt(distSq);
            float nx = dx / dist;
            float ny = dy / dist;

            float angle = Mathf.Atan2(nx, ny) * Mathf.Rad2Deg;
            casterTransform.rotZ = angle;

            var projectile = world.EntityRepo.CreateEntity(ServerGame.Entities.EntityType.Projectile); 
            projectile.OwnerPlayerId = playerId;
            projectile.ArchetypeId = id; 
            
            var t = new ServerGame.Entities.TransformComponent 
            { 
                posX = casterTransform.posX, 
                posY = casterTransform.posY,
                rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg
            };
            projectile.AddComponent(t);

            var movement = new ServerGame.Entities.MovementComponent
            {
                strategy = movementStrategy, 
                moveSpeed = projectileSpeed,
                velX = dir.x * projectileSpeed,
                velY = dir.y * projectileSpeed
            };
            projectile.AddComponent(movement);

            projectile.AddComponent(new ServerGame.Entities.LifetimeComponent 
            { 
                remainingTime = projectileLifeMs / 1000f 
            });

            projectile.AddComponent(new ServerGame.Entities.CollisionComponent { radius = collisionRadius, isTrigger = isTrigger });
            
            if (caster.TryGetComponent(out ServerGame.Entities.TeamComponent team))
            {
                projectile.AddComponent(new ServerGame.Entities.TeamComponent { teamId = team.teamId, friendlyFire = team.friendlyFire });
            }

            // Ideally projectile knows its Archetype which leads back to this Asset, 
            // so we look up the effects from the Asset when collision happens (which is what we do below).
            
            world.EnqueueEvent(new EntitySpawnEvent
            {
                CasterId = projectile.Id,
                PosX = t.posX,
                PosY = t.posY,
                ArchetypeId = projectile.ArchetypeId,
                TeamId = team != null ? team.teamId : 0
            });

            return true;
        }

        public override void OnEntityCollision(ServerGame.ServerWorld world, ServerGame.Entities.GameEntity me, ServerGame.Entities.GameEntity other)
        {
            // Prevent self-damage
            if (me.OwnerPlayerId == other.Id) return;

            if (other.TryGetComponent(out ServerGame.Entities.TeamComponent otherTeam))
            {
                if (me.TryGetComponent(out ServerGame.Entities.TeamComponent myTeam))
                {
                    if (myTeam.IsEnemyTo(otherTeam))
                    {
                        // Apply Effects
                        if (onHitEffects != null)
                        {
                            foreach (var effect in onHitEffects)
                            {
                                if (effect != null)
                                {
                                    effect.Apply(world, me, other);
                                }
                            }
                        }

                        world.DespawnEntity(me.Id); // Destroy projectile on hit
                    }
                }
            }
        }

        public override void ClientHandleEvent(IGameEvent evt, GameObject contextRoot)
        {
        }
    }
}
