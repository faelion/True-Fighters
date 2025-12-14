using System;
using UnityEngine;
using ServerGame;
using ServerGame.Entities;

namespace Shared.Effects
{
    [CreateAssetMenu(menuName = "Content/Effects/Dot Effect", fileName = "DotEffect")]
    public class DotEffect : Effect
    {
        public float DamagePerTick = 5f;
        public float Interval = 1f;

        public override void OnStart(ServerWorld world, ActiveEffect runtime, GameEntity target)
        {
             Debug.Log($"[DotEffect] Applied DOT to {target.Id}");
        }

        public override void OnTick(ServerWorld world, ActiveEffect runtime, GameEntity target, float dt)
        {
            runtime.TickTimer += dt;
            if (runtime.TickTimer >= Interval)
            {
                runtime.TickTimer -= Interval;
                if (target.TryGetComponent(out HealthComponent health))
                {
                    health.currentHp -= DamagePerTick;
                    Debug.Log($"[DotEffect] Tick damage {DamagePerTick} on {target.Id}. HP: {health.currentHp}");
                }
            }
        }
    }
}
