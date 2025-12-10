using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Buffers;
using Networking.Transport;
using ServerGame;
using Shared;

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

    public ServerGame.ConnectionRegistry Connections => connections;
    public event Action<IPEndPoint, object> OnClientMessage;
    public event Action<int> OnPlayerJoined;

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
        foreach (var ep in connections.PlayerEndpoints.Values)
        {
            SendMessageToClient(startMsg, ep);
        }

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
        simulation = new ServerGame.Systems.SimulationRunner(world);

        foreach (var kv in connections.PlayerEndpoints)
        {
            int pid = kv.Key;
            string heroId = connections.GetHeroId(pid);
            int team = connections.GetTeam(pid);

            replicationManager.RegisterClient(pid);

            var entity = world.EnsurePlayer(pid, connections.GetPlayerName(pid), heroId, team);
            
            world.SetTeam(pid, team);
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
        return ev.IsReliable;
    }

    private void OnReceiveMessage(IPEndPoint remote, object msg)
    {
        OnClientMessage?.Invoke(remote, msg);

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

        replicationManager.ProcessAck(pid, im.lastReceivedTick);

        if (im.kind == InputKind.RightClick)
        {
            world.HandleMove(pid, im.targetX, im.targetY);
            return;
        }
        var key = ServerGame.Systems.AbilitySystem.KeyFromInputKind(im.kind);
        if (key != null)
        {
            world.EnsurePlayer(pid, null, null, connections.GetTeam(pid));
            world.TryCastAbility(pid, key, im.targetX, im.targetY);
        }
    }

    public void SendToAll(object msg)
    {
        foreach (var ep in connections.PlayerEndpoints.Values)
        {
            SendMessageToClient(msg, ep);
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
        
        if (replicationManager != null) replicationManager.RegisterClient(assigned);
        
        SendJoinResponse(assigned, remote, heroId);
        
        OnPlayerJoined?.Invoke(assigned);
    }

    private void SendJoinResponse(int assignedId, IPEndPoint remote, string heroId)
    {
        var resp = new JoinResponseMessage { assignedPlayerId = assignedId, serverTick = Environment.TickCount, heroId = heroId };
        try { transport.Send(remote, resp); }
        catch (Exception e) { Debug.LogError("Failed to send JoinResponse: " + e); }
    }
}
