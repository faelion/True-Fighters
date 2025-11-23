using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Buffers;
using Networking.Transport;
using ServerGame;

public class ServerNetwork : MonoBehaviour
{
    public int listenPort = 9050;

    private INetworkTransport transport;
    private ServerWorld world;
    private ServerGame.Systems.SimulationRunner simulation;
    private ServerGame.ConnectionRegistry connections;
    private bool gameStarted = false;
    private string currentSceneName;

    private ServerGame.Managers.ReplicationManager replicationManager;
    private readonly System.Collections.Generic.List<IGameEvent> frameEvents = new System.Collections.Generic.List<IGameEvent>();

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        Init();
    }

    public void Init()
    {
        if (connections != null) return;
        
        ClientContent.ContentAssetRegistry.EnsureLoaded();
        connections = new ServerGame.ConnectionRegistry();
        replicationManager = new ServerGame.Managers.ReplicationManager();
        transport = new UdpTransport();
        transport.Start(new IPEndPoint(IPAddress.Any, listenPort));
        transport.OnReceive += OnReceiveMessage;
        Debug.Log($"[ServerNetwork] Started on port {listenPort}. Waiting for players...");
    }

    public void StartGame(string sceneName)
    {
        if (gameStarted) return;
        Init();
        StartCoroutine(LoadGameScene(sceneName));
    }

    private System.Collections.IEnumerator LoadGameScene(string sceneName)
    {
        Debug.Log($"[ServerNetwork] Loading scene '{sceneName}'...");
        currentSceneName = sceneName;
        
        // Notify clients to load scene
        var startMsg = new StartGameMessage { sceneName = sceneName };
        foreach (var ep in connections.PlayerEndpoints.Values)
        {
            SendMessageToClient(startMsg, ep);
        }

        // Load scene on server
        var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        Debug.Log("[ServerNetwork] Scene loaded. Initializing ServerWorld...");
        
        // Initialize world
        world = new ServerWorld();
        simulation = new ServerGame.Systems.SimulationRunner(world);

        // Spawn already connected players
        foreach (var kv in connections.PlayerEndpoints)
        {
            int pid = kv.Key;
            string heroId = connections.GetHeroId(pid);
            world.EnsurePlayer(pid, $"Player{pid}", heroId);
            replicationManager.RegisterClient(pid);
        }

        gameStarted = true;
    }

    void OnDestroy()
    {
        transport?.Stop();
        transport?.Dispose();
    }

    void Update()
    {
        transport?.Update();
        
        if (gameStarted && simulation != null)
        {
            simulation.Tick(Time.deltaTime);

            int tickNow = Environment.TickCount;
            
            // Consume events from world
            frameEvents.Clear();
            frameEvents.AddRange(world.ConsumePendingEvents());

            // Identify reliable events (e.g. Despawn, ProjectileSpawn) and queue them
            foreach (var ev in frameEvents)
            {
                ev.ServerTick = tickNow; // Assign tick here
                if (IsReliableEvent(ev))
                {
                    replicationManager.EnqueueReliableEvent(ev);
                }
            }

            // Build and send packets
            foreach (var kv in connections.PlayerEndpoints)
            {
                int pid = kv.Key;
                var endpoint = kv.Value;
                
                var pkt = replicationManager.BuildPacket(pid, world, tickNow, frameEvents);
                if (pkt != null)
                    SendMessageToClient(pkt, endpoint);
            }
        }
    }

    private bool IsReliableEvent(IGameEvent ev)
    {
        // Define what is reliable
        return ev.Type == GameEventType.EntityDespawn || 
               ev.Type == GameEventType.ProjectileSpawn ||
               ev.Type == GameEventType.ProjectileDespawn; 
               // Dash might be reliable too depending on design, but visual-only dashes can be unreliable
    }

    private void OnReceiveMessage(IPEndPoint remote, object msg)
    {
        if (msg is JoinRequestMessage jr)
            HandleJoinRequest(jr, remote);
        else if (msg is InputMessage im)
            HandleInput(im, remote);
    }

    private void HandleInput(InputMessage im, IPEndPoint remote)
    {
        if (!gameStarted || world == null) return;

        if (!connections.TryGetPlayerId(remote, out int pid))
        {
            Debug.LogWarning($"[Server] Input from unknown endpoint {remote}");
            return;
        }

        // Process ACK
        replicationManager.ProcessAck(pid, im.lastReceivedTick);

        if (im.kind == InputKind.RightClick)
        {
            world.HandleMove(pid, im.targetX, im.targetY);
            return;
        }
        var key = ServerGame.Systems.AbilitySystem.KeyFromInputKind(im.kind);
        if (key != null)
        {
            world.EnsurePlayer(pid);
            world.TryCastAbility(pid, key, im.targetX, im.targetY);
        }
    }

    private void SendMessageToClient(object msg, IPEndPoint remote)
    {
        try { transport.Send(remote, msg); }
        catch (Exception e) { Debug.LogError("Server send failed: " + e); }
    }

    private void HandleJoinRequest(JoinRequestMessage jr, IPEndPoint remote)
    {
        if (gameStarted)
        {
            Debug.LogWarning($"[Server] Rejecting join from {remote} because game already started.");
            return;
        }

        int assigned = connections.EnsurePlayer(remote, jr, world); 
        string heroId = connections.GetHeroId(assigned);
        Debug.Log($"Assigned playerId {assigned} to {remote} with name '{jr.playerName}' hero '{heroId}'");
        
        // Register with replication manager
        if (replicationManager != null) replicationManager.RegisterClient(assigned);
        
        SendJoinResponse(assigned, remote, heroId);
    }

    private void SendJoinResponse(int assignedId, IPEndPoint remote, string heroId)
    {
        var resp = new JoinResponseMessage { assignedPlayerId = assignedId, serverTick = Environment.TickCount, heroId = heroId };
        try { transport.Send(remote, resp); }
        catch (Exception e) { Debug.LogError("Failed to send JoinResponse: " + e); }
    }
}







