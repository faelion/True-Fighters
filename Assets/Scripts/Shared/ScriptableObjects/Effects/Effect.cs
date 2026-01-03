using System;
using UnityEngine;
using ServerGame;
using ServerGame.Entities;

namespace Shared.Effects
{
    public abstract class Effect : ScriptableObject
    {
        // Identification
        public string id;
        public float Duration = 0f;

        public virtual void Apply(ServerWorld world, GameEntity source, GameEntity target, Vector3? targetPos = null)
        {
            if (target.TryGetComponent(out StatusEffectComponent status))
            {
                status.AddEffect(this, Duration, source, targetPos);
            }
        }

        // --- Server Logic ---
        public virtual void OnStart(ServerWorld world, ActiveEffect runtime, GameEntity target) { }
        public virtual void OnTick(ServerWorld world, ActiveEffect runtime, GameEntity target, float dt) { }
        public virtual void OnRemove(ServerWorld world, ActiveEffect runtime, GameEntity target) { }

        // --- Client Visuals ---
        public virtual void ClientOnStart(GameObject targetVisuals) { }
        public virtual void ClientOnTick(GameObject targetVisuals, float dt) { }
        public virtual void ClientOnRemove(GameObject targetVisuals) { }
    }
}
