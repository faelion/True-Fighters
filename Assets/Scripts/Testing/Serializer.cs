using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class Serializer
{
    public static byte[] SerializeDTO(DTO dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var formatter = new BinaryFormatter();
        formatter.SurrogateSelector = SurrogateHelpers.CreateSurrogateSelector();

        using (var ms = new MemoryStream())
        {
            formatter.Serialize(ms, dto);
            return ms.ToArray();
        }
    }
}
