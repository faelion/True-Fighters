using System;
using UnityEngine;
using ServerGame.Entities;

namespace ServerGame.Systems
{
    public class NpcSystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            foreach (var npcEntity in world.EntityRepo.GetByType(EntityType.Neutral))
            {
                if (!npcEntity.Health.IsAlive) continue;

                var npcComponent = npcEntity.Npc;
                if (npcComponent == null) continue;

                GameEntity targetEntity = null;
            if (npcComponent.targetEntityId != -1)
            {
                world.EntityRepo.TryGetEntity(npcComponent.targetEntityId, out targetEntity);
            }

            if (targetEntity == null)
            {
                foreach (var hero in world.HeroEntities)
                {
                    if (!hero.Health.IsAlive) continue;
                    float dx = hero.Transform.posX - npcEntity.Transform.posX;
                    float dy = hero.Transform.posY - npcEntity.Transform.posY;
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
                float dx = targetEntity.Transform.posX - npcEntity.Transform.posX;
                float dy = targetEntity.Transform.posY - npcEntity.Transform.posY;
                float dist2 = dx * dx + dy * dy;
                if (dist2 > npcComponent.followRange * npcComponent.followRange)
                {
                    npcComponent.targetEntityId = -1;
                    targetEntity = null;
                }
            }

            if (targetEntity == null) continue;

            float dx2 = targetEntity.Transform.posX - npcEntity.Transform.posX;
            float dy2 = targetEntity.Transform.posY - npcEntity.Transform.posY;
            float dist = Mathf.Sqrt(dx2 * dx2 + dy2 * dy2);
            if (dist < npcComponent.followRange && dist > npcComponent.stopRange)
            {
                npcEntity.Transform.posX += dx2 / dist * npcEntity.Movement.moveSpeed * dt;
                npcEntity.Transform.posY += dy2 / dist * npcEntity.Movement.moveSpeed * dt;
            }
            }
        }
    }
}
