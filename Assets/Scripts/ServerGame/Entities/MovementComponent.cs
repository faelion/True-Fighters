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
        public float velX, velY;
        public float destX, destY;
        public bool hasDestination;
        
        // Pathfinding State
        public UnityEngine.Vector3[] pathCorners;
        public int currentCornerIdx;
        public bool pathDirty; // To trigger recalculation if needed

        // Generic "IsActive" logic for Disable effects (Stuns, Roots, etc)
        public int DisabledCount;
        public bool IsActive => DisabledCount <= 0;


        public void Serialize(BinaryWriter writer)
        {
            writer.Write(moveSpeed);
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
            moveSpeed = reader.ReadSingle();
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
