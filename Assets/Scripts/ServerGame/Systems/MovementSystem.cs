using System;
using UnityEngine;

namespace ServerGame.Systems
{
    public class MovementSystem : ISystem
    {
        public void Tick(ServerWorld world, float dt)
        {
            foreach (var p in world.Players.Values)
            {
                if (p.hit)
                {
                    p.hitTimer += dt;
                    if (p.hitTimer >= ServerWorld.HitFlashDuration)
                    {
                        p.hitTimer = 0f;
                        p.hit = false;
                    }
                }

                if (!p.hasDest) continue;
                float dx = p.destX - p.posX;
                float dy = p.destY - p.posY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist < 0.05f)
                {
                    p.posX = p.destX; p.posY = p.destY; p.hasDest = false;
                }
                else
                {
                    float move = p.speed * dt;
                    if (move > dist) move = dist;
                    p.posX += dx / dist * move;
                    p.posY += dy / dist * move;
                    p.rotZ = (float)(Math.Atan2(dy, dx) * (180.0 / Math.PI));
                }
            }
        }
    }
}
