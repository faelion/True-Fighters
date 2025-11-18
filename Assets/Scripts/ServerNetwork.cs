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
    private readonly ConcurrentQueue<(object msg, IPEndPoint remote)> incoming = new ConcurrentQueue<(object, IPEndPoint)>();
    private ServerWorld world;

    private readonly Dictionary<IPEndPoint, int> endpointToPlayerId = new Dictionary<IPEndPoint, int>();
    private readonly Dictionary<int, IPEndPoint> playerIdToEndpoint = new Dictionary<int, IPEndPoint>();
    private int nextPlayerId = 1;
    private readonly System.Collections.Generic.List<StateMessage> stateBuffer = new System.Collections.Generic.List<StateMessage>(32);
    private readonly System.Collections.Generic.List<AbilityEventMessage> eventBuffer = new System.Collections.Generic.List<AbilityEventMessage>(64);
    private readonly List<StateMessage> stateObjs = new List<StateMessage>(32);
    private readonly List<AbilityEventMessage> eventObjs = new List<AbilityEventMessage>(64);
    private int eventObjCountUsed = 0;

    private static string KeyFromInputKind(InputKind kind)
    {
        switch (kind)
        {
            case InputKind.Q: return "Q";
            case InputKind.W: return "W";
            case InputKind.E: return "E";
            case InputKind.R: return "R";
            default: return null;
        }
    }

    void Start()
    {
        ClientContent.AbilityAssetRegistry.EnsureLoaded();
        world = new ServerWorld();
        transport = new UdpTransport();
        transport.Start(new IPEndPoint(IPAddress.Loopback, listenPort));
        transport.OnReceive += (remote, msg) => incoming.Enqueue((msg, remote));
    }

    void OnDestroy()
    {
        transport?.Stop();
        transport?.Dispose();
    }

    void Update()
    {
        while (incoming.TryDequeue(out var tup))
        {
            var msg = tup.msg;
            var remote = tup.remote;
            if (msg is JoinRequestMessage jr)
                HandleJoinRequest(jr, remote);
            else if (msg is InputMessage im)
                HandleInput(im, remote);
        }

        world.Simulate(Time.deltaTime);

        int tickNow = Environment.TickCount;
        stateBuffer.Clear();
        eventBuffer.Clear();
        eventObjCountUsed = 0;
        BuildStateMessages(tickNow);
        BuildAbilityEvents(tickNow);

        int sc = stateBuffer.Count;
        int ec = eventBuffer.Count;
        var statesArr = ArrayPool<StateMessage>.Shared.Rent(sc);
        var eventsArr = ArrayPool<AbilityEventMessage>.Shared.Rent(ec);
        stateBuffer.CopyTo(0, statesArr, 0, sc);
        eventBuffer.CopyTo(0, eventsArr, 0, ec);
        var pkt = new TickPacketMessage { serverTick = tickNow, states = statesArr, abilityEvents = eventsArr, statesCount = sc, eventsCount = ec };
        foreach (var endpoint in endpointToPlayerId.Keys)
            SendMessageToClient(pkt, endpoint);
        pkt.states = null; pkt.abilityEvents = null;
        ArrayPool<StateMessage>.Shared.Return(statesArr, clearArray: true);
        ArrayPool<AbilityEventMessage>.Shared.Return(eventsArr, clearArray: true);
    }

    private void HandleInput(InputMessage im, IPEndPoint remote)
    {
        if (!endpointToPlayerId.TryGetValue(remote, out int pid))
        {
            Debug.LogWarning($"[Server] Input from unknown endpoint {remote}");
            return;
        }

        if (im.kind == InputKind.RightClick)
        {
            world.HandleMove(pid, im.targetX, im.targetY);
            return;
        }
        var key = KeyFromInputKind(im.kind);
        if (key != null)
        {
            //Debug.Log($"[Server] Player {pid} casts ability '{key}' at ({im.targetX}, {im.targetY})");
            world.EnsurePlayer(pid);
            world.TryCastAbility(pid, key, im.targetX, im.targetY);
        }
    }

    private void BuildStateMessages(int tick)
    {
        int countNeeded = world.Players.Count + 1; // players + npc
        while (stateObjs.Count < countNeeded) stateObjs.Add(new StateMessage());

        int i = 0;
        foreach (var p in world.Players.Values)
        {
            var sm = stateObjs[i++];
            sm.playerId = p.playerId;
            sm.hit = p.hit;
            sm.posX = p.posX;
            sm.posY = p.posY;
            sm.rotZ = p.rotZ;
            sm.tick = tick;
            stateBuffer.Add(sm);
        }
        var npc = stateObjs[i++];
        npc.playerId = world.Npc.id;
        npc.hit = false;
        npc.posX = world.Npc.posX;
        npc.posY = world.Npc.posY;
        npc.rotZ = 0f;
        npc.tick = tick;
        stateBuffer.Add(npc);
    }

    private void BuildAbilityEvents(int tick)
    {
        var instant = world.ConsumePendingAbilityEvents();
        if (instant != null)
        {
            foreach (var e in instant)
            {
                var m = GetEventObj();
                m.abilityIdOrKey = e.abilityIdOrKey;
                m.casterId = e.casterId;
                m.eventType = e.eventType;
                m.castTime = e.castTime;
                m.serverTick = tick;
                m.projectileId = e.projectileId;
                m.posX = e.posX; m.posY = e.posY;
                m.dirX = e.dirX; m.dirY = e.dirY;
                m.speed = e.speed; m.lifeMs = e.lifeMs;
                m.value = e.value;
                eventBuffer.Add(m);
            }
        }

        var spawned = world.ConsumeRecentlySpawnedEffects();
        if (spawned != null)
        {
            foreach (var id in spawned)
            {
                if (!world.AbilityEffects.TryGetValue(id, out var eff)) continue;
                if (!ClientContent.AbilityAssetRegistry.Abilities.TryGetValue(eff.abilityId, out var asset) || asset == null)
                {
                    Debug.LogWarning($"[Server] Missing ability asset for id '{eff.abilityId}' when spawning effect {id}");
                    continue;
                }
                var spawn = GetEventObj();
                if (asset.ServerPopulateSpawnEvent(world, eff, tick, spawn))
                    eventBuffer.Add(spawn);
            }
        }

        foreach (var eff in world.AbilityEffects.Values)
        {
            if (!ClientContent.AbilityAssetRegistry.Abilities.TryGetValue(eff.abilityId, out var asset) || asset == null)
            {
                Debug.LogWarning($"[Server] Missing ability asset for id '{eff.abilityId}' on update of effect {eff.id}");
                continue;
            }
            var upd = GetEventObj();
            if (asset.ServerPopulateUpdateEvent(world, eff, tick, upd))
                eventBuffer.Add(upd);
        }

        var despawned = world.ConsumeRecentlyDespawnedEffects();
        if (despawned != null)
        {
            foreach (var pair in despawned)
            {
                if (!ClientContent.AbilityAssetRegistry.Abilities.TryGetValue(pair.abilityId, out var asset) || asset == null)
                {
                    Debug.LogWarning($"[Server] Missing ability asset for id '{pair.abilityId}' on despawn of effect {pair.id}");
                    continue;
                }
                var desp = GetEventObj();
                if (asset.ServerPopulateDespawnEvent(world, pair.id, tick, desp))
                    eventBuffer.Add(desp);
            }
        }
    }

    private AbilityEventMessage GetEventObj()
    {
        if (eventObjCountUsed >= eventObjs.Count)
            eventObjs.Add(new AbilityEventMessage());
        return eventObjs[eventObjCountUsed++];
    }

    private void SendMessageToClient(object msg, IPEndPoint remote)
    {
        try { transport.Send(remote, msg); }
        catch (Exception e) { Debug.LogError("Server send failed: " + e); }
    }

    private void HandleJoinRequest(JoinRequestMessage jr, IPEndPoint remote)
    {
        if (endpointToPlayerId.TryGetValue(remote, out int existing))
        {
            SendJoinResponse(existing, remote);
            return;
        }

        int assigned = nextPlayerId++;
        endpointToPlayerId[remote] = assigned;
        playerIdToEndpoint[assigned] = remote;
        world.EnsurePlayer(assigned, jr.playerName);
        Debug.Log($"Assigned playerId {assigned} to {remote} with name '{jr.playerName}'");
        SendJoinResponse(assigned, remote);
        // Initial states will arrive in the next TickPacket
    }

    private void SendJoinResponse(int assignedId, IPEndPoint remote)
    {
        var resp = new JoinResponseMessage { assignedPlayerId = assignedId, serverTick = Environment.TickCount };
        try { transport.Send(remote, resp); }
        catch (Exception e) { Debug.LogError("Failed to send JoinResponse: " + e); }
    }
}







