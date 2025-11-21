namespace ServerGame.Entities
{
    public class HealthComponent
    {
        public float maxHp = 500f;
        public float currentHp = 500f;
        public bool invulnerable = false;
        public bool recentlyHit = false;
        public float hitTimer = 0f;

        public bool IsAlive => currentHp > 0f;

        public void Reset(float hp)
        {
            maxHp = hp;
            currentHp = hp;
            recentlyHit = false;
            hitTimer = 0f;
        }

        public void ApplyDamage(float value)
        {
            if (invulnerable) return;
            currentHp -= value;
            if (currentHp < 0f) currentHp = 0f;
            recentlyHit = true;
            hitTimer = 0f;
        }

        public void ApplyHeal(float value)
        {
            currentHp += value;
            if (currentHp > maxHp) currentHp = maxHp;
        }
    }
}
