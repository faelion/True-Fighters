using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Pokemon
{
    public string name;

    public Pokemon(string name)
    {
        this.name = name;
    }
}

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



public class Serialize : MonoBehaviour
{

    MemoryStream stream;

    public void Start()
    {
        Pokemon charmander = new Pokemon("Charmander");
        Pokemon bulbasur = new Pokemon("Bulbasur");
        Pokemon squirtle = new Pokemon("Squirtle");
        Pokemon pikachu = new Pokemon("Pikachu");
        List<Pokemon> list = new List<Pokemon>(new Pokemon[] { charmander, bulbasur, squirtle, pikachu });

        DTO data = new DTO("Joan", 10, list);

        Debug.Log($"{data.playerName}");
        Debug.Log($"{data.level}");
        for (int i = 0; i < data.ownedPokemons.Count; i++)
        {
            Debug.Log($"{data.ownedPokemons[i].name}");
        }

        SerializeJson(data);
        Deserialize();
    }


    void SerializeJson(DTO data)
    {
        string json = JsonUtility.ToJson(data);
        stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write(json);
        List<Pokemon> list = new List<Pokemon>(data.ownedPokemons);
        writer.Close();

    }

    void Deserialize()
    {
        DTO t;
        BinaryReader reader = new BinaryReader(stream);
        stream.Seek(0, SeekOrigin.Begin);
        string json = reader.ReadString();
        t = JsonUtility.FromJson<DTO>(json);
        Debug.Log(t.playerName);
        Debug.Log(t.level);
        List<Pokemon> list = new List<Pokemon>(t.ownedPokemons);
        for (int i = 0; i < list.Count; i++)
        {
            Debug.Log($"{list[i].name}");
        }
    }
}
