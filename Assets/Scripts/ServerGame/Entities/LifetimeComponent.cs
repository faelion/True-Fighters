using System.IO;

namespace ServerGame.Entities
{
    public class LifetimeComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.Lifetime;

        public float remainingTime; // in seconds

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(remainingTime);
        }

        public void Deserialize(BinaryReader reader)
        {
            remainingTime = reader.ReadSingle();
        }
    }
}
