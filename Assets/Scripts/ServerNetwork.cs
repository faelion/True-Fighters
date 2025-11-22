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
    private ServerGame.ServerSnapshotBuilder snapshotBuilder;
    private readonly System.Collections.Generic.List<StateMessage> stateBuffer = new System.Collections.Generic.List<StateMessage>(32);
    private readonly System.Collections.Generic.List<IGameEvent> eventBuffer = new System.Collections.Generic.List<IGameEvent>(64);

    private bool gameStarted = false;
    private string currentSceneName;

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
        snapshotBuilder = new ServerGame.ServerSnapshotBuilder();
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
            stateBuffer.Clear();
            eventBuffer.Clear();
            snapshotBuilder.BuildSnapshot(world, tickNow, stateBuffer, eventBuffer);

            int sc = stateBuffer.Count;
            int ec = eventBuffer.Count;
            var statesArr = ArrayPool<StateMessage>.Shared.Rent(sc);
            var eventsArr = ArrayPool<IGameEvent>.Shared.Rent(ec);
            stateBuffer.CopyTo(0, statesArr, 0, sc);
            eventBuffer.CopyTo(0, eventsArr, 0, ec);
            var pkt = new TickPacketMessage { serverTick = tickNow, states = statesArr, events = eventsArr, statesCount = sc, eventsCount = ec };
            foreach (var endpoint in connections.PlayerEndpoints.Values)
                SendMessageToClient(pkt, endpoint);
            pkt.states = null; pkt.events = null;
            ArrayPool<StateMessage>.Shared.Return(statesArr, clearArray: true);
            ArrayPool<IGameEvent>.Shared.Return(eventsArr, clearArray: true);
        }
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

        if (im.kind == InputKind.RightClick)
        {
            world.HandleMove(pid, im.targetX, im.targetY);
            return;
        }
        var key = ServerGame.Systems.AbilitySystem.KeyFromInputKind(im.kind);
        if (key != null)
        {
            //Debug.Log($"[Server] Player {pid} casts ability '{key}' at ({im.targetX}, {im.targetY})");
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

        int assigned = connections.EnsurePlayer(remote, jr, world); // world is null here initially, need to fix ConnectionRegistry or pass null
        string heroId = connections.GetHeroId(assigned);
        Debug.Log($"Assigned playerId {assigned} to {remote} with name '{jr.playerName}' hero '{heroId}'");
        SendJoinResponse(assigned, remote, heroId);
    }

    private void SendJoinResponse(int assignedId, IPEndPoint remote, string heroId)
    {
        var resp = new JoinResponseMessage { assignedPlayerId = assignedId, serverTick = Environment.TickCount, heroId = heroId };
        try { transport.Send(remote, resp); }
        catch (Exception e) { Debug.LogError("Failed to send JoinResponse: " + e); }
    }
}







