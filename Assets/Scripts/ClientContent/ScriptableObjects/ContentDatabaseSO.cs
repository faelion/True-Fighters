using System.Collections.Generic;
using UnityEngine;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Database", fileName = "ContentDatabase")]
    public class ContentDatabaseSO : ScriptableObject
    {
        public string defaultHeroId = "default";
        public List<BaseAbilitySO> abilities = new List<BaseAbilitySO>();
        public List<HeroSO> heroes = new List<HeroSO>();
    }
}

