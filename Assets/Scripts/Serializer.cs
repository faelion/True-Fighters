using System.IO;
using System.Text;
using UnityEngine;

public static class Serializer
{
    public static byte[] SerializeDTO(DTO dto)
    {
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8);
        
        // Player name
        WriteString(writer, dto.playerName);

        // Level
        writer.Write(dto.level);

        // Number of pokemons
        writer.Write(dto.ownedPokemons.Count);

        // Each Pokémon name
        foreach (var p in dto.ownedPokemons)
            WriteString(writer, p.name);
        
        return stream.ToArray();
    }

    private static void WriteString(BinaryWriter writer, string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        writer.Write(bytes.Length); // write string length
        writer.Write(bytes);        // write string bytes
    }
}
