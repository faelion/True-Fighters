using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class ServerNetwork : MonoBehaviour
{
    public int listenPort = 9050;

    private Socket udp;
    private Thread recvThread;
    private volatile bool running;

    private ConcurrentQueue<(object msg, IPEndPoint remote)> incoming = new ConcurrentQueue<(object, IPEndPoint)>();

    private int nextProjectileId = 1;

    public GameObject playerPrefab;
    public GameObject serverObjectPrefab;
    public GameObject projectilePrefab;

    private Dictionary<int, ServerPlayer> players = new Dictionary<int, ServerPlayer>();
    private ServerNPC serverObject;
    private Dictionary<int, ServerProjectile> projectiles = new Dictionary<int, ServerProjectile>();

    private Dictionary<IPEndPoint, int> endpointToPlayerId = new Dictionary<IPEndPoint, int>();
    private Dictionary<int, IPEndPoint> playerIdToEndpoint = new Dictionary<int, IPEndPoint>();
    private int nextPlayerId = 1;

    void Start()
    {
        serverObject = new ServerNPC() { id = 999, posX = 2f, posY = 0f, speed = 2.0f };

        udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udp.ReceiveTimeout = 1000;
        udp.Bind(new IPEndPoint(IPAddress.Loopback, listenPort));

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

    private void RecvLoop()
    {
        byte[] buffer = new byte[8192];
        EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
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
                Debug.LogError("Server recv error: " + se);
                break;
            }
            catch (ObjectDisposedException) 
            { 
                break; 
            }
            catch (Exception e) 
            { 
                Debug.LogError("Server recv loop: " + e); 
                break; 
            }
        }
    }

    void Update()
    {
        while (incoming.TryDequeue(out var tup))
        {
            object msg = tup.msg;
            IPEndPoint remote = tup.remote;

            if (msg is JoinRequestMessage jr)
            {
                HandleJoinRequest(jr, remote);
            }
            else if (msg is InputMessage im)
            {
                HandleInput(im, remote);
            }

        }

        float dt = Time.deltaTime;
        foreach (var p in players.Values)
        {
            p.Simulate(dt);
        }

        if (serverObject.target == null && players.Count > 0)
        {
            foreach(var p in players.Values)
            {
                if(p.posX - serverObject.posX < serverObject.followRange &&
                   p.posY - serverObject.posY < serverObject.followRange)
                {
                    serverObject.target = p;
                    break;
                }
            }
        }else if (serverObject.target != null)
        {
            float dx = serverObject.target.posX - serverObject.posX;
            float dy = serverObject.target.posY - serverObject.posY;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist > serverObject.followRange)
            {
                serverObject.target = null;
            }
        }
        serverObject.Simulate(dt);

        var toRemove = new List<int>();
        foreach (var kv in projectiles)
        {
            var pr = kv.Value;
            pr.Simulate(dt);
            if (pr.lifeMs <= 0)
                toRemove.Add(pr.id);
            else
            {
                foreach (var p in players.Values)
                {
                    if (p.playerId == pr.ownerPlayerId)
                        continue;

                    float dx = p.posX - pr.posX;
                    float dy = p.posY - pr.posY;
                    float dist2 = dx * dx + dy * dy;
                    if (dist2 < 0.25f)
                    {
                        p.hit = true;
                        pr.lifeMs = 0;
                        break; 
                    }
                }
            }
        }

        foreach (var endpoint in endpointToPlayerId.Keys)
        {
            foreach (var p in players.Values)
            {
                var st = new StateMessage()
                {
                    playerId = p.playerId,
                    hit = p.hit,
                    posX = p.posX,
                    posY = p.posY,
                    rotZ = p.rotZ,
                    tick = Environment.TickCount
                };
                SendMessageToClient(st, endpoint);
            }

            var so = new StateMessage()
            {
                playerId = serverObject.id,
                posX = serverObject.posX,
                posY = serverObject.posY,
                rotZ = 0,
                tick = Environment.TickCount
            };
            SendMessageToClient(so, endpoint);

            foreach (var pr in projectiles.Values)
            {
                var pst = new ProjectileStateMessage()
                {
                    projectileId = pr.id,
                    posX = pr.posX,
                    posY = pr.posY,
                    lifeMsRemaining = pr.lifeMs
                };
                SendMessageToClient(pst, endpoint);
            }
        }

        foreach (int id in toRemove)
        {
            projectiles.Remove(id);
        }

    }

    private void HandleInput(InputMessage im, IPEndPoint remote)
    {
        if (!players.ContainsKey(im.playerId))
        {
            players[im.playerId] = new ServerPlayer() { playerId = im.playerId, posX = 0f, posY = 0f, speed = 3.5f };
        }

        var pl = players[im.playerId];

        if (im.isMove)
        {
            pl.SetDestination(im.targetX, im.targetY);
        }
        else
        {
            if (im.skillKey == "Q")
            {
                int pid = nextProjectileId++;
                var dirX = im.targetX - pl.posX;
                var dirY = im.targetY - pl.posY;
                float len = Mathf.Sqrt(dirX * dirX + dirY * dirY);
                if (len <= 0.001f) { dirX = 1; dirY = 0; len = 1; }
                dirX /= len; dirY /= len;
                var projectile = new ServerProjectile()
                {
                    id = pid,
                    ownerPlayerId = im.playerId,
                    posX = pl.posX,
                    posY = pl.posY,
                    dirX = dirX,
                    dirY = dirY,
                    speed = 8f,
                    lifeMs = 1500
                };
                projectiles[pid] = projectile;

                var spawn = new ProjectileSpawnMessage()
                {
                    projectileId = pid,
                    ownerPlayerId = im.playerId,
                    posX = projectile.posX,
                    posY = projectile.posY,
                    dirX = projectile.dirX,
                    dirY = projectile.dirY,
                    speed = projectile.speed,
                    lifeMs = projectile.lifeMs
                };

                foreach (var endpoint in endpointToPlayerId.Keys)
                {
                    SendMessageToClient(spawn, endpoint);
                }
            }
        }
    }

    private void SendMessageToClient(object msg, IPEndPoint remote)
    {
        try
        {
            byte[] b = MsgSerializer.Serialize(msg);
            udp.SendTo(b, remote);
        }
        catch (Exception e)
        {
            Debug.LogError("Server send failed: " + e);
        }
    }

    public class ServerPlayer
    {
        public int playerId;
        public string name;
        public float posX, posY;
        public float rotZ;
        public float speed = 3.5f;
        private float destX, destY;
        private bool hasDest = false;
        public bool hit = false;
        private float hitTimer = 0f;
        private const float hitDuration = 0.2f;

        public void SetDestination(float x, float y)
        {
            destX = x; destY = y; hasDest = true;
        }

        public void Simulate(float dt)
        {
            if (hit)
            {
                hitTimer += dt;
                if (hitTimer >= hitDuration)
                {
                    hitTimer = 0f;
                    hit = false;
                }
            }

            if (!hasDest) return;
            float dx = destX - posX;
            float dy = destY - posY;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist < 0.05f)
            {
                posX = destX; posY = destY; hasDest = false;
            }
            else
            {
                float move = speed * dt;
                if (move > dist) move = dist;
                posX += dx / dist * move;
                posY += dy / dist * move;
                rotZ = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            }
        }
    }

    private class ServerProjectile
    {
        public int id;
        public int ownerPlayerId;
        public float posX, posY;
        public float dirX, dirY;
        public float speed;
        public int lifeMs;

        public void Simulate(float dt)
        {
            float step = speed * dt;
            posX += dirX * step;
            posY += dirY * step;
            lifeMs -= (int)(dt * 1000f);
        }
    }

    public class ServerNPC
    {
        public int id;
        public float posX, posY;
        public float speed = 3f;
        public float followRange = 6f;
        public float stopRange = 2f;
        public ServerPlayer target = null; // jugador al que seguir

        public void Simulate(float deltaTime)
        {
            if (target == null) return;

            float dx = target.posX - posX;
            float dy = target.posY - posY;
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist < followRange && dist > stopRange)
            {
                posX += dx / dist * speed * deltaTime;
                posY += dy / dist * speed * deltaTime;
            }
        }
    }

    private void HandleJoinRequest(JoinRequestMessage jr, IPEndPoint remote)
    {
        if (endpointToPlayerId.TryGetValue(remote, out int existing))
        {
            SendJoinResponse(existing, remote);
            return;
        }

        int assigned;
        assigned = nextPlayerId++;

        endpointToPlayerId[remote] = assigned;
        playerIdToEndpoint[assigned] = remote;

        if (!players.ContainsKey(assigned))
        {
            players[assigned] = new ServerPlayer() { playerId = assigned, posX = 0f, posY = 0f, speed = 3.5f, name = jr.playerName };
        }

        Debug.Log($"Assigned playerId {assigned} to {remote} with name '{jr.playerName}'");

        SendJoinResponse(assigned, remote);

        var playerState = new StateMessage() { playerId = assigned, posX = players[assigned].posX, posY = players[assigned].posY, rotZ = players[assigned].rotZ, tick = Environment.TickCount };
        SendMessageToClient(playerState, remote);

        var soState = new StateMessage() { playerId = serverObject.id, posX = serverObject.posX, posY = serverObject.posY, rotZ = 0, tick = Environment.TickCount };
        SendMessageToClient(soState, remote);
    }

    private void SendJoinResponse(int assignedId, IPEndPoint remote)
    {
        var resp = new JoinResponseMessage() { assignedPlayerId = assignedId, serverTick = Environment.TickCount };
        try
        {
            byte[] b = MsgSerializer.Serialize(resp);
            udp.SendTo(b, remote);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to send JoinResponse: " + e);
        }
    }

    public List<ServerPlayer> GetPlayerList()
    {
        return players.Values.ToList();
    }

}
