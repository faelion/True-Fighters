using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class ClientNetwork : MonoBehaviour
{
    public string serverHost = "127.0.0.1";
    public int serverPort = 9050;
    public int clientPlayerId = 1;

    private Socket udp;
    private Thread recvThread;
    private volatile bool running;
    private EndPoint serverEndpoint;

    private ConcurrentQueue<(object msg, IPEndPoint remote)> incoming = new ConcurrentQueue<(object, IPEndPoint)>();

    public GameObject playerVisualPrefab;
    public GameObject serverObjectVisualPrefab;
    private GameObject localPlayerGO;
    private GameObject serverObjectGO;

    private System.Collections.Generic.Dictionary<int, GameObject> projectiles = new System.Collections.Generic.Dictionary<int, GameObject>();
    public GameObject projectilePrefab;

    void Start()
    {
        localPlayerGO = Instantiate(playerVisualPrefab, Vector3.zero, Quaternion.identity);
        serverObjectGO = Instantiate(serverObjectVisualPrefab, new Vector3(2, 0, 0), Quaternion.identity);

        serverEndpoint = new IPEndPoint(IPAddress.Parse(serverHost), serverPort);

        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.Bind(new IPEndPoint(IPAddress.Any, 0));
        udp.ReceiveTimeout = 1000;
        running = true;
        recvThread = new Thread(RecvLoop) { IsBackground = true };
        recvThread.Start();
    }

    void OnDestroy()
    {
        running = false;
        try { udp?.Close(); } catch { }
        if (recvThread != null && recvThread.IsAlive) recvThread.Join(500);
    }

    public void SendInput(InputMessage input)
    {
        byte[] b = MsgSerializer.Serialize(input);
        try
        {
            udp.SendTo(b, serverEndpoint);
        }
        catch (Exception e)
        {
            Debug.LogError("Client send failed: " + e);
        }
    }

    private void RecvLoop()
    {
        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        byte[] buffer = new byte[8192];

        while (running)
        {
            try
            {
                int r = udp.ReceiveFrom(buffer, ref remoteEP);
                if (r > 0)
                {
                    byte[] payload = new byte[r];
                    Array.Copy(buffer, 0, payload, 0, r);
                    object msg = MsgSerializer.Deserialize(payload);
                    incoming.Enqueue((msg, (IPEndPoint)remoteEP));
                }
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.TimedOut)
                    continue;

                Debug.LogError($"Client receive socket exception: {se} (remoteEP={remoteEP})");
                Thread.Sleep(100);
                continue;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.LogError("Client recv loop unexpected error: " + ex);
                break;
            }
        }
    }

    void Update()
    {
        while (incoming.TryDequeue(out var tup))
        {
            object msg = tup.msg;
            if (msg is StateMessage s)
            {
                if (s.playerId == clientPlayerId)
                {
                    localPlayerGO.transform.position = new Vector3(s.posX, 0f, s.posY);
                    localPlayerGO.transform.rotation = Quaternion.Euler(0f, s.rotZ, 0f);
                }
                else
                {
                    serverObjectGO.transform.position = new Vector3(s.posX, 0f, s.posY);
                    serverObjectGO.transform.rotation = Quaternion.Euler(0f, s.rotZ, 0f);
                }
            }
            else if (msg is ProjectileSpawnMessage ps)
            {
                var go = Instantiate(projectilePrefab, new Vector3(ps.posX, 0f, ps.posY), Quaternion.identity);
                projectiles[ps.projectileId] = go;
                float angle = Mathf.Atan2(ps.dirY, ps.dirX) * Mathf.Rad2Deg;
                go.transform.rotation = Quaternion.Euler(0f, angle, 0f);
                Destroy(go, ps.lifeMs / 1000f + 0.2f);
            }
            else if (msg is ProjectileStateMessage pst)
            {
                if (projectiles.TryGetValue(pst.projectileId, out var go))
                {
                    if (go != null)
                    {
                        go.transform.position = new Vector3(pst.posX, 0f, pst.posY);
                    }
                    else
                    {
                        projectiles.Remove(pst.projectileId);
                    }
                }
            }

        }
    }
}
