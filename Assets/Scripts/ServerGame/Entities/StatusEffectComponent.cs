using System.Collections.Generic;
using ServerGame.Entities;
using Shared.Effects;

namespace ServerGame.Entities
{
    // Runtime wrapper for an active effect
    [System.Serializable]
    public class ActiveEffect
    {
        public Effect SourceEffect;
        public float Duration;
        public float RemainingTime;
        public float TickTimer; // For DOTs
        public int CasterId; // Using ID for easier sync (GameEntity reference is transient)
        public bool IsNew = true; // Flag to trigger OnStart
    }

    public class StatusEffectComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.StatusEffect;

        public List<ActiveEffect> ActiveEffects = new List<ActiveEffect>();

        public void AddEffect(Effect source, float duration, GameEntity caster)
        {
            // Optional: Check if unique or stackable. For now, multiple allowed.
            ActiveEffects.Add(new ActiveEffect 
            { 
                SourceEffect = source, 
                Duration = duration, 
                RemainingTime = duration,
                CasterId = caster != null ? caster.Id : -1,
                TickTimer = 0f,
                IsNew = true
            });
        }

        public void Serialize(System.IO.BinaryWriter writer)
        {
            writer.Write(ActiveEffects.Count);
            foreach (var ae in ActiveEffects)
            {
                // Write ID
                string effectId = ae.SourceEffect != null ? ae.SourceEffect.id : "";
                writer.Write(effectId);
                
                // Write State
                writer.Write(ae.RemainingTime);
                writer.Write(ae.CasterId);
            }
        }

        public void Deserialize(System.IO.BinaryReader reader)
        {
            // Note: This Deserialize is raw data. 
            // In the Client implementation (NetEntityView), we'll read this manually 
            // because we need to map string ID back to ScriptableObject.
            // This method might be unused if we handle it in View, but good to keep structure.
            int count = reader.ReadInt32();
            // We can't reconstruct SourceEffect here easily without Registry access.
            // So we skip reading or assume this is only called if we have access.
            // For now, let's just skip the bytes to prevent stream corruption if called blindly.
            for (int i = 0; i < count; i++)
            {
                reader.ReadString(); // ID
                reader.ReadSingle(); // Time
                reader.ReadInt32();  // Caster
            }
        }
    }
}
