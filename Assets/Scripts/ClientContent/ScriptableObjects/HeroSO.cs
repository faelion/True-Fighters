using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Hero", fileName = "Hero")]
    public class HeroSO : ScriptableObject
    {
        [Serializable]
        public struct Binding
        {
            public string key; // e.g., "Q","W","E","R" (or any)
            public AbilityAsset ability;
        }

        public string id;
        public string displayName;
        public float baseHp = 500f;
        public float baseMoveSpeed = 3.5f;
        public List<Binding> bindings = new List<Binding>();
        public GameObject heroPrefab;
    }
}
