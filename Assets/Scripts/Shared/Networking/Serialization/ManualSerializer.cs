using System;
using System.IO;
using System.Text;

namespace Networking.Serialization
{
    // High-performance manual serializer for our network DTOs.
    // Format: [typeId:byte][payload...] where payload is field-wise binary.
    public sealed class ManualSerializer : ISerializer
    {
        private const byte TYPE_InputMessage = 1;
        private const byte TYPE_StateMessage = 2;
        private const byte TYPE_JoinRequestMessage = 3;
        private const byte TYPE_JoinResponseMessage = 4;
        private const byte TYPE_StartGameMessage = 5;
        private const byte TYPE_TickPacketMessage = 6;
        
        private const byte TYPE_LobbyUpdateMessage = 7;
        private const byte TYPE_LobbyActionMessage = 8;
        
        // Event payloads
        private const byte TYPE_EntityDespawnEvent = 15;
        private const byte TYPE_EntitySpawnEvent = 16;

        public byte[] Serialize(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                if (obj is InputMessage im)
                {
                    bw.Write(TYPE_InputMessage);
                    WriteInputMessage(bw, im);
                }
                else if (obj is EntityStateData sm)
                {
                    bw.Write(TYPE_StateMessage); // reusing op code
                    WriteEntityStateData(bw, sm);
                }
                else if (obj is JoinRequestMessage jr)
                {
                    bw.Write(TYPE_JoinRequestMessage);
                    WriteJoinRequest(bw, jr);
                }
                else if (obj is JoinResponseMessage jp)
                {
                    bw.Write(TYPE_JoinResponseMessage);
                    WriteJoinResponse(bw, jp);
                }
                else if (obj is StartGameMessage sgm)
                {
                    bw.Write(TYPE_StartGameMessage);
                    WriteStartGame(bw, sgm);
                }
                else if (obj is IGameEvent ge)
                {
                    bw.Write(GetEventTypeId(ge));
                    WriteEventPayload(bw, ge);
                }
                else if (obj is TickPacketMessage tpm)
                {
                    bw.Write(TYPE_TickPacketMessage);
                    WriteTickPacket(bw, tpm);
                }
                else if (obj is LobbyUpdateMessage lum)
                {
                    bw.Write(TYPE_LobbyUpdateMessage);
                    WriteLobbyUpdate(bw, lum);
                }
                else if (obj is LobbyActionMessage lam)
                {
                    bw.Write(TYPE_LobbyActionMessage);
                    WriteLobbyAction(bw, lam);
                }
                else
                {
                    throw new NotSupportedException("Unknown message type: " + obj.GetType().FullName);
                }
                bw.Flush();
                return ms.ToArray();
            }
        }

        public object Deserialize(byte[] data)
        {
            if (data == null || data.Length == 0) throw new ArgumentException("empty data", nameof(data));
            return Deserialize(data, 0, data.Length);
        }

