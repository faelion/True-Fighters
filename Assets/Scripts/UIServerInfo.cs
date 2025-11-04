using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UIServerInfo : MonoBehaviour
{
    public Text infoText;
    private ServerNetwork server;

    void Start()
    {
        server = GameObject.Find("ServerNetwork").GetComponent<ServerNetwork>();
    }

    void Update()
    {
        if (server == null || infoText == null) return;

        var players = server.GetPlayerList();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Users Online: {players.Count}");
        sb.AppendLine("-------------------------");

        foreach (var p in players)
        {
            sb.AppendLine($"ID: {p.playerId} | Name: {p.name}");
        }

        infoText.text = sb.ToString();
    }
}
