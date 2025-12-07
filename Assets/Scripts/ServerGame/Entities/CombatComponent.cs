using System.IO;

namespace ServerGame.Entities
{
    public class CombatComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.Combat;

        public float attackDamage = 25f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1.0f;
        public float attackTimer = 0f;

        // Generic "IsActive" logic for Disables (Silences, Stuns)
        public int DisabledCount;
        public bool IsActive => DisabledCount <= 0;


        public void Serialize(BinaryWriter writer)
        {
            writer.Write(attackDamage);
            writer.Write(attackRange);
            writer.Write(attackCooldown);
            // attackTimer is transient state, not syncing for now to save bandwidth
        }

        public void Deserialize(BinaryReader reader)
        {
            attackDamage = reader.ReadSingle();
            attackRange = reader.ReadSingle();
            attackCooldown = reader.ReadSingle();
        }
    }
}
