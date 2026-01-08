using System;
using UnityEngine;
using ServerGame;
using ServerGame.Entities;

namespace Shared.Effects
{
    [CreateAssetMenu(menuName = "Content/Effects/Speed Speed", fileName = "Speed")]
    public class SpeedEffect : Effect
    {
        [Tooltip("Multiplier of speed to increase.")]
        public float speedMultiplier = 1.5f;

        private float initialSpeed = 0.0f; 
        public override void OnStart(ServerWorld world, ActiveEffect runtime, GameEntity target)
        {
            if(target.TryGetComponent(out MovementComponent move))
            {
                initialSpeed = move.moveSpeed;
                move.moveSpeed *= speedMultiplier;
            }
        }

        public override void OnRemove(ServerWorld world, ActiveEffect runtime, GameEntity target)
        {
            if (target.TryGetComponent(out MovementComponent move))
            {
                move.moveSpeed = initialSpeed;
            }
        }
    }
}
