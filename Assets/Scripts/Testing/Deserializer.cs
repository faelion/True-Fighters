using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class Deserializer
{
    public static DTO DeserializeDTO(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Length == 0) throw new ArgumentException("data is empty", nameof(data));

        var formatter = new BinaryFormatter();
        formatter.SurrogateSelector = SurrogateHelpers.CreateSurrogateSelector();

        using (var ms = new MemoryStream(data))
        {
            object obj = formatter.Deserialize(ms);
            return obj as DTO;
        }
    }
}
