using System.IO;
using ServerGame.AI;

namespace ServerGame.Entities
{
    public class AIControllerComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.AIBehavior; // Using same type ID as old one to replace it, or could be new val.
        // Actually, let's double check ComponentType enum in NetMessages or IGameComponent to see if we can reuse or need new.
        // For now, I'll assume we reusing logic or I should check ComponentType definition first. 
        // But since this is server-only logic (logic doesn't serialize to client usually, only resulting state), 
        // wait, AIBehaviorComponent WAS serialized in the previous code. 
        // IF we need to sync "Intent" to client, we might serialize.
        // BUT usually AI logic is server-only. Clients just see movement/attacks.
        // So this component might NOT need to be serialized to client.
        
        public AIBehavior Behavior { get; set; }

        public void Serialize(BinaryWriter writer)
        {
            // Server-only component, usually. 
            // If we need to save state for savegames, we'd serialize internal state here.
            // For now, leave empty or minimal.
        }

        public void Deserialize(BinaryReader reader)
        {
            // Re-instantiation logic would be complex here without the StrategySO reference.
            // For a multiplayer session based game, typically we don't hot-reload AI from binary stream mid-match often.
        }
    }
}
