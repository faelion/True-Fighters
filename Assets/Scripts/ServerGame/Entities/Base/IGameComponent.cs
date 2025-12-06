using System.IO;

namespace ServerGame.Entities
{
    public enum ComponentType
    {
        None = 0,
        Transform = 1,
        Movement = 2, // Player/Generic movement with input
        Health = 3,
        Combat = 4,
        Team = 5,
        AIBehavior = 6, // Was NpcComponent
        Collision = 7,  // New
        ProjectileMovement = 8, // New
        // ImpactEffect = 9
    }

    public interface IGameComponent
    {
        ComponentType Type { get; }
        // We will add Serialization methods here in the Networking phase or now if convenient.
        // Let's add them now to prepare.
        void Serialize(BinaryWriter writer);
        void Deserialize(BinaryReader reader);
    }
}
