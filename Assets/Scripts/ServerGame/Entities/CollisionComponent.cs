using System.IO;

namespace ServerGame.Entities
{
    public class CollisionComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.Collision;

        public float radius = 0.5f;
        public bool isTrigger = false; 

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(radius);
            writer.Write(isTrigger);
        }

        public void Deserialize(BinaryReader reader)
        {
            radius = reader.ReadSingle();
            isTrigger = reader.ReadBoolean();
        }
    }
}
