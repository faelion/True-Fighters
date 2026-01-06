using UnityEngine;

namespace Shared.ScriptableObjects.AI
{
    public abstract class AIStrategySO : ScriptableObject
    {
        /// <summary>
        /// Creates the runtime behavior instance for this strategy.
        /// </summary>
        public abstract ServerGame.AI.AIBehavior CreateBehavior();
    }
}
