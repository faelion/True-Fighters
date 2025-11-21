using UnityEngine;

namespace ClientContent
{
    public abstract class AbilityAsset : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public string defaultKey = "";

        [Header("Balance")]
        public float range = 12f;
        public float castTime = 0f;
        public float cooldown = 2f;

        // Server-side behavior: perform validation, spawn effects and enqueue events
        public abstract bool ServerTryCast(ServerGame.ServerWorld world, int playerId, float targetX, float targetY);

        // Hooks for persistent effects
        public virtual void OnEffectSpawn(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff) { }
        public virtual bool OnEffectTick(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, float dt) { return true; }
        public virtual void OnEffectHit(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, int targetEntityId) { }
        public virtual void OnEffectExpired(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff) { }

        // Server-side event population for persistent effects (spawn/update).
        public virtual void EmitEvents(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, int tick, System.Collections.Generic.IList<IGameEvent> buffer) { }
        public virtual bool EmitDespawnEvent(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, out IGameEvent evt) { evt = null; return false; }

        // Client-side view handler: react to typed game events for this ability id
        public abstract void ClientHandleEvent(IGameEvent evt, GameObject contextRoot);
    }
}
