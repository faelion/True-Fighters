using System;

namespace Networking.Serialization
{
    public sealed class BinaryFormatterSerializer : ISerializer
    {
        public byte[] Serialize(object obj)
        {
            return MsgSerializer.Serialize(obj);
        }

        public object Deserialize(byte[] data)
        {
            return MsgSerializer.Deserialize(data);
        }
    }
}

