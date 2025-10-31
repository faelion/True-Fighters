using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class SocketUDP : MonoBehaviour
{
    Thread m_serverThread;
    Thread m_clientThread;
    public bool m_ServerConnected = true;
    public bool m_ClientConnected = true;

    private void Start()
    {
        m_serverThread = new Thread(ServerSocket); 
        m_clientThread = new Thread(ClientSocket);
        m_serverThread.Start();
    }

    void ClientSocket()
    {
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        EndPoint ipep = new IPEndPoint(IPAddress.Loopback, 9050);
        Debug.Log("Client: Creating UDP Socket");
        Debug.Log("Client: Sending First message");

        clientSocket.SendTo(System.Text.Encoding.UTF8.GetBytes("Hello"), ipep);
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
                clientSocket.SendTo(System.Text.Encoding.UTF8.GetBytes("pong"), Remote);
            }
        }
        clientSocket.Close();
    }

    void ServerSocket()
    {
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint ipep = new IPEndPoint(IPAddress.Loopback, 9050);
        Debug.Log("Server: Creating UDP Socket");
        serverSocket.Bind(ipep);
        Debug.Log("Server: Listening For UDP Package");
        m_clientThread.Start();

        byte[] buffer = new byte[4056];
        int numMsg = 0;

        EndPoint Remote = new IPEndPoint(IPAddress.Any, 0);

        while (m_ServerConnected)
        {
            Thread.Sleep(100 + 200 * numMsg);

            int recived = serverSocket.ReceiveFrom(buffer, ref Remote);
            if (recived > 0)
            {
                string text = System.Text.Encoding.UTF8.GetString(buffer, 0, recived);
                string serverLog = "Server recived from " + Remote.ToString() + ": " + text + " num: " + numMsg;
                Debug.Log(serverLog);
                numMsg++;
                serverSocket.SendTo(System.Text.Encoding.UTF8.GetBytes("ping"), Remote);
            }
        }
        serverSocket.Close();
    }
}
