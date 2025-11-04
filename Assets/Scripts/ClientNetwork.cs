using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientNetwork : MonoBehaviour
{
    public string serverHost = "127.0.0.1";
    public int serverPort = 9050;

    public GameObject playerVisualPrefab;
    public GameObject serverObjectVisualPrefab;
    public GameObject projectilePrefab;

    public int clientPlayerId = 0;

    private Socket udp;
    private Thread recvThread;
    private volatile bool running = false;
    private EndPoint serverEndpoint;

    private ConcurrentQueue<(object msg, IPEndPoint remote)> incoming = new ConcurrentQueue<(object, IPEndPoint)>();

    private GameObject localPlayerGO = null;
    private GameObject serverObjectGO = null;
    private Dictionary<int, GameObject> projectiles = new Dictionary<int, GameObject>();

    private Dictionary<int, GameObject> otherPlayers = new Dictionary<int, GameObject>();

    private volatile bool hasAssignedId = false;
    private int assignedPlayerId = 0;
    private int joinAttempts = 0;
    private const int MAX_JOIN_ATTEMPTS = 8;
    private float joinRetryDelay = 0.6f;
    private float timeSinceLastJoin = 0f;

    public bool HasAssignedId => hasAssignedId;
    public int AssignedPlayerId => assignedPlayerId;

    void Start()
    {
        if (!string.IsNullOrEmpty(NetworkConfig.serverHost))
            serverHost = NetworkConfig.serverHost;
        if (NetworkConfig.serverPort != 0)
            serverPort = NetworkConfig.serverPort;

        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.Bind(new IPEndPoint(IPAddress.Any, 0));
        udp.ReceiveTimeout = 1000;

        serverEndpoint = new IPEndPoint(IPAddress.Parse(serverHost), serverPort);

        running = true;
        recvThread = new Thread(RecvLoop) { IsBackground = true };
        recvThread.Start();

        SendJoinRequest();
    }

    void OnDestroy()
    {
        running = false;
        try { udp?.Close(); } catch { }
        if (recvThread != null && recvThread.IsAlive) recvThread.Join(500);
    }

    public void SendInput(InputMessage input)
    {
        if (!hasAssignedId) return;
        input.playerId = assignedPlayerId;
        SendToServer(input);
    }

    private void SendToServer(object msg)
    {
        try
        {
            byte[] data = MsgSerializer.Serialize(msg);
            udp.SendTo(data, serverEndpoint);
        }
        catch (Exception e)
        {
            Debug.LogError("Client send failed: " + e);
        }
    }

    public void SendJoinRequest()
    {
        var jr = new JoinRequestMessage() { playerName = NetworkConfig.playerName ?? "Player" };
        try
        {
            byte[] b = MsgSerializer.Serialize(jr);
            udp.SendTo(b, serverEndpoint);
            joinAttempts++;
            timeSinceLastJoin = 0f;
            Debug.Log($"Client: Sent JoinRequest attempt {joinAttempts}");
        }
        catch (Exception e)
        {
            Debug.LogError("SendJoinRequest failed: " + e);
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
                if (se.SocketErrorCode == SocketError.TimedOut) continue;
                Debug.LogError("Client receive socket exception: " + se);
                Thread.Sleep(100);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception e)
            {
                Debug.LogError("Client recv loop error: " + e);
                break;
            }
        }
    }

    void Update()
    {
        if (!hasAssignedId && joinAttempts < MAX_JOIN_ATTEMPTS)
        {
            timeSinceLastJoin += Time.deltaTime;
            if (timeSinceLastJoin >= joinRetryDelay)
            {
                SendJoinRequest();
            }
        }

        while (incoming.TryDequeue(out var tup))
        {
            object msg = tup.msg;

            if (msg is JoinResponseMessage jr)
            {
                assignedPlayerId = jr.assignedPlayerId;
                hasAssignedId = true;
                clientPlayerId = assignedPlayerId;
                Debug.Log($"Client: Received JoinResponse -> assigned id {assignedPlayerId}");
            }
            else if (msg is StateMessage s)
            {
                HandleStateMessage(s);
            }
            else if (msg is ProjectileSpawnMessage ps)
            {
                HandleProjectileSpawn(ps);
            }
            else if (msg is ProjectileStateMessage pst)
            {
                HandleProjectileState(pst);
            }
            else
            {
                Debug.Log("Client received unknown message type: " + msg.GetType());
            }
        }

        CleanDestroyedProjectiles();
    }

    private void HandleStateMessage(StateMessage s)
    {
        if (s.playerId == assignedPlayerId)
        {
            if (localPlayerGO == null)
            {
                if (playerVisualPrefab != null)
                {
                    localPlayerGO = Instantiate(playerVisualPrefab, new Vector3(s.posX, 0f, s.posY), Quaternion.Euler(0f, s.rotZ, 0f));
                    SceneManager.MoveGameObjectToScene(localPlayerGO, this.gameObject.scene);
                    localPlayerGO.tag = "Player";
                }
                else
                {
                    Debug.LogWarning("Client: playerVisualPrefab not set, cannot create local player visual.");
                }
            }
            else
            {
                if (localPlayerGO != null)
                {
                    localPlayerGO.transform.position = new Vector3(s.posX, 0f, s.posY);
                    localPlayerGO.transform.rotation = Quaternion.Euler(0f, s.rotZ, 0f);
                }
            }
        }
        else
        {
            if (s.playerId == 999)
            {
                // NPC
                if (serverObjectGO == null && serverObjectVisualPrefab != null)
                {
                    serverObjectGO = Instantiate(serverObjectVisualPrefab, new Vector3(s.posX, 0f, s.posY), Quaternion.Euler(0f, s.rotZ, 0f));
                    SceneManager.MoveGameObjectToScene(serverObjectGO, this.gameObject.scene);
                }
                else if (serverObjectGO != null)
                {
                    serverObjectGO.transform.position = new Vector3(s.posX, 0f, s.posY);
                    serverObjectGO.transform.rotation = Quaternion.Euler(0f, s.rotZ, 0f);
                }
                return;
            }

            // Other players
            if (!otherPlayers.TryGetValue(s.playerId, out var otherGO) || otherGO == null)
            {
                if (playerVisualPrefab != null)
                {
                    var go = Instantiate(playerVisualPrefab, new Vector3(s.posX, 0f, s.posY), Quaternion.Euler(0f, s.rotZ, 0f));
                    SceneManager.MoveGameObjectToScene(go, this.gameObject.scene);
                    otherPlayers[s.playerId] = go;
                }
            }
            else
            {
                otherGO.transform.position = new Vector3(s.posX, 0f, s.posY);
                otherGO.transform.rotation = Quaternion.Euler(0f, s.rotZ, 0f);
            }
        }
    }

    private void HandleProjectileSpawn(ProjectileSpawnMessage ps)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("ProjectileSpawnMessage received but projectilePrefab is not set.");
            return;
        }

        if (projectiles.TryGetValue(ps.projectileId, out var existing))
        {
            if (existing == null)
                projectiles.Remove(ps.projectileId);
            else
            {
                Debug.LogWarning($"Projectile spawn for already-existing id {ps.projectileId} - ignoring.");
                return;
            }
        }

        var go = Instantiate(projectilePrefab, new Vector3(ps.posX, 0f, ps.posY), Quaternion.identity);
        SceneManager.MoveGameObjectToScene(go, this.gameObject.scene);
        float angle = Mathf.Atan2(ps.dirY, ps.dirX) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, angle, 0f);
        projectiles[ps.projectileId] = go;

        if (ps.lifeMs > 0)
            Destroy(go, ps.lifeMs / 1000f + 0.25f);
    }

    private void HandleProjectileState(ProjectileStateMessage pst)
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
        else
        {
        }
    }

    private void CleanDestroyedProjectiles()
    {
        List<int> toRemove = null;
        foreach (var kv in projectiles)
        {
            if (kv.Value == null)
            {
                if (toRemove == null) toRemove = new List<int>();
                toRemove.Add(kv.Key);
            }
        }
        if (toRemove != null)
        {
            foreach (int id in toRemove) projectiles.Remove(id);
        }
    }
}
