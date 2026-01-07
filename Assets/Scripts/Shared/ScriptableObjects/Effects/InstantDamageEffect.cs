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

        private void OnEnable()
        {
            destroyVfxOnEnd = false; // Instant effects should not destroy immediately
            if (vfxDuration <= 0f) vfxDuration = 1.5f; // Default safety
        }

        public override void OnStart(ServerWorld world, ActiveEffect runtime, GameEntity target)
        {
            if (target.TryGetComponent(out HealthComponent health))
            {
                health.currentHp -= Amount;
                Debug.Log($"[InstantDamageEffect] Dealt {Amount} damage to entity {target.Id}. HP: {health.currentHp}");
            }
        }
    }
}
