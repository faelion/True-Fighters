namespace ServerGame.Content
{
    public enum AbilityTargeting
    {
        Point,
        Direction,
        Unit
    }

    public enum AbilityKind
    {
        Projectile,
        Area,
        Dash,
        Heal,
        Buff
    }

    // Data-driven ability definition for server logic
    public class AbilityDef
    {
        public string id;
        public string key; // optional default binding (Q/W/E/R), server may ignore
        public AbilityKind kind = AbilityKind.Projectile;

        public float range = 12f;
        public float castTime = 0f;
        public float cooldown = 2f;
        public AbilityTargeting targeting = AbilityTargeting.Point;

        // Projectile params
        public float projectileSpeed = 8f;
        public int projectileLifeMs = 1500;
        public float projectileDamage = 0f;

        // Area params
        public float areaRadius = 2f;
        public int areaLifeMs = 1500;

        // Dash params
        public float dashDistance = 4f;
        public float dashSpeed = 10f;
        public float dashDamage = 0f;

        // Heal params
        public float healAmount = 50f;
    }
}
