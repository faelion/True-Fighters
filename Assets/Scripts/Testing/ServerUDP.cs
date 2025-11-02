using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class ServerUDP : MonoBehaviour
{
    Thread m_serverThread;
    private volatile bool m_ServerConnected = true;
    private Socket serverSocket;
    DTO client;

    [SerializeField] int port = 9050;

    private void Start()
    {
        m_ServerConnected = true;
        m_serverThread = new Thread(ServerSocket) { IsBackground = true };
        m_serverThread.Start();
    }

    private void OnDestroy()
    {
        m_ServerConnected = false;

        try
        {
            serverSocket?.Close();
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error closing server socket: " + e);
        }

        if (m_serverThread != null && m_serverThread.IsAlive)
            m_serverThread.Join(1000);
    }

    void ServerSocket()
    {
        try
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            serverSocket.ReceiveTimeout = 1000;

            IPEndPoint ipep = new IPEndPoint(IPAddress.Loopback, port);
            Debug.Log($"Server: Creating UDP Socket and binding to {ipep}");
            serverSocket.Bind(ipep);
            Debug.Log("Server: Listening For UDP Package");

            byte[] buffer = new byte[8192];
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            while (m_ServerConnected)
            {
                try
                {
                    int received = serverSocket.ReceiveFrom(buffer, ref remote);

                    if (received > 0)
                    {
                        byte[] payload = new byte[received];
                        Array.Copy(buffer, 0, payload, 0, received);

                        try
                        {
                            client = Deserializer.DeserializeDTO(payload);
                        }
                        catch (Exception exDes)
                        {
                            Debug.LogError("Failed to deserialize DTO: " + exDes);
                            continue;
                        }

                        string serverLog = "Server received from " + remote.ToString();
                        Debug.Log(serverLog);
                        Debug.Log($"Player Name: {client.playerName}");
                        Debug.Log($"Level: {client.level}");
                        List<Pokemon> ownedPokemon = client.ownedPokemons;
                        if (ownedPokemon != null)
                        {
                            foreach (Pokemon p in ownedPokemon)
                                Debug.Log(" - " + p.name);
                        }

                        client.level++;
                        byte[] reply = Serializer.SerializeDTO(client);
                        serverSocket.SendTo(reply, remote);
                    }
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.TimedOut)
                    {
                        continue;
                    }

                    Debug.LogError("SocketException in server: " + se);
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Unexpected exception in server thread: " + ex);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start server socket: " + e);
        }
        finally
        {
            try { serverSocket?.Close(); } catch { }
            serverSocket = null;
            Debug.Log("Server socket thread exiting.");
        }
    }
}
