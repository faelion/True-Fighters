using UnityEngine;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability/Dash", fileName = "DashAbility")]
    public class DashAbilitySO : BaseAbilitySO
    {
        public float distance = 4f;
        public float speed = 10f;
        public float damageOnHit = 0f;

        private void OnValidate() { kind = AbilityKind.Dash; }
    }
}

