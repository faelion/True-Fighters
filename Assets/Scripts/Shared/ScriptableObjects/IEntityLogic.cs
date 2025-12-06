using ServerGame.Entities;
using ServerGame;

namespace Shared.ScriptableObjects
{
    public interface IEntityLogic
    {
        void OnEntitySpawn(ServerWorld world, GameEntity entity);
        void OnEntityTick(ServerWorld world, GameEntity entity, float dt);
        void OnEntityCollision(ServerWorld world, GameEntity me, GameEntity other);
        void OnEntityDespawn(ServerWorld world, GameEntity entity);
    }
}
