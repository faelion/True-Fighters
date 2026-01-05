using System;
using UnityEngine;
using ServerGame;
using ServerGame.Entities;

namespace Shared.Effects
{
    [CreateAssetMenu(menuName = "Content/Effects/Duration Heal", fileName = "HealDuration")]
    public class HealDuration : Effect
    {
        public float HealPerTick = 5f;
        public float Interval = 1f;
        public override void OnTick(ServerWorld world, ActiveEffect runtime, GameEntity target, float dt)
        {
            runtime.TickTimer += dt;
            if (runtime.TickTimer >= Interval)
            {
                runtime.TickTimer -= Interval;
                if (target.TryGetComponent(out HealthComponent health))
                {
                    health.currentHp += HealPerTick;
                    if (health.currentHp > health.maxHp)
                    {
                        health.currentHp = health.maxHp;
                    }
                    Debug.Log($"[DotEffect] Tick heal {HealPerTick} on {target.Id}. HP: {health.currentHp}");
                }
            }
        }
    }
}
