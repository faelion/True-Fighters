using System;
using System.Collections.Generic;
using System.IO;
using ServerGame.Entities;

namespace ServerGame.Managers
{
    public class ReplicationManager
    {
        private class ClientState
        {
            public int LastAckedTick = -1;
            public List<IGameEvent> ReliableEvents = new List<IGameEvent>();
        }

        private readonly Dictionary<int, ClientState> clients = new Dictionary<int, ClientState>();
        
        private readonly List<EntityStateData> stateBuffer = new List<EntityStateData>(64);
        private readonly List<IGameEvent> eventBuffer = new List<IGameEvent>(64);

        public void RegisterClient(int playerId)
        {
            if (!clients.ContainsKey(playerId))
                clients[playerId] = new ClientState();
        }

        public void UnregisterClient(int playerId)
        {
            clients.Remove(playerId);
        }

        public void ProcessAck(int playerId, int ackTick)
        {
            if (clients.TryGetValue(playerId, out var client))
            {
                if (ackTick > client.LastAckedTick)
                    client.LastAckedTick = ackTick;

                for (int i = client.ReliableEvents.Count - 1; i >= 0; i--)
                {
                    if (client.ReliableEvents[i].ServerTick <= client.LastAckedTick)
                    {
                        client.ReliableEvents.RemoveAt(i);
                    }
                }
            }
        }

        public void EnqueueReliableEvent(IGameEvent ev)
        {
            foreach (var client in clients.Values)
            {
                client.ReliableEvents.Add(ev);
            }
        }

        public TickPacketMessage BuildPacket(int playerId, ServerWorld world, int currentTick, List<IGameEvent> frameEvents)
        {
            if (!clients.TryGetValue(playerId, out var client)) return null;

            stateBuffer.Clear();
            eventBuffer.Clear();

            var playerHero = world.GetHeroEntity(playerId);
            float px = 0f, py = 0f;
            bool hasHero = playerHero != null && playerHero.TryGetComponent(out TransformComponent heroTransform);
            
            if (hasHero)
            {
                // playerHero.GetComponent would work too since we checked hasHero
                var t = playerHero.GetComponent<TransformComponent>();
                px = t.posX;
                py = t.posY;
            }

            const float InterestRadius = 20f;
            const float InterestRadiusSq = InterestRadius * InterestRadius;

            foreach (var entity in world.EntityRepo.AllEntities)
            {
                bool isOwner = entity.Id == playerId;
                
                // Interest Management
                if (hasHero && !isOwner)
                {
                    // If entity has transform, check distance
                    if (entity.TryGetComponent(out TransformComponent t))
                    {
                        // Logic from GameMath inline or usage if available.
                        // Ideally reused GameMath but I deleted it per user request.
                        // Manual calc:
                        float dx = t.posX - px;
                        float dy = t.posY - py;
                        if (dx * dx + dy * dy > InterestRadiusSq)
                            continue;
                    }
                }
                
                var entityState = new EntityStateData();
                entityState.entityId = entity.Id;
                entityState.entityType = (int)entity.Type;
                entityState.archetypeId = entity.ArchetypeId;
                entityState.tick = currentTick;

                var compList = new List<ComponentData>();
                foreach (var comp in entity.AllComponents)
                {
                    using (var ms = new MemoryStream())
                    using (var writer = new BinaryWriter(ms))
                    {
                        comp.Serialize(writer);
                        compList.Add(new ComponentData
                        {
                            type = (int)comp.Type,
                            data = ms.ToArray()
                        });
                    }
                }
                entityState.components = compList.ToArray();
                stateBuffer.Add(entityState);
            }

            eventBuffer.AddRange(frameEvents);

            foreach (var relEvent in client.ReliableEvents)
            {
                if (relEvent.ServerTick < currentTick)
                {
                    eventBuffer.Add(relEvent);
                }
            }

            return new TickPacketMessage
            {
                serverTick = currentTick,
                states = stateBuffer.ToArray(),
                events = eventBuffer.ToArray(),
                statesCount = stateBuffer.Count,
                eventsCount = eventBuffer.Count
            };
        }
    }
}
