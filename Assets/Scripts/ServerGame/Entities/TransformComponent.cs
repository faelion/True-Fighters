using System.IO;

namespace ServerGame.Entities
{
    public class TransformComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.Transform;

        public float posX;
        public float posY;
        public float rotZ;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(posX);
            writer.Write(posY);
            writer.Write(rotZ);
        }

        public void Deserialize(BinaryReader reader)
        {
            posX = reader.ReadSingle();
            posY = reader.ReadSingle();
            rotZ = reader.ReadSingle();
        }
    }
}
