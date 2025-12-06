using UnityEngine;
using ServerGame.Entities;

namespace Shared.ScriptableObjects
{
    public abstract class MovementStrategySO : ScriptableObject
    {
        public abstract void UpdateMovement(ServerGame.ServerWorld world, GameEntity entity, float dt);
    }
}
