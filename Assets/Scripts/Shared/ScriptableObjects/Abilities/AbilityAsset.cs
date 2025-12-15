using UnityEngine;

namespace ClientContent
{
    public abstract class AbilityAsset : ScriptableObject, Shared.ScriptableObjects.IEntityLogic
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
            if (!caster.TryGetComponent(out ServerGame.Entities.TransformComponent t)) return false;
            float dx = targetX - t.posX;
            float dy = targetY - t.posY;
            float dist2 = dx * dx + dy * dy;
            if (dist2 > range * range) return false;

            dir = new Vector2(dx, dy);
            if (dir.sqrMagnitude <= 0.0001f) dir = Vector2.right;
            else dir.Normalize();
            return true;
        }



        public virtual void OnEntitySpawn(ServerGame.ServerWorld world, ServerGame.Entities.GameEntity entity) { }
        public virtual void OnEntityTick(ServerGame.ServerWorld world, ServerGame.Entities.GameEntity entity, float dt) { }
        public virtual void OnEntityCollision(ServerGame.ServerWorld world, ServerGame.Entities.GameEntity me, ServerGame.Entities.GameEntity other) { }
        public virtual void OnEntityDespawn(ServerGame.ServerWorld world, ServerGame.Entities.GameEntity entity) { }


        public abstract void ClientHandleEvent(IGameEvent evt, GameObject contextRoot);
        
        public virtual void ClientOnCast(AbilityCastedEvent evt, GameObject contextRoot) 
        { 
            if (contextRoot)
            {
                var anim = contextRoot.GetComponent<NetworkHeroAnimator>();
                if (anim) anim.TriggerAbility(evt.SourceId);
            }
        }
    }
}
