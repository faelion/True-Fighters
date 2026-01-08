using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability Assets/AoEAllies", fileName = "AoEAbilityAssetAllies")]
    public class AoEAbilityAssetAllies : AbilityAsset
    {

        [Header("Lifetime")]
        public int projectileLifeMs = 5000;

        [Header("Effects")]
        public System.Collections.Generic.List<Shared.Effects.Effect> onHitEffects;

        [Header("Collision")]
        public float collisionRadius = 2.0f;
        public bool isTrigger = true;
        public float distanceFromCaster = 3.0f;

        [Header("View")]
        public GameObject aoePrefab;

        public override GameObject GetPreviewPrefab() => aoePrefab;

        private bool hasHit = false;

        public override bool ServerTryCast(ServerGame.ServerWorld world, int playerId, float targetX, float targetY)
        {
            if (!ValidateCastRange(world, playerId, targetX, targetY, out var dir)) return false;
            
            var caster = world.EnsurePlayer(playerId);
            if (!caster.TryGetComponent(out ServerGame.Entities.TransformComponent casterTransform)) return false;

            hasHit = false;

            float dx = targetX - casterTransform.posX;
            float dy = targetY - casterTransform.posY;
            float distSq = dx * dx + dy * dy;

            float dist = Mathf.Sqrt(distSq);
            float nx = dx / dist;
            float ny = dy / dist;

            var projectile = world.EntityRepo.CreateEntity(ServerGame.Entities.EntityType.AoE); 
            projectile.OwnerPlayerId = playerId;
            projectile.ArchetypeId = id;

            float clampDist = (dist > distanceFromCaster) ? distanceFromCaster : dist;

            var t = new ServerGame.Entities.TransformComponent 
            {
                posX = casterTransform.posX + (nx * clampDist),
                posY = casterTransform.posY + (ny * clampDist),

                rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg
            };
            projectile.AddComponent(t);

            projectile.AddComponent(new ServerGame.Entities.LifetimeComponent 
            { 
                remainingTime = projectileLifeMs / 1000f 
            });

            projectile.AddComponent(new ServerGame.Entities.CollisionComponent { radius = collisionRadius, isTrigger = isTrigger });
            
            if (caster.TryGetComponent(out ServerGame.Entities.TeamComponent team))
            {
                projectile.AddComponent(new ServerGame.Entities.TeamComponent { teamId = team.teamId, friendlyFire = team.friendlyFire });
            }

            // Optimization/Hack: Pass effects to the projectile entity? 
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
            if (other.TryGetComponent(out ServerGame.Entities.TeamComponent otherTeam))
            {
                if (me.TryGetComponent(out ServerGame.Entities.TeamComponent myTeam))
                {
                    if (myTeam.teamId == otherTeam.teamId)
                    {
                        if (onHitEffects != null)
                        {
                            foreach (var effect in onHitEffects)
                            {
                                if (effect != null && !hasHit)
                                {
                                    hasHit = true;
                                    effect.Apply(world, me, other);
                                }
                            }
                        }
                        return;
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
