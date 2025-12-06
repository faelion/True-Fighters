using System.IO;

namespace ServerGame.Entities
{
    public class AIBehaviorComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.AIBehavior;

        public float followRange = 6f;
        public float stopRange = 2f;
        public int targetEntityId = -1;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(followRange);
            writer.Write(stopRange);
            writer.Write(targetEntityId);
        }

        public void Deserialize(BinaryReader reader)
        {
            followRange = reader.ReadSingle();
            stopRange = reader.ReadSingle();
            targetEntityId = reader.ReadInt32();
        }
    }
}
