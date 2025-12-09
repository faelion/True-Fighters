using System.IO;

namespace ServerGame.Entities
{
    public class HealthComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.Health;

        public float maxHp = 500f;
        public float currentHp = 500f;
        public bool invulnerable = false;

        public bool IsAlive => currentHp > 0f;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(maxHp);
            writer.Write(currentHp);
            writer.Write(invulnerable);
        }

        public void Deserialize(BinaryReader reader)
        {
            maxHp = reader.ReadSingle();
            currentHp = reader.ReadSingle();
            invulnerable = reader.ReadBoolean();
        }

        public void Reset(float hp)
        {
            maxHp = hp;
            currentHp = hp;
        }

        public void ApplyDamage(float value)
        {
            if (invulnerable) return;
            currentHp -= value;
            if (currentHp < 0f) currentHp = 0f;
        }

        public void ApplyHeal(float value)
        {
            currentHp += value;
            if (currentHp > maxHp) currentHp = maxHp;
        }
    }
}
