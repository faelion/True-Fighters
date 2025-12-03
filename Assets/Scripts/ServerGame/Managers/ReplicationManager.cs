using System;
using System.Collections.Generic;
using System.Buffers;
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
        private readonly List<IGameEvent> globalReliableEvents = new List<IGameEvent>();
        

        private readonly List<StateMessage> stateBuffer = new List<StateMessage>(64);
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
            bool hasHero = playerHero != null;
            if (hasHero)
            {
                px = playerHero.Transform.posX;
                py = playerHero.Transform.posY;
            }

            const float InterestRadius = 20f;
            const float InterestRadiusSq = InterestRadius * InterestRadius;

            foreach (var entity in world.EntityRepo.AllEntities)
            {
                bool isOwner = entity.Id == playerId;
                
                // Spectator/Dead logic: if no hero, see everything.
                if (!hasHero)
                {
                    // Fallthrough to add state
                }
                else if (!isOwner)
                {
                    float dx = entity.Transform.posX - px;
                    float dy = entity.Transform.posY - py;
                    if (dx * dx + dy * dy > InterestRadiusSq)
                        continue;
                }
                
                var state = new StateMessage
                {
                    entityId = entity.Id,
                    hp = entity.Health.currentHp,
                    maxHp = entity.Health.maxHp,
                    posX = entity.Transform.posX,
                    posY = entity.Transform.posY,
                    rotZ = entity.Transform.rotZ,
                    teamId = entity.Team.teamId,
                    entityType = (int)entity.Type,
                    archetypeId = entity.ArchetypeId,
                    tick = currentTick
                };
                stateBuffer.Add(state);
            }

            eventBuffer.AddRange(frameEvents);

            foreach (var relEvent in client.ReliableEvents)
            {
                if (relEvent.ServerTick < currentTick)
                {
                    eventBuffer.Add(relEvent);
                }
            }

            int sc = stateBuffer.Count;
            int ec = eventBuffer.Count;
            
            var statesArr = new StateMessage[sc];
            stateBuffer.CopyTo(statesArr);
            
            var eventsArr = new IGameEvent[ec];
            eventBuffer.CopyTo(eventsArr);

            return new TickPacketMessage
            {
                serverTick = currentTick,
                states = statesArr,
                events = eventsArr,
                statesCount = sc,
                eventsCount = ec
            };
        }
    }
}
