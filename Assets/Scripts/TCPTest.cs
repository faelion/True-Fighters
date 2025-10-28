using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TCPTest : MonoBehaviour
{
    Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    IPEndPoint ipep = new IPEndPoint(IPAddress.Loopback, 9050);
    Thread clientThread;
    Thread serverThread;
    public bool clientCancel = true;
    public bool serverCancel = true;

    private void Start()
    {
        serverThread = new Thread(ServerConnect);
        clientThread = new Thread(ClientConnect);

        serverThread.Start();

    }
    void ClientConnect()
    {
        clientSocket.Connect(ipep);
        Debug.Log("Client connectet to the server.");

        byte[] data = new byte[4096];
        int size = 0;

        while (clientCancel)
        {
            size = clientSocket.Receive(data);
            if (size > 0)
            {
                string text = Encoding.UTF8.GetString(data, 0, size);
                Debug.Log($"Cliente recibió: {text}");

                if (text == "hola??")
                {
                    clientSocket.Send(Encoding.UTF8.GetBytes("adios"));
                }

                else if (text == "ping")
                {
                    Debug.Log("Cliente recibió un PING del servidor!");
                    clientSocket.Send(Encoding.UTF8.GetBytes("pong"));
                }

                size = 0;
            }
            Thread.Sleep(500);
        }
    }
    void ServerConnect()
    {

        serverSocket.Bind(ipep);
        serverSocket.Listen(10);

        Debug.Log("Server listening...");
        clientThread.Start();

        Socket clientReturnedSocket = serverSocket.Accept();
        Debug.Log($"Server accepted conection from {clientReturnedSocket.RemoteEndPoint}");

        clientReturnedSocket.Send(Encoding.UTF8.GetBytes("hola??"));

        byte[] data = new byte[4096];
        int size = 0;

        while (serverCancel)
        {
            size = clientReturnedSocket.Receive(data);

            if (size > 0)
            {
                string text = Encoding.UTF8.GetString(data, 0, size);
                Debug.Log($"Server recived: {text}");

                clientReturnedSocket.Send(Encoding.UTF8.GetBytes("ping"));
                size = 0;
            }
            Thread.Sleep(500);
        }
    }
}
