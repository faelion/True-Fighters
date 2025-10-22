using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Serialize : MonoBehaviour
{
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
        string playerName;
        int level;
        List<Pokemon> ownedPokemons;

        public DTO(string player, int lvl, List<Pokemon> owned)
        {
            playerName = player;
            level = lvl;
            ownedPokemons = owned;
        }
    }

    MemoryStream stream;

    void Start()
    {
        Pokemon charmander = new Pokemon("Charmander");
        Pokemon bulbasur = new Pokemon("Bulbasur");
        Pokemon squirtle = new Pokemon("Squirtle");
        Pokemon pikachu = new Pokemon("Pikachu");
        List<Pokemon> list = new List<Pokemon>(new Pokemon[] { charmander, bulbasur, squirtle, pikachu });

        DTO data = new DTO("Joan", 10, list);

        SerializeJson(data);
    }


    void SerializeJson(DTO data)
    {
        string json = JsonUtility.ToJson(data);
        stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write(json);
    }

    void Deserialize()
    {

    }
}
