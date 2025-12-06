using UnityEngine;

namespace ClientContent
{
    public abstract class UnitSO : ScriptableObject, Shared.ScriptableObjects.IEntityLogic
    {
        public string id;
        public string displayName;
        public float baseHp = 100f;
        public float moveSpeed = 4f;
        
        [Header("Movement")]
        public Shared.ScriptableObjects.MovementStrategySO movementStrategy;

        public virtual void OnEntitySpawn(ServerGame.ServerWorld world, ServerGame.Entities.GameEntity entity) { }
        public virtual void OnEntityTick(ServerGame.ServerWorld world, ServerGame.Entities.GameEntity entity, float dt) { }
        public virtual void OnEntityCollision(ServerGame.ServerWorld world, ServerGame.Entities.GameEntity me, ServerGame.Entities.GameEntity other) { }
        public virtual void OnEntityDespawn(ServerGame.ServerWorld world, ServerGame.Entities.GameEntity entity) { }
    }
}
