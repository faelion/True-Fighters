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
        private const byte TYPE_AbilityEventMessage = 5;
        private const byte TYPE_TickPacketMessage = 6;

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
                else if (obj is StateMessage sm)
                {
                    bw.Write(TYPE_StateMessage);
                    WriteStateMessage(bw, sm);
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
                else if (obj is AbilityEventMessage aem)
                {
                    bw.Write(TYPE_AbilityEventMessage);
                    WriteAbilityEvent(bw, aem);
                }
                else if (obj is TickPacketMessage tpm)
                {
                    bw.Write(TYPE_TickPacketMessage);
                    WriteTickPacket(bw, tpm);
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
                    case TYPE_StateMessage: return ReadStateMessage(br);
                    case TYPE_JoinRequestMessage: return ReadJoinRequest(br);
                    case TYPE_JoinResponseMessage: return ReadJoinResponse(br);
                    case TYPE_AbilityEventMessage: return ReadAbilityEvent(br);
                    case TYPE_TickPacketMessage: return ReadTickPacket(br);
                    default:
                        throw new NotSupportedException("Unknown message type id: " + type);
                }
            }
        }

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

        private static void WriteInputMessage(BinaryWriter bw, InputMessage m)
        {
            bw.Write(m.playerId);
            bw.Write((int)m.kind);
            bw.Write(m.targetX);
            bw.Write(m.targetY);
        }
        private static InputMessage ReadInputMessage(BinaryReader br)
        {
            return new InputMessage
            {
                playerId = br.ReadInt32(),
                kind = (InputKind)br.ReadInt32(),
                targetX = br.ReadSingle(),
                targetY = br.ReadSingle(),
            };
        }

        private static void WriteStateMessage(BinaryWriter bw, StateMessage m)
        {
            bw.Write(m.playerId);
            bw.Write(m.hit);
            bw.Write(m.posX);
            bw.Write(m.posY);
            bw.Write(m.rotZ);
            bw.Write(m.tick);
        }
        private static StateMessage ReadStateMessage(BinaryReader br)
        {
            return new StateMessage
            {
                playerId = br.ReadInt32(),
                hit = br.ReadBoolean(),
                posX = br.ReadSingle(),
                posY = br.ReadSingle(),
                rotZ = br.ReadSingle(),
                tick = br.ReadInt32(),
            };
        }

        private static void WriteJoinRequest(BinaryWriter bw, JoinRequestMessage m)
        {
            WriteString(bw, m.playerName);
        }
        private static JoinRequestMessage ReadJoinRequest(BinaryReader br)
        {
            return new JoinRequestMessage
            {
                playerName = ReadString(br),
            };
        }

        private static void WriteJoinResponse(BinaryWriter bw, JoinResponseMessage m)
        {
            bw.Write(m.assignedPlayerId);
            bw.Write(m.serverTick);
        }
        private static JoinResponseMessage ReadJoinResponse(BinaryReader br)
        {
            return new JoinResponseMessage
            {
                assignedPlayerId = br.ReadInt32(),
                serverTick = br.ReadInt32(),
            };
        }

        private static void WriteAbilityEvent(BinaryWriter bw, AbilityEventMessage m)
        {
            WriteString(bw, m.abilityIdOrKey);
            bw.Write(m.casterId);
            bw.Write((int)m.eventType);
            bw.Write(m.castTime);
            bw.Write(m.serverTick);
            bw.Write(m.projectileId);
            bw.Write(m.posX);
            bw.Write(m.posY);
            bw.Write(m.dirX);
            bw.Write(m.dirY);
            bw.Write(m.speed);
            bw.Write(m.lifeMs);
            bw.Write(m.value);
        }
        private static AbilityEventMessage ReadAbilityEvent(BinaryReader br)
        {
            return new AbilityEventMessage
            {
                abilityIdOrKey = ReadString(br),
                casterId = br.ReadInt32(),
                eventType = (AbilityEventType)br.ReadInt32(),
                castTime = br.ReadSingle(),
                serverTick = br.ReadInt32(),
                projectileId = br.ReadInt32(),
                posX = br.ReadSingle(),
                posY = br.ReadSingle(),
                dirX = br.ReadSingle(),
                dirY = br.ReadSingle(),
                speed = br.ReadSingle(),
                lifeMs = br.ReadInt32(),
                value = br.ReadSingle(),
            };
        }

        private static void WriteTickPacket(BinaryWriter bw, TickPacketMessage m)
        {
            bw.Write(m.serverTick);
            int sc = m.statesCount > 0 ? m.statesCount : (m.states != null ? m.states.Length : 0);
            bw.Write(sc);
            for (int i = 0; i < sc; i++) WriteStateMessage(bw, m.states[i]);
            int ec = m.eventsCount > 0 ? m.eventsCount : (m.abilityEvents != null ? m.abilityEvents.Length : 0);
            bw.Write(ec);
            for (int i = 0; i < ec; i++) WriteAbilityEvent(bw, m.abilityEvents[i]);
        }
        private static TickPacketMessage ReadTickPacket(BinaryReader br)
        {
            var m = new TickPacketMessage();
            m.serverTick = br.ReadInt32();
            int sc = br.ReadInt32();
            m.states = new StateMessage[sc];
            for (int i = 0; i < sc; i++) m.states[i] = ReadStateMessage(br);
            int ec = br.ReadInt32();
            m.abilityEvents = new AbilityEventMessage[ec];
            for (int i = 0; i < ec; i++) m.abilityEvents[i] = ReadAbilityEvent(br);
            m.statesCount = sc;
            m.eventsCount = ec;
            return m;
        }
    }
}
