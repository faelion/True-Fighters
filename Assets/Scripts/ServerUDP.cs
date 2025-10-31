using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
public class ServerUDP : MonoBehaviour
{
    Thread m_serverThread;
    public bool m_ServerConnected = true;
    DTO client;
    private void Start()
    {
        m_serverThread = new Thread(ServerSocket);
        m_serverThread.Start();
    }
    void ServerSocket()
    {
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint ipep = new IPEndPoint(IPAddress.Loopback, 9050);
        Debug.Log("Server: Creating UDP Socket");
        serverSocket.Bind(ipep);
        Debug.Log("Server: Listening For UDP Package");

        byte[] message;

        byte[] buffer = new byte[4056];

        EndPoint Remote = new IPEndPoint(IPAddress.Any, 0);

        while (m_ServerConnected)
        {
            Thread.Sleep(500);

            int recived = 0;
            try
            {
                recived = serverSocket.ReceiveFrom(buffer, ref Remote);
            } catch
            {
                Debug.Log("Connection closed");
            }

            if (recived > 0)
            {
                client = Deserializer.DeserializeDTO(buffer);
                string serverLog = "Server recived from " + Remote.ToString();
                Debug.Log(serverLog);
                Debug.Log($"Player Name: {client.playerName}");
                Debug.Log($"Level: {client.level}");
                List<Pokemon> ownedPokemon = client.ownedPokemons;
                foreach (Pokemon p in ownedPokemon)
                {
                    Debug.Log(p.name);
                }
                client.level++;
                message = Serializer.SerializeDTO(client);
                serverSocket.SendTo(message, Remote);
            }
        }
        serverSocket.Close();
    }
}
