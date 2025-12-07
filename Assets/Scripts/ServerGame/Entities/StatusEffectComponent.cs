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
        public GameEntity Caster; // Who cast it?
    }

    public class StatusEffectComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.StatusEffect;

        public List<ActiveEffect> ActiveEffects = new List<ActiveEffect>();

        public void AddEffect(Effect source, float duration, GameEntity caster)
        {
            ActiveEffects.Add(new ActiveEffect 
            { 
                SourceEffect = source, 
                Duration = duration, 
                RemainingTime = duration,
                Caster = caster,
                TickTimer = 0f
            });
        }

        public void Serialize(System.IO.BinaryWriter writer)
        {
            // For now, minimal sync. 
            // Ideally we sync Effect IDs, but with Polymorphic Inline effects, they don't have IDs!
            // This is the tricky part of Option 3. How to sync to client?
            // Client NEEDS to know "I am stunned".
            // We can add a "Tag" or "VisualId" string to the Effect base class.
            writer.Write(ActiveEffects.Count);
            // We'll figure out full sync later. For now just sync count to avoid crashing.
        }

        public void Deserialize(System.IO.BinaryReader reader)
        {
            int count = reader.ReadInt32();
            // Stub
        }
    }
}
