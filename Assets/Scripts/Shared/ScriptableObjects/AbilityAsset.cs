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


        public abstract bool ServerTryCast(ServerGame.ServerWorld world, int playerId, float targetX, float targetY);

        protected bool ValidateCastRange(ServerGame.ServerWorld world, int playerId, float targetX, float targetY, out Vector2 dir)
        {
            dir = Vector2.right;
            var caster = world.EnsurePlayer(playerId);
            float dx = targetX - caster.Transform.posX;
            float dy = targetY - caster.Transform.posY;
            float dist2 = dx * dx + dy * dy;
            if (dist2 > range * range) return false;

            dir = new Vector2(dx, dy);
            if (dir.sqrMagnitude <= 0.0001f) dir = Vector2.right;
            else dir.Normalize();
            return true;
        }


        public virtual void OnEffectSpawn(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff) { }
        public virtual bool OnEffectTick(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, float dt) { return true; }
        public virtual void OnEffectHit(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, int targetEntityId) { }
        public virtual void OnEffectExpired(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff) { }


        public virtual void EmitEvents(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, int tick, System.Collections.Generic.IList<IGameEvent> buffer) { }
        public virtual bool EmitDespawnEvent(ServerGame.ServerWorld world, ServerGame.AbilityEffect eff, out IGameEvent evt) { evt = null; return false; }


        public abstract void ClientHandleEvent(IGameEvent evt, GameObject contextRoot);
    }
}
