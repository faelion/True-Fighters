using UnityEngine;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability/Heal", fileName = "HealAbility")]
    public class HealAbilitySO : BaseAbilitySO
    {
        public float amount = 50f;

        private void OnValidate() { kind = AbilityKind.Heal; }
    }
}

