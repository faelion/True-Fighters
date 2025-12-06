using System;
using UnityEngine;
using ServerGame.Entities;

namespace ServerGame.Systems
{
    public class AIBehaviorSystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            foreach (var npcEntity in world.EntityRepo.GetByType(EntityType.Neutral))
            {
                if (!npcEntity.TryGetComponent(out HealthComponent health) || !health.IsAlive) continue;
                if (!npcEntity.TryGetComponent(out AIBehaviorComponent npcComponent)) continue;
                if (!npcEntity.TryGetComponent(out TransformComponent npcTransform)) continue;
                if (!npcEntity.TryGetComponent(out PlayerMovementComponent npcMovement)) continue;

                GameEntity targetEntity = null;
                if (npcComponent.targetEntityId != -1)
                {
                    world.EntityRepo.TryGetEntity(npcComponent.targetEntityId, out targetEntity);
                }

                if (targetEntity == null)
                {
                    foreach (var hero in world.HeroEntities)
                    {
                        if (!hero.TryGetComponent(out HealthComponent heroHealth) || !heroHealth.IsAlive) continue;
                        if (!hero.TryGetComponent(out TransformComponent heroTransform)) continue;
                        
                        float dx = heroTransform.posX - npcTransform.posX;
                        float dy = heroTransform.posY - npcTransform.posY;
                        if (dx * dx + dy * dy <= npcComponent.followRange * npcComponent.followRange)
                        {
                            targetEntity = hero;
                            npcComponent.targetEntityId = hero.Id;
                            break;
                        }
                    }
                }
                else
                {
                    if (!targetEntity.TryGetComponent(out TransformComponent targetTransform))
                    {
                        npcComponent.targetEntityId = -1;
                        targetEntity = null;
                    }
                    else
                    {
                        float dx = targetTransform.posX - npcTransform.posX;
                        float dy = targetTransform.posY - npcTransform.posY;
                        float dist2 = dx * dx + dy * dy;
                        if (dist2 > npcComponent.followRange * npcComponent.followRange)
                        {
                            npcComponent.targetEntityId = -1;
                            targetEntity = null;
                        }
                    }
                }

                if (targetEntity == null) continue;
                if (!targetEntity.TryGetComponent(out TransformComponent tTransform)) continue; // Double check

                float dx2 = tTransform.posX - npcTransform.posX;
                float dy2 = tTransform.posY - npcTransform.posY;
                float dist = Mathf.Sqrt(dx2 * dx2 + dy2 * dy2);
                if (dist < npcComponent.followRange && dist > npcComponent.stopRange)
                {
                    npcTransform.posX += dx2 / dist * npcMovement.moveSpeed * dt;
                    npcTransform.posY += dy2 / dist * npcMovement.moveSpeed * dt;
                }
            }
        }
    }
}
