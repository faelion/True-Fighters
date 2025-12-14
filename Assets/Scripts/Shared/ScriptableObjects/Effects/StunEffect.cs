using System;
using UnityEngine;
using ServerGame;
using ServerGame.Entities;

namespace Shared.Effects
{
    [CreateAssetMenu(menuName = "Content/Effects/Stun Effect", fileName = "StunEffect")]
    public class StunEffect : Effect
    {

        public override void OnStart(ServerWorld world, ActiveEffect runtime, GameEntity target)
        {
            if (target.TryGetComponent(out MovementComponent move))
            {
                move.DisabledCount++;
            }
            if (target.TryGetComponent(out CombatComponent combat))
            {
                combat.DisabledCount++;
            }
            Debug.Log($"[StunEffect] Applied Stun to {target.Id}");
        }

        public override void OnRemove(ServerWorld world, ActiveEffect runtime, GameEntity target)
        {
            if (target.TryGetComponent(out MovementComponent move))
            {
                move.DisabledCount--;
                if (move.DisabledCount < 0) move.DisabledCount = 0;
            }
            if (target.TryGetComponent(out CombatComponent combat))
            {
                combat.DisabledCount--;
                if (combat.DisabledCount < 0) combat.DisabledCount = 0;
            }
            Debug.Log($"[StunEffect] Removed Stun from {target.Id}");
        }
    }
}
