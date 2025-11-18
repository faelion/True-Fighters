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

        // Per-tick server update of a persistent effect (optional). Return true if effect remains alive.
        public virtual bool ServerUpdateEffect(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, float dt) { return true; }

        // Server-side event population (spawn/update/despawn). Return true to emit the message.
        public virtual bool ServerPopulateSpawnEvent(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, int tick, out IGameEvent evt) { evt = null; return false; }
        public virtual bool ServerPopulateUpdateEvent(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, int tick, out IGameEvent evt) { evt = null; return false; }
        public virtual bool ServerPopulateDespawnEvent(ServerGame.ServerWorld world, int effectId, int tick, out IGameEvent evt) { evt = null; return false; }

        // Client-side view handler: react to typed game events for this ability id
        public abstract void ClientHandleEvent(IGameEvent evt, GameObject contextRoot);
    }
}
