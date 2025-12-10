using UnityEngine;
using ServerGame;

namespace Shared.ScriptableObjects
{
    [CreateAssetMenu(fileName = "EndlessCondition", menuName = "Game/VictoryConditions/Endless")]
    public class EndlessVictorySO : VictoryConditionSO
    {
        public override bool CheckVictory(ServerWorld world, float time, out int winner)
        {
            winner = -1;
            return false; // Never ends
        }
    }
}
