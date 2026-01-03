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
        
        // Custom Data
        public UnityEngine.Vector3 TargetPos;
        public bool HasTarget;
    }

    public class StatusEffectComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.StatusEffect;

        public List<ActiveEffect> ActiveEffects = new List<ActiveEffect>();

        public void AddEffect(Effect source, float duration, GameEntity caster, UnityEngine.Vector3? targetPos = null)
        {
            // Optional: Check if unique or stackable. For now, multiple allowed.
            ActiveEffects.Add(new ActiveEffect 
            { 
                SourceEffect = source, 
                Duration = duration, 
                RemainingTime = duration,
                CasterId = caster != null ? caster.Id : -1,
                TickTimer = 0f,
                IsNew = true,
                HasTarget = targetPos.HasValue,
                TargetPos = targetPos.GetValueOrDefault()
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
            // Note: Client implementation (NetEntityView) reads this manually to map string ID.
            // We skip bytes here to prevent stream corruption if called blindly.
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                reader.ReadString(); // ID
                reader.ReadSingle(); // Time
                reader.ReadInt32();  // Caster
            }
        }
    }
}
