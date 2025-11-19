using System.Collections.Generic;

namespace ServerGame
{
    // Builds state and event payloads for replication from the current world snapshot.
    public class ServerSnapshotBuilder
    {
        private readonly List<StateMessage> stateObjs = new List<StateMessage>(32);

        public void BuildSnapshot(ServerWorld world, int tick, IList<StateMessage> stateBuffer, IList<IGameEvent> eventBuffer)
        {
            BuildStates(world, tick, stateBuffer);
            BuildEvents(world, tick, eventBuffer);
        }

        private void BuildStates(ServerWorld world, int tick, IList<StateMessage> stateBuffer)
        {
            stateBuffer.Clear();
            int countNeeded = world.Players.Count + 1; // players + npc
            while (stateObjs.Count < countNeeded) stateObjs.Add(new StateMessage());

            int i = 0;
            foreach (var p in world.Players.Values)
            {
                var sm = stateObjs[i++];
                sm.playerId = p.playerId;
                sm.hit = p.hit;
                sm.posX = p.posX;
                sm.posY = p.posY;
                sm.rotZ = p.rotZ;
                sm.tick = tick;
                stateBuffer.Add(sm);
            }
            var npc = stateObjs[i++];
            npc.playerId = world.Npc.id;
            npc.hit = false;
            npc.posX = world.Npc.posX;
            npc.posY = world.Npc.posY;
            npc.rotZ = 0f;
            npc.tick = tick;
            stateBuffer.Add(npc);
        }

        private void BuildEvents(ServerWorld world, int tick, IList<IGameEvent> eventBuffer)
        {
            eventBuffer.Clear();

            var instant = world.ConsumePendingEvents();
            if (instant != null)
            {
                foreach (var e in instant)
                {
                    if (e == null) continue;
                    e.ServerTick = tick;
                    eventBuffer.Add(e);
                }
            }

            foreach (var eff in world.AbilityEffects.Values)
            {
                if (!ClientContent.AbilityAssetRegistry.Abilities.TryGetValue(eff.abilityId, out var asset) || asset == null)
                {
                    UnityEngine.Debug.LogWarning($"[Server] Missing ability asset for id '{eff.abilityId}' when emitting events for effect {eff.id}");
                    continue;
                }
                asset.EmitEvents(world, eff, tick, eventBuffer);
            }
        }
    }
}
