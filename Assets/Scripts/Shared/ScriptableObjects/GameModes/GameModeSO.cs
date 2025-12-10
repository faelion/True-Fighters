using UnityEngine;
using System.Collections.Generic;

namespace Shared.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewGameMode", menuName = "Game/GameMode")]
    public class GameModeSO : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;

        [Header("Rules")]
        public TeamDefinition[] teams; // Empty = Free For All
        public int minPlayers = 1;
        public int maxPlayers = 10;
        public float matchDuration = 300f; // 0 = Infinite
        public float playerRespawnTime = 5f;
        public List<SceneReference> maps;

        [Header("Logic")]
        public VictoryConditionSO victoryCondition;
    }
}
