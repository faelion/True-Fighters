using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Hero", fileName = "Hero")]
    public class HeroSO : UnitSO
    {
        [Serializable]
        public struct Binding
        {
            public string key; // "Q","W","E","R" (or any)
            public AbilityAsset ability;
        }
        public List<Binding> bindings = new List<Binding>();
        public GameObject heroPrefab;
    }
}
