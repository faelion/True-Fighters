using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class MsgSerializer
{
    private static BinaryFormatter CreateFormatter()
    {
        var fmt = new BinaryFormatter();
        fmt.SurrogateSelector = NetSurrogateRegistry.CreateSelectorWithNetMessages();
        return fmt;
    }

    public static byte[] Serialize(object obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        var fmt = CreateFormatter();
        using (var ms = new MemoryStream())
        {
            fmt.Serialize(ms, obj);
            return ms.ToArray();
        }
    }

    public static object Deserialize(byte[] data)
    {
        if (data == null || data.Length == 0) throw new ArgumentException("empty data", nameof(data));
        var fmt = CreateFormatter();
        using (var ms = new MemoryStream(data))
        {
            return fmt.Deserialize(ms);
        }
    }
}
