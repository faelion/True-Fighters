using UnityEngine;

namespace ClientContent
{
    [CreateAssetMenu(menuName = "Content/Ability/Projectile", fileName = "ProjectileAbility")]
    public class ProjectileAbilitySO : BaseAbilitySO
    {
        public float projectileSpeed = 8f;
        public int projectileLifeMs = 1500;
        public float damage = 0f;

        private void OnValidate() { kind = AbilityKind.Projectile; }
    }
}