        public object Deserialize(byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (count <= 0) throw new ArgumentException("empty segment", nameof(count));
            using (var ms = new MemoryStream(data, offset, count, false))
            using (var br = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true))
            {
                byte type = br.ReadByte();
                switch (type)
                {
                    case TYPE_InputMessage: return ReadInputMessage(br);
                    case TYPE_StateMessage: return ReadEntityStateData(br);
                    case TYPE_JoinRequestMessage: return ReadJoinRequest(br);
                    case TYPE_JoinResponseMessage: return ReadJoinResponse(br);
                    case TYPE_StartGameMessage: return ReadStartGame(br);
                    case TYPE_TickPacketMessage: return ReadTickPacket(br);
                    case TYPE_LobbyUpdateMessage: return ReadLobbyUpdate(br);
                    case TYPE_LobbyActionMessage: return ReadLobbyAction(br);
                    case TYPE_EntityDespawnEvent: return ReadEntityDespawnEvent(br);
                    case TYPE_EntitySpawnEvent: return ReadEntitySpawnEvent(br);
                    default:
                        throw new NotSupportedException("Unknown message type id: " + type);
                }
            }
        }
        
        // ... (Existing Writers/Readers unchanged) ...

        private static void WriteLobbyUpdate(BinaryWriter bw, LobbyUpdateMessage m)
        {
            m.data.Serialize(bw);
        }
        private static LobbyUpdateMessage ReadLobbyUpdate(BinaryReader br)
        {
            var m = new LobbyUpdateMessage();
            m.data = new Shared.LobbyStateData();
            m.data.Deserialize(br);
            return m;
        }

        private static void WriteLobbyAction(BinaryWriter bw, LobbyActionMessage m)
        {
            bw.Write(m.actionType);
            WriteString(bw, m.payload);
        }
        private static LobbyActionMessage ReadLobbyAction(BinaryReader br)
        {
            return new LobbyActionMessage
            {
                actionType = br.ReadInt32(),
                payload = ReadString(br)
            };
        }
        
        // Static Helpers for Strings/etc
        private static void WriteString(BinaryWriter bw, string s)
        {
            bool has = !string.IsNullOrEmpty(s);
            bw.Write(has);
            if (has) bw.Write(s);
        }
        private static string ReadString(BinaryReader br)
        {
            bool has = br.ReadBoolean();
            return has ? br.ReadString() : string.Empty;
        }

        // ... (Rest of existing methods)

        private static void WriteInputMessage(BinaryWriter bw, InputMessage m)
        {
            bw.Write(m.playerId);
            bw.Write((int)m.kind);
            bw.Write(m.targetX);
            bw.Write(m.targetY);
            bw.Write(m.lastReceivedTick);
        }
        private static InputMessage ReadInputMessage(BinaryReader br)
        {
            return new InputMessage
            {
                playerId = br.ReadInt32(),
                kind = (InputKind)br.ReadInt32(),
                targetX = br.ReadSingle(),
                targetY = br.ReadSingle(),
                lastReceivedTick = br.ReadInt32(),
            };
        }

        private static void WriteEntityStateData(BinaryWriter bw, EntityStateData m)
        {
            bw.Write(m.entityId);
            bw.Write(m.entityType);
            WriteString(bw, m.archetypeId);
            bw.Write(m.tick);
            int count = m.components != null ? m.components.Length : 0;
            bw.Write(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var c = m.components[i];
                    bw.Write(c.type);
                    int len = c.data != null ? c.data.Length : 0;
                    bw.Write(len);
                    if (len > 0) bw.Write(c.data);
                }
            }
        }
        private static EntityStateData ReadEntityStateData(BinaryReader br)
        {
            var m = new EntityStateData();
            m.entityId = br.ReadInt32();
            m.entityType = br.ReadInt32();
            m.archetypeId = ReadString(br);
            m.tick = br.ReadInt32();
            int count = br.ReadInt32();
            if (count > 0)
            {
                m.components = new ComponentData[count];
                for (int i = 0; i < count; i++)
                {
                    var c = new ComponentData();
                    c.type = br.ReadInt32();
                    int len = br.ReadInt32();
                    if (len > 0) c.data = br.ReadBytes(len);
                    m.components[i] = c;
                }
            }
            return m;
        }

        private static void WriteJoinRequest(BinaryWriter bw, JoinRequestMessage m)
        {
            WriteString(bw, m.playerName);
            WriteString(bw, m.heroId);
        }
        private static JoinRequestMessage ReadJoinRequest(BinaryReader br)
        {
            return new JoinRequestMessage
            {
                playerName = ReadString(br),
                heroId = ReadString(br),
            };
        }

        private static void WriteJoinResponse(BinaryWriter bw, JoinResponseMessage m)
        {
            bw.Write(m.assignedPlayerId);
            bw.Write(m.serverTick);
            WriteString(bw, m.heroId);
        }
        private static JoinResponseMessage ReadJoinResponse(BinaryReader br)
        {
            return new JoinResponseMessage
            {
                assignedPlayerId = br.ReadInt32(),
                serverTick = br.ReadInt32(),
                heroId = ReadString(br),
            };
        }

        private static byte GetEventTypeId(IGameEvent ev)
        {
            switch (ev.Type)
            {
                case GameEventType.EntityDespawn: return TYPE_EntityDespawnEvent;
                case GameEventType.EntitySpawn: return TYPE_EntitySpawnEvent;
                default: throw new NotSupportedException("Unknown event type: " + ev.Type);
            }
        }

        private static void WriteEvent(BinaryWriter bw, IGameEvent ev)
        {
            bw.Write(GetEventTypeId(ev));
            WriteEventPayload(bw, ev);
        }

        private static void WriteEventPayload(BinaryWriter bw, IGameEvent ev)
        {
            switch (ev.Type)
            {
                case GameEventType.EntityDespawn:
                {
                    var m = (EntityDespawnEvent)ev;
                    bw.Write(m.CasterId);
                    bw.Write(m.ServerTick);
                    bw.Write(m.EventId);
                    break;
                }
                case GameEventType.EntitySpawn:
                {
                    var m = (EntitySpawnEvent)ev;
                    bw.Write(m.CasterId);
                    bw.Write(m.ServerTick);
                    bw.Write(m.PosX);
                    bw.Write(m.PosY);
                    WriteString(bw, m.ArchetypeId);
                    bw.Write(m.TeamId);
                    bw.Write(m.EventId);
                    break;
                }
                default:
                    throw new NotSupportedException("Unknown event type: " + ev.Type);
            }
        }

        private static IGameEvent ReadEvent(BinaryReader br, byte type)
        {
            switch (type)
            {
                case TYPE_EntityDespawnEvent: return ReadEntityDespawnEvent(br);
                case TYPE_EntitySpawnEvent: return ReadEntitySpawnEvent(br);
                default:
                    throw new NotSupportedException("Unknown event type id: " + type);
            }
        }

        private static EntityDespawnEvent ReadEntityDespawnEvent(BinaryReader br)
        {
            return new EntityDespawnEvent
            {
                CasterId = br.ReadInt32(),
                ServerTick = br.ReadInt32(),

                EventId = br.ReadInt32(),
            };
        }

        private static EntitySpawnEvent ReadEntitySpawnEvent(BinaryReader br)
        {
            return new EntitySpawnEvent
            {
                CasterId = br.ReadInt32(),
                ServerTick = br.ReadInt32(),
                PosX = br.ReadSingle(),
                PosY = br.ReadSingle(),
                ArchetypeId = ReadString(br),
                TeamId = br.ReadInt32(),
                EventId = br.ReadInt32(),
            };
        }

        private static void WriteTickPacket(BinaryWriter bw, TickPacketMessage m)
        {
            bw.Write(m.serverTick);
            int sc = m.statesCount > 0 ? m.statesCount : (m.states != null ? m.states.Length : 0);
            bw.Write(sc);
            for (int i = 0; i < sc; i++) WriteEntityStateData(bw, m.states[i]);
            int ec = m.eventsCount > 0 ? m.eventsCount : (m.events != null ? m.events.Length : 0);
            bw.Write(ec);
            for (int i = 0; i < ec; i++) WriteEvent(bw, m.events[i]);
        }
        private static TickPacketMessage ReadTickPacket(BinaryReader br)
        {
            var m = new TickPacketMessage();
            m.serverTick = br.ReadInt32();
            int sc = br.ReadInt32();
            m.states = new EntityStateData[sc];
            for (int i = 0; i < sc; i++) m.states[i] = ReadEntityStateData(br);
            int ec = br.ReadInt32();
            m.events = new IGameEvent[ec];
            for (int i = 0; i < ec; i++)
            {
                byte evtType = br.ReadByte();
                m.events[i] = ReadEvent(br, evtType);
            }
            m.statesCount = sc;
            m.eventsCount = ec;
            return m;
        }

        private static void WriteStartGame(BinaryWriter bw, StartGameMessage m)
        {
            WriteString(bw, m.sceneName);
        }
        private static StartGameMessage ReadStartGame(BinaryReader br)
        {
            return new StartGameMessage
            {
                sceneName = ReadString(br),
            };
        }
    }
}
