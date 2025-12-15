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
            public string key; // "Q","W","E","R"
            public AbilityAsset ability;
            public AnimationClip castClip;
        }
        public List<Binding> bindings = new List<Binding>();
        public GameObject heroPrefab;

        [Header("Animation Config")]
        public RuntimeAnimatorController baseControllerTemplate;
        public AnimationClip idleClip;
        public AnimationClip walkClip;
    }
}
