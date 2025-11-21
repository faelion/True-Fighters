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

    void Start()
    {
        ClientContent.ContentAssetRegistry.EnsureLoaded();
        world = new ServerWorld();
        simulation = new ServerGame.Systems.SimulationRunner(world);
        connections = new ServerGame.ConnectionRegistry();
        snapshotBuilder = new ServerGame.ServerSnapshotBuilder();
        transport = new UdpTransport();
        transport.Start(new IPEndPoint(IPAddress.Loopback, listenPort));
        transport.OnReceive += OnReceiveMessage;
    }

    void OnDestroy()
    {
        transport?.Stop();
        transport?.Dispose();
    }

    void Update()
    {
        transport?.Update();
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

    private void OnReceiveMessage(IPEndPoint remote, object msg)
    {
        if (msg is JoinRequestMessage jr)
            HandleJoinRequest(jr, remote);
        else if (msg is InputMessage im)
            HandleInput(im, remote);
    }

    private void HandleInput(InputMessage im, IPEndPoint remote)
    {
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
        int assigned = connections.EnsurePlayer(remote, jr, world);
        string heroId = connections.GetHeroId(assigned);
        Debug.Log($"Assigned playerId {assigned} to {remote} with name '{jr.playerName}' hero '{heroId}'");
        SendJoinResponse(assigned, remote, heroId);
        // Initial states will arrive in the next TickPacket
    }

    private void SendJoinResponse(int assignedId, IPEndPoint remote, string heroId)
    {
        var resp = new JoinResponseMessage { assignedPlayerId = assignedId, serverTick = Environment.TickCount, heroId = heroId };
        try { transport.Send(remote, resp); }
        catch (Exception e) { Debug.LogError("Failed to send JoinResponse: " + e); }
    }
}







