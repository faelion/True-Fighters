using System;
using UnityEngine;

namespace ServerGame.Systems
{
    public class NpcSystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            var npc = world.Npc;
            if (npc == null) return;

            if (npc.target == null && world.Players.Count > 0)
            {
                foreach (var p in world.Players.Values)
                {
                    float dx = p.posX - npc.posX;
                    float dy = p.posY - npc.posY;
                    if (dx * dx + dy * dy <= npc.followRange * npc.followRange)
                    {
                        npc.target = p;
                        break;
                    }
                }
            }
            else if (npc.target != null)
            {
                float dx = npc.target.posX - npc.posX;
                float dy = npc.target.posY - npc.posY;
                float dist2 = dx * dx + dy * dy;
                if (dist2 > npc.followRange * npc.followRange)
                {
                    npc.target = null;
                }
            }

            if (npc.target == null) return;

            float dx2 = npc.target.posX - npc.posX;
            float dy2 = npc.target.posY - npc.posY;
            float dist = Mathf.Sqrt(dx2 * dx2 + dy2 * dy2);
            if (dist < npc.followRange && dist > npc.stopRange)
            {
                npc.posX += dx2 / dist * npc.speed * dt;
                npc.posY += dy2 / dist * npc.speed * dt;
            }
        }
    }
}
