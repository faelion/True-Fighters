using ServerGame.Entities;

namespace ServerGame.AI
{
    public abstract class AIBehavior
    {
        /// <summary>
        /// Updates the AI behavior logic.
        /// </summary>
        /// <param name="world">The server world context.</param>
        /// <param name="entity">The entity executing this behavior.</param>
        /// <param name="dt">Delta time in seconds.</param>
        public abstract void Tick(ServerWorld world, GameEntity entity, float dt);
    }
}
