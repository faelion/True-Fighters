using System.IO;

namespace ServerGame.Entities
{
    public class CooldownComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.Cooldown;

        // Fixed slots for network efficiency and simplicity
        // Stores {CurrentTime, MaxTime} for each slot
        public float cdQ, maxQ;
        public float cdW, maxW;
        public float cdE, maxE;
        public float cdR, maxR;

        public void SetCooldown(string key, float duration)
        {
            switch (key)
            {
                case "Q": cdQ = duration; maxQ = duration; break;
                case "W": cdW = duration; maxW = duration; break;
                case "E": cdE = duration; maxE = duration; break;
                case "R": cdR = duration; maxR = duration; break;
            }
        }

        public float GetCooldown(string key)
        {
            switch (key)
            {
                case "Q": return cdQ;
                case "W": return cdW;
                case "E": return cdE;
                case "R": return cdR;
            }
            return 0;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(cdQ); writer.Write(maxQ);
            writer.Write(cdW); writer.Write(maxW);
            writer.Write(cdE); writer.Write(maxE);
            writer.Write(cdR); writer.Write(maxR);
        }

        public void Deserialize(BinaryReader reader)
        {
            cdQ = reader.ReadSingle(); maxQ = reader.ReadSingle();
            cdW = reader.ReadSingle(); maxW = reader.ReadSingle();
            cdE = reader.ReadSingle(); maxE = reader.ReadSingle();
            cdR = reader.ReadSingle(); maxR = reader.ReadSingle();
        }
    }
}
