using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Networking.Serialization
{
    public class AutoSerializer : ISerializer
    {
        // Type ID mapping for top-level messages
        private static readonly Dictionary<Type, byte> TypeToId = new Dictionary<Type, byte>
        {
            { typeof(InputMessage), 1 },
            { typeof(StateMessage), 2 },
            { typeof(JoinRequestMessage), 3 },
            { typeof(JoinResponseMessage), 4 },
            { typeof(StartGameMessage), 5 },
            { typeof(TickPacketMessage), 6 },
            // Events are handled separately via IGameEvent interface check
        };

        private static readonly Dictionary<byte, Type> IdToType = new Dictionary<byte, Type>();

        // Cache for field accessors
        private static readonly Dictionary<Type, List<FieldInfo>> TypeFields = new Dictionary<Type, List<FieldInfo>>();

        static AutoSerializer()
        {
            foreach (var kv in TypeToId)
            {
                IdToType[kv.Value] = kv.Key;
                CacheFields(kv.Key);
            }
            
            // Also cache event types
            CacheFields(typeof(ProjectileSpawnEvent));
            CacheFields(typeof(ProjectileUpdateEvent));
            CacheFields(typeof(ProjectileDespawnEvent));
            CacheFields(typeof(DashEvent));
            CacheFields(typeof(EntityDespawnEvent));
        }

        private static void CacheFields(Type t)
        {
            if (TypeFields.ContainsKey(t)) return;
            
            // Get all public instance fields, sorted by name for determinism
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance)
                          .OrderBy(f => f.Name)
                          .ToList();
            TypeFields[t] = fields;
        }

        public byte[] Serialize(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
            {
                // 1. Write Type ID
                if (TypeToId.TryGetValue(obj.GetType(), out byte id))
                {
                    bw.Write(id);
                    WriteObject(bw, obj);
                }
                else if (obj is IGameEvent ev)
                {
                    // Special handling for top-level events if passed directly (unlikely, usually inside TickPacket)
                    // But if we do:
                    // We need a way to distinguish Event vs Message if they share IDs?
                    // Current ManualSerializer used distinct IDs for everything.
                    // Let's assume we only serialize Messages at top level.
                    throw new NotSupportedException($"Top level object {obj.GetType()} is not a registered Message.");
                }
                else
                {
                    throw new NotSupportedException($"Unknown message type: {obj.GetType()}");
                }
                
                return ms.ToArray();
            }
        }

        public object Deserialize(byte[] data)
        {
            return Deserialize(data, 0, data.Length);
        }

        public object Deserialize(byte[] data, int offset, int count)
        {
            using (var ms = new MemoryStream(data, offset, count, false))
            using (var br = new BinaryReader(ms, Encoding.UTF8))
            {
                byte id = br.ReadByte();
                if (IdToType.TryGetValue(id, out Type type))
                {
                    object instance = Activator.CreateInstance(type);
                    ReadObject(br, instance);
                    return instance;
                }
                throw new NotSupportedException($"Unknown message ID: {id}");
            }
        }

        private void WriteObject(BinaryWriter bw, object obj)
        {
            var type = obj.GetType();
            if (!TypeFields.TryGetValue(type, out var fields))
            {
                CacheFields(type);
                fields = TypeFields[type];
            }

            foreach (var field in fields)
            {
                object val = field.GetValue(obj);
                WriteValue(bw, val, field.FieldType);
            }
        }

        private void ReadObject(BinaryReader br, object obj)
        {
            var type = obj.GetType();
            if (!TypeFields.TryGetValue(type, out var fields))
            {
                CacheFields(type);
                fields = TypeFields[type];
            }

            foreach (var field in fields)
            {
                object val = ReadValue(br, field.FieldType);
                field.SetValue(obj, val);
            }
        }

        private void WriteValue(BinaryWriter bw, object val, Type type)
        {
            if (type == typeof(int)) bw.Write((int)val);
            else if (type == typeof(float)) bw.Write((float)val);
            else if (type == typeof(bool)) bw.Write((bool)val);
            else if (type == typeof(string))
            {
                string s = (string)val;
                bool has = !string.IsNullOrEmpty(s);
                bw.Write(has);
                if (has) bw.Write(s);
            }
            else if (type.IsEnum) bw.Write((int)val);
            else if (type.IsArray)
            {
                var arr = (Array)val;
                if (arr == null)
                {
                    bw.Write(0);
                }
                else
                {
                    bw.Write(arr.Length);
                    var elemType = type.GetElementType();
                    for (int i = 0; i < arr.Length; i++)
                    {
                        WriteValue(bw, arr.GetValue(i), elemType);
                    }
                }
            }
            else if (typeof(IGameEvent).IsAssignableFrom(type))
            {
                // Polymorphic Event
                var ev = (IGameEvent)val;
                // Write Event Type ID (byte)
                bw.Write((byte)ev.Type); 
                // Write Payload
                WriteObject(bw, ev);
            }
            else if (type.IsClass)
            {
                // Nested object (like StateMessage inside TickPacket array)
                if (val == null) throw new Exception("Null nested objects not supported unless array");
                WriteObject(bw, val);
            }
            else
            {
                throw new NotSupportedException($"Type {type} not supported");
            }
        }

        private object ReadValue(BinaryReader br, Type type)
        {
            if (type == typeof(int)) return br.ReadInt32();
            if (type == typeof(float)) return br.ReadSingle();
            if (type == typeof(bool)) return br.ReadBoolean();
            if (type == typeof(string))
            {
                bool has = br.ReadBoolean();
                return has ? br.ReadString() : null;
            }
            if (type.IsEnum) return Enum.ToObject(type, br.ReadInt32());
            if (type.IsArray)
            {
                int len = br.ReadInt32();
                var elemType = type.GetElementType();
                var arr = Array.CreateInstance(elemType, len);
                
                // Special case for IGameEvent array (polymorphic elements)
                bool isEventArray = elemType == typeof(IGameEvent);

                for (int i = 0; i < len; i++)
                {
                    if (isEventArray)
                    {
                        // Read Type ID first
                        byte evtTypeId = br.ReadByte();
                        Type concreteType = GetEventType(evtTypeId);
                        object instance = Activator.CreateInstance(concreteType);
                        ReadObject(br, instance);
                        arr.SetValue(instance, i);
                    }
                    else
                    {
                        object val = ReadValue(br, elemType);
                        arr.SetValue(val, i);
                    }
                }
                return arr;
            }
            if (typeof(IGameEvent).IsAssignableFrom(type) && !type.IsInterface)
            {
                // Concrete event type (shouldn't happen directly if we use interface logic above, but for safety)
                object instance = Activator.CreateInstance(type);
                ReadObject(br, instance);
                return instance;
            }
            if (type.IsClass)
            {
                object instance = Activator.CreateInstance(type);
                ReadObject(br, instance);
                return instance;
            }

            throw new NotSupportedException($"Type {type} not supported");
        }

        private Type GetEventType(byte typeId)
        {
            // Map GameEventType (byte) to Class
            // We can cast byte to enum
            var et = (GameEventType)typeId;
            switch (et)
            {
                case GameEventType.ProjectileSpawn: return typeof(ProjectileSpawnEvent);
                case GameEventType.ProjectileUpdate: return typeof(ProjectileUpdateEvent);
                case GameEventType.ProjectileDespawn: return typeof(ProjectileDespawnEvent);
                case GameEventType.Dash: return typeof(DashEvent);
                case GameEventType.EntityDespawn: return typeof(EntityDespawnEvent);
                default: throw new Exception($"Unknown event type id: {typeId}");
            }
        }
    }
}
