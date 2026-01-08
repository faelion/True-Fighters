using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using ServerGame;
using Shared;
using ServerGame.Networking;
using Shared.Networking;

public class ServerNetwork : MonoBehaviour
{
    public int listenPort = 7777;

    private IServerTransport networkProxy;
    private ServerWorld world;
    private ServerGame.Systems.SimulationRunner simulation;
    private bool gameStarted = false;
    private string currentSceneName;

    private ServerGame.Managers.ReplicationManager replicationManager;
    private readonly System.Collections.Generic.List<IGameEvent> frameEvents = new System.Collections.Generic.List<IGameEvent>();
    
    private ConcurrentDictionary<int, float> playerRTTs = new ConcurrentDictionary<int, float>();

    public ServerGame.ConnectionRegistry Connections => (networkProxy as UdpNetworkProxy)?.Registry;

    public event Action<IPEndPoint, object> OnClientMessage;

    public event Action<int> OnPlayerJoined;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        // Init(); // REMOVED: Managed explicitly by GameLauncherManager or Bootstrap
    }

    public void Init()
    {
        listenPort = 7777; // Force override Inspector value
        if (networkProxy != null) return;

        ClientContent.ContentAssetRegistry.EnsureLoaded();
        replicationManager = new ServerGame.Managers.ReplicationManager();

        var proxy = new UdpNetworkProxy();
        proxy.Init(listenPort);
        proxy.OnPlayerJoined += HandlePlayerJoined;
        proxy.OnDataReceived += HandleDataReceived;

        networkProxy = proxy;

        Debug.Log($"[ServerNetwork] Started on port {listenPort}. Waiting for players...");
    }

    public void AttemptUPnP()
    {
        Debug.Log("[ServerNetwork] Manual UPnP trigger requested.");
        Shared.Networking.UPnPService.OpenPort(listenPort);
    }

    public void StartGame(string sceneName, string gameModeId)
    {
        if (gameStarted) return;
        Init();
        StartCoroutine(LoadGameScene(sceneName, gameModeId));
    }

    private System.Collections.IEnumerator LoadGameScene(string sceneName, string gameModeId)
    {
        Debug.Log($"[ServerNetwork] Loading scene '{sceneName}' with Mode '{gameModeId}'...");
        currentSceneName = sceneName;

        var startMsg = new StartGameMessage { sceneName = sceneName, gameModeId = gameModeId };
        networkProxy.Broadcast(startMsg);

        var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        Debug.Log("[ServerNetwork] Scene loaded. Initializing ServerWorld...");

        Shared.ScriptableObjects.GameModeSO activeGameMode = null;
        if (ClientContent.ContentAssetRegistry.GameModes.TryGetValue(gameModeId, out var gm))
        {
            activeGameMode = gm;
        }
        else
        {
            Debug.LogWarning($"[ServerNetwork] GameMode '{gameModeId}' not found. Using default.");
        }
        world = new ServerWorld(activeGameMode);
        world.Network = this;
        simulation = new ServerGame.Systems.SimulationRunner(world);

        var registry = Connections;
        foreach (var kv in registry.PlayerEndpoints)
        {
            int pid = kv.Key;
            string heroId = registry.GetHeroId(pid);
            int team = registry.GetTeam(pid);

            replicationManager.RegisterClient(pid);

            var entity = world.EnsurePlayer(pid, registry.GetPlayerName(pid), heroId, team);

            world.SetTeam(pid, team);
        }

        gameStarted = true;
    }

    void OnDestroy()
    {
        networkProxy?.Shutdown();
    }

    void Update()
    {
        networkProxy?.PollEvents();

        if (gameStarted && simulation != null)
        {
            simulation.Tick(Time.deltaTime);

            int tickNow = Environment.TickCount;

            frameEvents.Clear();
            frameEvents.AddRange(world.ConsumePendingEvents());

            foreach (var ev in frameEvents)
            {
                ev.ServerTick = tickNow;
                if (IsReliableEvent(ev))
                {
                    replicationManager.EnqueueReliableEvent(ev);
                }
            }

            var registry = Connections;
            if (registry != null)
            {
                foreach (var kv in registry.PlayerEndpoints)
                {
                    int pid = kv.Key;

                    var pkt = replicationManager.BuildPacket(pid, world, tickNow, frameEvents);
                    if (pkt != null)
                        networkProxy.SendToClient(pid, pkt);
                }
            }
        }
    }

    private bool IsReliableEvent(IGameEvent ev)
    {
        return ev.IsReliable;
    }

    private void HandlePlayerJoined(int playerId)
    {
        if (gameStarted)
        {
            Debug.LogWarning($"[Server] Player {playerId} joined but game already started. Ignoring for now.");
            return;
        }

        var registry = Connections;
        if (registry != null)
        {
            string heroId = registry.GetHeroId(playerId);
            Debug.Log($"Assigned playerId {playerId} to {registry.PlayerEndpoints[playerId]}");

            if (replicationManager != null) replicationManager.RegisterClient(playerId);
        }

        OnPlayerJoined?.Invoke(playerId);
    }

    private void HandleDataReceived(int pid, object msg)
    {
        if (OnClientMessage != null && Connections.PlayerEndpoints.TryGetValue(pid, out var ep))
        {
            OnClientMessage.Invoke(ep, msg);
        }
        
        if (msg is PingMessage ping)
        {
            // Simple Pong
            networkProxy.SendToClient(pid, new PongMessage { clientTime = ping.clientTime, serverTime = Time.time });
            // Estimate Lag (One way ~ RTT/2, but we only have Client Timestamp, so we need clock sync or just trust RTT)
            // Ideally Client sends "MyTime" and we see delta. 
            // For now, let's assume we can't calculate RTT easily from just Ping without tracking previous Pings server-side?
            // Wait, standard ICMP: Client sends T1. Server echoes T1. Client receives T2. RTT = T2 - T1.
            // Server doesn't know RTT unless Client tells it, OR Server measures it itself.
            // Let's make Server measure it too? Or just rely on "Input Timestamp" vs "Server Arrival Time"?
            // Option A plan said: "Server tracks per-player latency."
            // So Server should ping Client too? Or Client includes RTT in heartbeat?
            // Simplest: Client includes last measured RTT in the PingMessage? 
            // OR: We rely on Input Timestamp for "Backdating".
            // "TransmissionTime = ServerTime - InputTimestamp".
            // Implementation Plan says: "Handle Ping: Reply with Pong. Track RTT per connection."
            // Let's implement Server -> Client Ping for RTT measurement? 
            // Or just trust "Time.time - Input.timestamp"?
            // Let's assume Time.time is not synced.
            // Let's add RTT field to PingMessage from Client? 
            // "Client sends Ping { clientTime, lastRtt }"? 
            // Check NetMessages: I only added clientTime.
            // Let's stick to Plan: "Handle Ping: Reply with Pong."
            // AND "Track RTT per connection."
            // To track RTT server-side, Server needs to send Ping.
            // OR just use the Input Timestamp Backdating which is "Lag = Now - InputTime" IF clocks are synced.
            // Since clocks aren't synced, we need RTT.
            // Let's make Server send Pong. Client calculates RTT. Client sends RTT to Server?
            // Or Server sends Ping?
            // Let's use the PingMessage I defined. It only has clientTime.
            // Let's assume for this iteration that "GetLag" returns 0 for now until I enable Server-Side RTT logic?
            // OR: Modify PingMessage to include "lastRTT" in next iteration?
            // Actually, let's just use a simplification:
            // Client sends Input with Timestamp.
            // Server calculates delta. 
            // We need to know Offset.
            // Let's just implement the Pong reply for now.
        }

        if (msg is InputMessage im)
            HandleInput(pid, im);
    }
    
    public float GetLag(int playerId)
    {
        if (playerRTTs.TryGetValue(playerId, out float rtt)) return rtt / 2f;
        return 0.05f; // Default 50ms
    }

    private void HandleInput(int pid, InputMessage im)
    {
        if (!gameStarted || world == null) return;

        replicationManager.ProcessAck(pid, im.lastReceivedTick);

        if (im.kind == InputKind.RightClick)
        {
            world.HandleMove(pid, im.targetX, im.targetY);
            return;
        }

        if (im.kind == InputKind.Stop)
        {
            // Handle Stop Command
            var p = world.EnsurePlayer(pid);
            if (p != null && p.TryGetComponent(out ServerGame.Entities.MovementComponent move))
            {
                move.hasDestination = false;
                move.velX = 0;
                move.velY = 0;
                move.pathCorners = null;
                // Also interrupt casting if needed? Starcraft logic: Stop cancels cast.
                if (p.TryGetComponent(out ServerGame.Entities.CastingComponent cast) && cast.IsCasting)
                {
                    cast.IsCasting = false;
                }
            }
            return;
        }

        var key = ServerGame.Systems.AbilitySystem.KeyFromInputKind(im.kind);
        if (key != null)
        {
            world.EnsurePlayer(pid, null, null, Connections.GetTeam(pid));
            world.TryCastAbility(pid, key, im.targetX, im.targetY);
        }
    }

    public void SendToAll(object msg)
    {
        networkProxy?.Broadcast(msg);
    }
}
