using System.IO;
using Shared.ScriptableObjects;

namespace ServerGame.Entities
{
    public class MovementComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.Movement;

        // Configuration (can be set from SO)
        public MovementStrategySO strategy;
        public float moveSpeed;

        // State
        public float velX;
        public float velY;
        public float destX;
        public float destY;
        public bool hasDestination;

        public void Serialize(BinaryWriter writer)
        {
            // Sync velocity for client prediction/interpolation? 
            // Or just pos (via Transform) + dest?
            // Usually syncing velocity is good for linear extrapolation.
            writer.Write(velX);
            writer.Write(velY);
            writer.Write(hasDestination);
            if (hasDestination)
            {
                writer.Write(destX);
                writer.Write(destY);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            velX = reader.ReadSingle();
            velY = reader.ReadSingle();
            hasDestination = reader.ReadBoolean();
            if (hasDestination)
            {
                destX = reader.ReadSingle();
                destY = reader.ReadSingle();
            }
        }
    }
}
