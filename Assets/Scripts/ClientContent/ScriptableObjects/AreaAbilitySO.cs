using UnityEngine;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability/Area", fileName = "AreaAbility")]
    public class AreaAbilitySO : BaseAbilitySO
    {
        public float radius = 2f;
        public int lifeMs = 1500;

        private void OnValidate() { kind = AbilityKind.Area; }
    }
}

