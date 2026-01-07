using System.IO;

namespace ServerGame.Entities
{
    public enum ComponentType
    {
        None = 0,
        Transform = 1,
        Movement = 2,
        Health = 3,
        Combat = 4,
        Team = 5,
        AIBehavior = 6,
        Collision = 7,
        Lifetime = 8,
        StatusEffect = 9,
        Casting = 10,
        Cooldown = 11
    }

    public interface IGameComponent
    {
        ComponentType Type { get; }
        void Serialize(BinaryWriter writer);
        void Deserialize(BinaryReader reader);
    }
}
