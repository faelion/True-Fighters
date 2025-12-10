using UnityEngine;
using ServerGame;

namespace Shared.ScriptableObjects
{
    public abstract class VictoryConditionSO : ScriptableObject
    {
        /// <summary>
        /// Checks if the victory condition is met.
        /// </summary>
        /// <param name="world">The server world context.</param>
        /// <param name="time">Current match time in seconds.</param>
        /// <param name="winner">Outputs the winning team ID (or -1 if no winner yet/draw).</param>
        /// <returns>True if the game should end.</returns>
        public abstract bool CheckVictory(ServerWorld world, float time, out int winner);
    }
}
