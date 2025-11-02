using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
public class Pokemon
{
    public string name;
    public Pokemon() { }
    public Pokemon(string name) { this.name = name; }
}

public class DTO
{
    public string playerName;
    public int level;
    public List<Pokemon> ownedPokemons;

    public DTO() { }
    public DTO(string playerName, int level, List<Pokemon> pokemons)
    {
        this.playerName = playerName;
        this.level = level;
        this.ownedPokemons = pokemons;
    }
}

public class PokemonSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
    {
        Pokemon p = obj as Pokemon;
        info.AddValue("name", p?.name ?? string.Empty);
    }

    public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
    {
        Pokemon p = obj as Pokemon ?? new Pokemon();
        p.name = info.GetString("name");
        return p;
    }
}

public class DTOSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
    {
        DTO dto = obj as DTO;
        info.AddValue("playerName", dto?.playerName ?? string.Empty);
        info.AddValue("level", dto != null ? dto.level : 0);
        info.AddValue("ownedPokemons", dto?.ownedPokemons ?? new List<Pokemon>());
    }

    public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
    {
        DTO dto = obj as DTO ?? new DTO();
        dto.playerName = info.GetString("playerName");
        dto.level = info.GetInt32("level");
        dto.ownedPokemons = (List<Pokemon>)info.GetValue("ownedPokemons", typeof(List<Pokemon>));
        return dto;
    }
}

public static class SurrogateHelpers
{
    public static SurrogateSelector CreateSurrogateSelector()
    {
        var selector = new SurrogateSelector();
        var ctx = new StreamingContext(StreamingContextStates.All);
        
        selector.AddSurrogate(typeof(Pokemon), ctx, new PokemonSurrogate());
        selector.AddSurrogate(typeof(DTO), ctx, new DTOSurrogate());

        return selector;
    }
}


public class Serialize
{

    //public void Start()
    //{

    //    //Pokemon charmander = new Pokemon("Charmander");
    //    //Pokemon bulbasur = new Pokemon("Bulbasur");
    //    //Pokemon squirtle = new Pokemon("Squirtle");
    //    //Pokemon pikachu = new Pokemon("Pikachu");
    //    //List<Pokemon> list = new List<Pokemon>(new Pokemon[] { charmander, bulbasur, squirtle, pikachu });

    //    //DTO data = new DTO("Joan", 10, list);

    //    //Debug.Log($"{data.playerName}");
    //    //Debug.Log($"{data.level}");
    //    //for (int i = 0; i < data.ownedPokemons.Count; i++)
    //    //{
    //    //    Debug.Log($"{data.ownedPokemons[i].name}");
    //    //}

    //    //DTO result = Deserialize(SerializeJson(data));
    //}


    //public byte[] SerializeJson(DTO data)
    //{
    //    byte[] objectAsBytes;
    //    MemoryStream stream = new MemoryStream();
    //    BinaryFormatter formatter = new BinaryFormatter();

    //    try
    //    {
    //        formatter.Serialize(stream, data);
    //    }
    //    catch (SerializationException e)
    //    {
    //        Debug.Log("Serialization Failed : " + e.Message);
    //        return null;
    //    }

    //    objectAsBytes = stream.ToArray();
    //    stream.Close();
    //    return objectAsBytes;
    //}

    //public DTO Deserialize(byte[] objectAsBytes)
    //{
    //    DTO t = new DTO();
    //    MemoryStream stream = new MemoryStream();
    //    stream.Write(objectAsBytes, 0, objectAsBytes.Length);
    //    stream.Seek(0, SeekOrigin.Begin);
    //    BinaryFormatter formatter = new BinaryFormatter();
    //    try
    //    {
    //        t = (DTO)formatter.Deserialize(stream);
    //    }  
    //    catch (SerializationException e)
    //    {
    //        Debug.Log("Deserialization Failed : " + e.Message);
    //    }
    //    stream.Close();
    //    return t;
    //}
}
