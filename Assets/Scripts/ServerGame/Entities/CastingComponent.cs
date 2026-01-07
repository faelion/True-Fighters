using System.IO;

namespace ServerGame.Entities
{
    public class CastingComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.Casting;

        public bool IsCasting;
        public string AbilityId;
        public string Key;
        public float Timer;
        public float TotalTime;
        
        // For casting target (if needed later)
        public float TargetX;
        public float TargetY;

        public float Progress => TotalTime > 0 ? (1f - (Timer / TotalTime)) : 0f;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(IsCasting);
            if (IsCasting)
            {
                writer.Write(AbilityId ?? "");
                writer.Write(Timer);
                writer.Write(TotalTime);
                writer.Write(TargetX);
                writer.Write(TargetY);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            IsCasting = reader.ReadBoolean();
            if (IsCasting)
            {
                AbilityId = reader.ReadString();
                Timer = reader.ReadSingle();
                TotalTime = reader.ReadSingle();
                TargetX = reader.ReadSingle();
                TargetY = reader.ReadSingle();
            }
            else
            {
                // Reset defaults just in case
                AbilityId = "";
                Timer = 0f;
                TotalTime = 0f;
            }
        }
    }
}
