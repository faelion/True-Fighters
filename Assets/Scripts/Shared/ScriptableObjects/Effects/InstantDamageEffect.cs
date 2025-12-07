using System;
using UnityEngine;
using ServerGame;
using ServerGame.Entities;

namespace Shared.Effects
{
    [CreateAssetMenu(menuName = "Content/Effects/Instant Damage", fileName = "InstantDamage")]
    public class InstantDamageEffect : Effect
    {
        [Tooltip("Amount of damage to deal instantly.")]
        public float Amount;

        public override void Apply(ServerWorld world, GameEntity source, GameEntity target)
        {
            if (target.TryGetComponent(out HealthComponent health))
            {
                health.currentHp -= Amount;
                Debug.Log($"[InstantDamageEffect] Dealt {Amount} damage to entity {target.Id}. HP: {health.currentHp}");
            }
        }
    }
}
