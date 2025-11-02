using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class ClientUDP : MonoBehaviour
{
    private volatile bool m_ClientConnected = false;
    private Thread m_clientThread;
    public bool connect = false;
    public bool disconnect = false;

    private Socket clientSocket;
    private DTO data;

    [Header("DTO")]
    public string playerName = "John";
    public int level = 1;
    public List<Pokemon> ownedPokemons;

    [SerializeField] int serverPort = 9050;
    [SerializeField] string serverHost = "127.0.0.1";

    void Start()
    {
        if (ownedPokemons == null || ownedPokemons.Count < 2)
        {
            ownedPokemons = new List<Pokemon>()
            {
                new Pokemon("Pikachu"),
                new Pokemon("Charmander")
            };
        }

        data = new DTO(playerName, level, ownedPokemons);
    }

    private void Update()
    {
        if (connect)
        {
            if (m_clientThread == null || !m_clientThread.IsAlive)
            {
                m_ClientConnected = true;
                m_clientThread = new Thread(ClientSocket) { IsBackground = true };
                m_clientThread.Start();
            }
            else
            {
                Debug.LogWarning("Client thread already running.");
            }
            connect = false;
        }

        if (disconnect)
        {
            m_ClientConnected = false;

            try
            {
                clientSocket?.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Error closing client socket: " + e);
            }

            if (m_clientThread != null && m_clientThread.IsAlive)
                m_clientThread.Join(1000);

            disconnect = false;
        }
    }

    private void OnDestroy()
    {
        m_ClientConnected = false;
        try { clientSocket?.Close(); } catch { }
        if (m_clientThread != null && m_clientThread.IsAlive)
            m_clientThread.Join(1000);
    }

    public void ClientSocket()
    {
        try
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            clientSocket.ReceiveTimeout = 1000;

            EndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverHost), serverPort);
            Debug.Log($"Client: Creating UDP Socket and sending to {serverEndPoint}");

            byte[] message = Serializer.SerializeDTO(data);
            clientSocket.SendTo(message, serverEndPoint);

            byte[] buffer = new byte[8192];
            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            Debug.Log("Client: Waiting messages from server...");

            while (m_ClientConnected)
            {
                try
                {
                    int received = clientSocket.ReceiveFrom(buffer, ref remote);
                    if (received > 0)
                    {
                        byte[] payload = new byte[received];
                        Array.Copy(buffer, 0, payload, 0, received);

                        DTO receivedDto = null;
                        try
                        {
                            receivedDto = Deserializer.DeserializeDTO(payload);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("Client: failed to deserialize payload: " + ex);
                            continue;
                        }

                        Debug.Log("Server replied from: " + remote.ToString());
                        Debug.Log($"Player Name: {receivedDto.playerName}");
                        Debug.Log($"Level: {receivedDto.level}");
                        if (receivedDto.ownedPokemons != null)
                        {
                            foreach (var p in receivedDto.ownedPokemons)
                                Debug.Log(" - " + p.name);
                        }

                        receivedDto.level++;
                        byte[] outMsg = Serializer.SerializeDTO(receivedDto);
                        clientSocket.SendTo(outMsg, remote);
                    }
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.TimedOut)
                    {
                        continue;
                    }

                    Debug.LogError("Client socket exception: " + se);
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Client unexpected exception: " + ex);
                    break;
                }

                Thread.Sleep(10);
            }
        }
        finally
        {
            try { clientSocket?.Close(); } catch { }
            clientSocket = null;
            Debug.Log("Client socket thread exiting.");
        }
    }
}
