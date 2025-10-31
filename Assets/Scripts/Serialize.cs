using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[Serializable]
public class Pokemon
{
    public string name;

    public Pokemon(string name)
    {
        this.name = name;
    }
}

[Serializable]
public struct DTO
{
    public string playerName;
    public int level;
    public List<Pokemon> ownedPokemons;

    public DTO(string player, int lvl, List<Pokemon> owned)
    {
        playerName = player;
        level = lvl;
        ownedPokemons = owned;
    }
}



public class Serialize
{

    public void Start()
    {

        //Pokemon charmander = new Pokemon("Charmander");
        //Pokemon bulbasur = new Pokemon("Bulbasur");
        //Pokemon squirtle = new Pokemon("Squirtle");
        //Pokemon pikachu = new Pokemon("Pikachu");
        //List<Pokemon> list = new List<Pokemon>(new Pokemon[] { charmander, bulbasur, squirtle, pikachu });

        //DTO data = new DTO("Joan", 10, list);

        //Debug.Log($"{data.playerName}");
        //Debug.Log($"{data.level}");
        //for (int i = 0; i < data.ownedPokemons.Count; i++)
        //{
        //    Debug.Log($"{data.ownedPokemons[i].name}");
        //}

        //DTO result = Deserialize(SerializeJson(data));
    }


    public byte[] SerializeJson(DTO data)
    {
        byte[] objectAsBytes;
        MemoryStream stream = new MemoryStream();
        BinaryFormatter formatter = new BinaryFormatter();

        try
        {
            formatter.Serialize(stream, data);
        }
        catch (SerializationException e)
        {
            Debug.Log("Serialization Failed : " + e.Message);
            return null;
        }

        objectAsBytes = stream.ToArray();
        stream.Close();
        return objectAsBytes;
    }

    public DTO Deserialize(byte[] objectAsBytes)
    {
        DTO t = new DTO();
        MemoryStream stream = new MemoryStream();
        stream.Write(objectAsBytes, 0, objectAsBytes.Length);
        stream.Seek(0, SeekOrigin.Begin);
        BinaryFormatter formatter = new BinaryFormatter();
        try
        {
            t = (DTO)formatter.Deserialize(stream);
        }  
        catch (SerializationException e)
        {
            Debug.Log("Deserialization Failed : " + e.Message);
        }
        stream.Close();
        return t;
    }
}
