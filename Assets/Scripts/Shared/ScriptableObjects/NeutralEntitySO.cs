using UnityEngine;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Neutral Entity", fileName = "NeutralEntity")]
    public class NeutralEntitySO : ScriptableObject
    {
        public string id;
        public string displayName;
        public float baseHp = 400f;
        public float moveSpeed = 2f;
        public GameObject prefab;
    }
}
