using System.Collections.Generic;
using UnityEngine;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Database", fileName = "ContentDatabase")]
    public class ContentDatabaseSO : ScriptableObject
    {
        public string defaultHeroId = "default";
        public string defaultNeutralId = "neutral_default";
        public List<AbilityAsset> abilities = new List<AbilityAsset>();
        public List<HeroSO> heroes = new List<HeroSO>();
        public List<NeutralEntitySO> neutrals = new List<NeutralEntitySO>();
        public List<Shared.ScriptableObjects.GameModeSO> gameModes = new List<Shared.ScriptableObjects.GameModeSO>();
    }
}
