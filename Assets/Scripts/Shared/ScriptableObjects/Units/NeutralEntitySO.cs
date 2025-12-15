using UnityEngine;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Neutral Entity", fileName = "NeutralEntity")]
    public class NeutralEntitySO : UnitSO
    {
        public GameObject prefab;

        [Header("Animation Config")]
        public RuntimeAnimatorController baseControllerTemplate;
        public AnimationClip idleClip;
        public AnimationClip walkClip;
        public AnimationClip attackClip;
    }
}
