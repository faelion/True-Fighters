using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
public class ServerUDP : MonoBehaviour
{
    Serialize serializer;
    Thread m_serverThread;
    public bool m_ServerConnected = true;
    DTO client;
    private void Start()
    {
        serializer = new Serialize();
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

        byte[] buffer = new byte[4056];
        int numMsg = 0;

        EndPoint Remote = new IPEndPoint(IPAddress.Any, 0);

        while (m_ServerConnected)
        {
            Thread.Sleep(100 + 200 * numMsg);

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
                client = serializer.Deserialize(buffer);
                //string text = System.Text.Encoding.UTF8.GetString(buffer, 0, recived);
                string serverLog = "Server recived from " + Remote.ToString() + ": " + client.playerName + " num: " + numMsg;
                List<Pokemon> ownedPokemon = client.ownedPokemons;
                foreach (Pokemon p in ownedPokemon)
                {
                    Debug.Log(p.name);
                }
                Debug.Log(serverLog);
                numMsg++;
                serverSocket.SendTo(System.Text.Encoding.UTF8.GetBytes("ping"), Remote);
            }
        }
        serverSocket.Close();
    }
}
