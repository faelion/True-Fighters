using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
public class ClientUDP : MonoBehaviour
{
    bool m_ClientConnected = false;
    Thread m_clientThread;
    public bool connect = false;
    public bool disconnect = false;

    private DTO data;

    [Header("DTO")]
    public string playerName = "John";
    public int level = 1;
    public List<Pokemon> ownedPokemons;

    void Start()
    {
        data = new DTO(playerName, level, ownedPokemons);
        m_clientThread = new Thread(ClientSocket);
    }

    private void Update()
    {
        if (connect)
        {
            m_ClientConnected = true;
            m_clientThread.Start();
            connect = false;
        }
        if (disconnect) //WIP
        {
            m_ClientConnected = false;
            m_clientThread.Join();
            disconnect = false;
        }
    }

    public void ClientSocket()
    {
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        EndPoint ipep = new IPEndPoint(IPAddress.Loopback, 9050);
        Debug.Log("Client: Creating UDP Socket");
        Debug.Log("Client: Sending First message");

        byte[] message;
        message = Serializer.SerializeDTO(data);

        clientSocket.SendTo(message, ipep);
        byte[] buffer = new byte[4056];

        EndPoint Remote = new IPEndPoint(IPAddress.Any, 0);
        Debug.Log("Client: Waiting messages from server");

        while (m_ClientConnected)
        {
            Thread.Sleep(500);

            int recived = clientSocket.ReceiveFrom(buffer, ref Remote);
            if (recived > 0)
            {
                string serverLog = "Server recived from " + Remote.ToString();
                Debug.Log(serverLog);
                data = Deserializer.DeserializeDTO(buffer);
                Debug.Log($"Player Name: {data.playerName}");
                Debug.Log($"Level: {data.level}");
                List<Pokemon> ownedPokemon = data.ownedPokemons;
                foreach (Pokemon p in ownedPokemon)
                {
                    Debug.Log(p.name);
                }
                data.level++;
                message = Serializer.SerializeDTO(this.data);
                clientSocket.SendTo(message, Remote);
            }
        }
        clientSocket.Close();
    }
}
