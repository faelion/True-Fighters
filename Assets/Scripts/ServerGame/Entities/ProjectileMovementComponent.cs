using System.IO;

namespace ServerGame.Entities
{
    public class ProjectileMovementComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.ProjectileMovement;

        public float speed;
        public float dirX;
        public float dirY;
        public int lifeMs; // Lifetime in milliseconds

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(speed);
            writer.Write(dirX);
            writer.Write(dirY);
            writer.Write(lifeMs);
        }

        public void Deserialize(BinaryReader reader)
        {
            speed = reader.ReadSingle();
            dirX = reader.ReadSingle();
            dirY = reader.ReadSingle();
            lifeMs = reader.ReadInt32();
        }
    }
}
