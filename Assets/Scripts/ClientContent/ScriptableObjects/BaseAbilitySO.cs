using UnityEngine;

namespace ClientContent
{
    public abstract class BaseAbilitySO : ScriptableObject
    {
        public string id;
        public string displayName;
        public AbilityKind kind;
        public float range = 12f;
        public float castTime = 0f;
        public float cooldown = 2f;
        public AbilityTargeting targeting = AbilityTargeting.Point;
        public string defaultKey = ""; // optional editor helper
    }
}

