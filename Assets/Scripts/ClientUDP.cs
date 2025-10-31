using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
public class ClientUDP : MonoBehaviour
{
    bool m_ClientConnected = false;
    Thread m_clientThread;
    public bool connect = false;
    public bool disconnect = false;


    Serialize serializer;
    private DTO data;
    byte[] message;

    [Header("DTO")]
    public string playerName = "John";
    public int level = 1;
    public List<Pokemon> ownedPokemons;

    void Start()
    {
        serializer = new Serialize();
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
        message = serializer.SerializeJson(data);

        clientSocket.SendTo(message, ipep);
        byte[] buffer = new byte[4056];
        int numMsg = 0;

        EndPoint Remote = new IPEndPoint(IPAddress.Any, 0);
        Debug.Log("Client: Waiting messages from server");

        while (m_ClientConnected)
        {
            Thread.Sleep(100 + 200 * numMsg);

            int recived = clientSocket.ReceiveFrom(buffer, ref Remote);
            if (recived > 0)
            {
                string text = System.Text.Encoding.UTF8.GetString(buffer, 0, recived);
                string clientLog = "Client recived from " + Remote.ToString() + ": " + text + " num: " + numMsg;
                Debug.Log(clientLog);
                numMsg++;
                clientSocket.SendTo(message, Remote);
            }
        }
        clientSocket.Close();
    }
}
