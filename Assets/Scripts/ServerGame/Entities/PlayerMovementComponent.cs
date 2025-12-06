using System.IO;

namespace ServerGame.Entities
{
    public class PlayerMovementComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.Movement;

        public float moveSpeed = 3.5f;
        
        // This seems to be click-to-move destination
        public bool hasDestination;
        public float destX;
        public float destY;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(moveSpeed);
            writer.Write(hasDestination);
            if (hasDestination)
            {
                writer.Write(destX);
                writer.Write(destY);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            moveSpeed = reader.ReadSingle();
            hasDestination = reader.ReadBoolean();
            if (hasDestination)
            {
                destX = reader.ReadSingle();
                destY = reader.ReadSingle();
            }
        }
    }
}
