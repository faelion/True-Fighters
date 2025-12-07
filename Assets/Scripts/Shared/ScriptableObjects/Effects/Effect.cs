using System;
using UnityEngine;
using ServerGame;
using ServerGame.Entities;

namespace Shared.Effects
{
    public abstract class Effect : ScriptableObject
    {
        // Apply method called by the server when the effect should trigger (e.g. on hit)
        public abstract void Apply(ServerWorld world, GameEntity source, GameEntity target);

        // Optional: Called every tick while active (for dots, movement, etc)
        public virtual void OnTick(ServerWorld world, ActiveEffect runtime, GameEntity target, float dt) { }

        // Optional: Called when the effect expires or is removed
        public virtual void OnRemove(ServerWorld world, ActiveEffect runtime, GameEntity target) { }
    }
}
