using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class SocketUDP : MonoBehaviour
{
    Thread m_serverThread;
    Thread m_clientThread;

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

        clientSocket.SendTo(System.Text.Encoding.UTF8.GetBytes("Hello"), ipep);

        byte[] buffer = new byte[4056];
        int numMsg = 0;
        while (true) // NO SE QUE VA AQUÍ
        {
            Thread.Sleep(100 + 200 * numMsg);
            int recived = clientSocket.ReceiveFrom(buffer, ref ipep);
            if (recived > 0)
            {
                string text = System.Text.Encoding.UTF8.GetString(buffer, 0, recived);
                Debug.Log("Client recived: " + text);
                text = "Msg from client: " + numMsg;
                numMsg++;
                clientSocket.SendTo(System.Text.Encoding.UTF8.GetBytes(text), ipep);
            }
        }
        clientSocket.Close();
    }

    void ServerSocket()
    {
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint ipep = new IPEndPoint(IPAddress.Loopback, 9050);
        serverSocket.Bind(ipep);
        m_clientThread.Start();

        byte[] buffer = new byte[4056];
        int numMsg = 0;

        while (true) // NO SE QUE VA AQUÍ
        {
            Thread.Sleep(100 + 200 * numMsg);
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            int recived = serverSocket.ReceiveFrom(buffer, ref sender);
            if (recived > 0)
            {
                string text = System.Text.Encoding.UTF8.GetString(buffer, 0, recived);
                Debug.Log("Server recived: " + text);
                text = "Msg from server: " + numMsg;
                numMsg++;
                serverSocket.SendTo(System.Text.Encoding.UTF8.GetBytes(text), ipep);
            }
        }
        serverSocket.Close();
    }
}
