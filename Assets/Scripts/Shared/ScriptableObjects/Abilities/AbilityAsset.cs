using UnityEngine;

namespace ClientContent
{
    public abstract class AbilityAsset : ScriptableObject, Shared.ScriptableObjects.IEntityLogic
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public string defaultKey = "";

        [Header("Casting")]
        [Tooltip("Visual effect to spawn on the CASTER while casting (e.g. magic circle, glowing hands).")]
        public GameObject castingEffectPrefab;

        public enum CastingPreviewMode { None, MainPrefabAtTarget, MainPrefabAtCaster, MainPrefabAtCasterNoFollow }
        
        [Tooltip("Controls if/where the ability's main prefab should be shown during the cast.")]
        public CastingPreviewMode castingPreviewMode;
        
        [Header("Balance")]
        public float range = 12f;
        public float castTime;
        public float cooldown;
        [Tooltip("If true, moving will interrupt the cast. If false, you can cast while moving.")]
        public bool interruptOnMove = false;
        
        [Tooltip("If true, you cannot move while casting. Move inputs will be ignored.")]
        public bool stopWhileCasting = true;

        public virtual GameObject GetPreviewPrefab() => null;

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
