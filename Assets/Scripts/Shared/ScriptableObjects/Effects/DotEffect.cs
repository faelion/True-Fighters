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
        public float Duration = 5f;

        public override void Apply(ServerWorld world, GameEntity source, GameEntity target)
        {
            if (target.TryGetComponent(out StatusEffectComponent status))
            {
                status.AddEffect(this, Duration, source);
                Debug.Log($"[DotEffect] Applied DOT to {target.Id}");
            }
        }

        public override void OnTick(ServerWorld world, ActiveEffect runtime, GameEntity target, float dt)
        {
            // Simple timer usage; could use 'tickTimer' in runtime if added
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
