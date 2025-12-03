using UnityEngine;

namespace ClientContent
{
    public abstract class UnitSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public float baseHp = 500f;
        public float moveSpeed = 3.5f;
    }
}
