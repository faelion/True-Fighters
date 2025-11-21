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
            int i = 0;
            foreach (var entity in world.EntityRepo.AllEntities)
            {
                if (i >= stateObjs.Count)
                    stateObjs.Add(new StateMessage());
                var sm = stateObjs[i++];
                sm.playerId = entity.Id;
                sm.hp = entity.Health.currentHp;
                sm.maxHp = entity.Health.maxHp;
                sm.posX = entity.Transform.posX;
                sm.posY = entity.Transform.posY;
                sm.rotZ = entity.Transform.rotZ;
                sm.teamId = entity.Team.teamId;
                sm.entityType = (int)entity.Type;
                sm.archetypeId = entity.ArchetypeId ?? string.Empty;
                sm.tick = tick;
                stateBuffer.Add(sm);
            }
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
                if (!ClientContent.ContentAssetRegistry.Abilities.TryGetValue(eff.abilityId, out var asset) || asset == null)
                {
                    UnityEngine.Debug.LogWarning($"[Server] Missing ability asset for id '{eff.abilityId}' when emitting events for effect {eff.id}");
                    continue;
                }
                asset.EmitEvents(world, eff, tick, eventBuffer);
            }
        }
    }
}
