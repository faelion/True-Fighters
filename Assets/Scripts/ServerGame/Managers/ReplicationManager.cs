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
            // Events that must be reliably delivered until acked
            public List<IGameEvent> ReliableEvents = new List<IGameEvent>();
        }

        private readonly Dictionary<int, ClientState> clients = new Dictionary<int, ClientState>();
        private readonly List<IGameEvent> globalReliableEvents = new List<IGameEvent>();
        
        // Temporary buffers for snapshot building
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
                // Monotonic increase only
                if (ackTick > client.LastAckedTick)
                    client.LastAckedTick = ackTick;

                // Prune acknowledged reliable events
                // If the event occurred at tick T, and client acked T (or >T), they have it.
                // NOTE: This assumes events are sent with the tick they occurred.
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
            // Add to all currently connected clients
            foreach (var client in clients.Values)
            {
                client.ReliableEvents.Add(ev);
            }
        }

        // Called every tick to build packets for all clients
        public TickPacketMessage BuildPacket(int playerId, ServerWorld world, int currentTick, List<IGameEvent> frameEvents)
        {
            if (!clients.TryGetValue(playerId, out var client)) return null;

            stateBuffer.Clear();
            eventBuffer.Clear();

            // 1. Collect World State with Interest Management
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
                // Always send the player's own entity
                bool isOwner = entity.Id == playerId;
                
                if (!isOwner && hasHero)
                {
                    float dx = entity.Transform.posX - px;
                    float dy = entity.Transform.posY - py;
                    if (dx * dx + dy * dy > InterestRadiusSq)
                        continue; // Too far
                }
                // If we don't have a hero yet (dead or not spawned), maybe we should see everything? 
                // Or nothing? Let's default to seeing everything if we are dead/spectating for now, 
                // or maybe just 0,0. Let's assume if no hero, we see everything (spectator mode fallback).
                
                var state = new StateMessage
                {
                    playerId = entity.Id, // Using playerId field for EntityId as per NetMessages
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

            // 2. Add Frame Events (Unreliable/One-shot for this tick)
            // These are events that happened THIS frame.
            eventBuffer.AddRange(frameEvents);

            // 3. Add Reliable Events (Retries)
            // Add any reliable event that hasn't been acked yet.
            // We filter out events that are already in frameEvents to avoid duplicates in the same frame (optional, but good practice)
            foreach (var relEvent in client.ReliableEvents)
            {
                // If it's old, we resend it. If it's from this frame, it's already in frameEvents? 
                // Actually, let's assume frameEvents are passed in. We should add reliable events that are NOT in frameEvents (older ones).
                if (relEvent.ServerTick < currentTick)
                {
                    eventBuffer.Add(relEvent);
                }
            }

            // 4. Construct Packet
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
