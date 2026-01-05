using System;
using UnityEngine;
using ServerGame;
using ServerGame.Entities;

namespace Shared.Effects
{
    [CreateAssetMenu(menuName = "Content/Effects/Shield Sheild", fileName = "Shield")]
    public class ShieldEffect : Effect
    {
        [Tooltip("Amount of damage to deal instantly.")]

        private float initialLive = 0.0f; 
        public override void OnStart(ServerWorld world, ActiveEffect runtime, GameEntity target)
        {
            if (target.TryGetComponent(out HealthComponent health))
            {
                initialLive = health.currentHp;
            }
        }

        public override void OnTick(ServerWorld world, ActiveEffect runtime, GameEntity target, float dt)
        {
            base.OnTick(world, runtime, target, dt);
            if (target.TryGetComponent(out HealthComponent health))
            {
                health.currentHp = initialLive;
            }
        }
    }
}
