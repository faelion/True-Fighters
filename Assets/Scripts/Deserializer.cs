using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class Deserializer
{
    public static DTO DeserializeDTO(byte[] data)
    {
        using (MemoryStream stream = new MemoryStream(data))
        using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
        {
            string playerName = ReadString(reader);
            int level = reader.ReadInt32();

            int pokemonCount = reader.ReadInt32();
            List<Pokemon> pokemons = new List<Pokemon>();

            for (int i = 0; i < pokemonCount; i++)
                pokemons.Add(new Pokemon(ReadString(reader)));

            return new DTO(playerName, level, pokemons);
        }
    }

    private static string ReadString(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        byte[] bytes = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }
}
