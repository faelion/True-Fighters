using System;

namespace Networking.Serialization
{
    public interface ISerializer
    {
        byte[] Serialize(object obj);
        object Deserialize(byte[] data);
    }
}

