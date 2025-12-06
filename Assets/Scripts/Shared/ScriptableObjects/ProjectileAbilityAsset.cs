using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability Assets/Projectile", fileName = "ProjectileAbilityAsset")]
    public class ProjectileAbilityAsset : AbilityAsset
    {
        [Header("Projectile Data")]
        public float projectileSpeed = 8f;
        public int projectileLifeMs = 1500;
        public float damage = 0f;

        [Header("View")]
        public GameObject projectilePrefab;
        public override bool ServerTryCast(ServerGame.ServerWorld world, int playerId, float targetX, float targetY)
        {
            if (!ValidateCastRange(world, playerId, targetX, targetY, out var dir)) return false;
            
            var caster = world.EnsurePlayer(playerId);
            if (!caster.TryGetComponent(out ServerGame.Entities.TransformComponent casterTransform)) return false;

            // Create Entity
            var projectile = world.EntityRepo.CreateEntity(ServerGame.Entities.EntityType.Projectile); 
            projectile.OwnerPlayerId = playerId;
            projectile.ArchetypeId = id; // Identify which AbilityAsset defines this entity
            
            // Add Components
            var t = new ServerGame.Entities.TransformComponent 
            { 
                posX = casterTransform.posX, 
                posY = casterTransform.posY,
                rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg
            };
            projectile.AddComponent(t);

            projectile.AddComponent(new ServerGame.Entities.ProjectileMovementComponent
            {
                speed = projectileSpeed,
                dirX = dir.x,
                dirY = dir.y,
                lifeMs = projectileLifeMs
            });

            projectile.AddComponent(new ServerGame.Entities.CollisionComponent { radius = 0.25f, isTrigger = true });
            
            if (caster.TryGetComponent(out ServerGame.Entities.TeamComponent team))
            {
                projectile.AddComponent(new ServerGame.Entities.TeamComponent { teamId = team.teamId, friendlyFire = team.friendlyFire });
            }

            // Broadcast Spawn
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
            // Simple damage logic
            if (other.TryGetComponent(out ServerGame.Entities.HealthComponent health) && other.TryGetComponent(out ServerGame.Entities.TeamComponent otherTeam))
            {
                if (me.TryGetComponent(out ServerGame.Entities.TeamComponent myTeam))
                {
                    if (myTeam.IsEnemyTo(otherTeam))
                    {
                        health.ApplyDamage(damage);
                        world.DespawnEntity(me.Id); // Destroy projectile on hit
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
