using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
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

    void Start()
    {
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
        var states = BuildStateMessages(tickNow);
        var abilityEvents = BuildAbilityEvents(tickNow);
        var pkt = new TickPacketMessage { serverTick = tickNow, states = states.ToArray(), abilityEvents = abilityEvents.ToArray() };
        foreach (var endpoint in endpointToPlayerId.Keys)
            SendMessageToClient(pkt, endpoint);
    }

    private void HandleInput(InputMessage im, IPEndPoint remote)
    {
        if (!endpointToPlayerId.TryGetValue(remote, out int pid))
        {
            Debug.LogWarning($"[Server] Input from unknown endpoint {remote}");
            return;
        }

        switch (im.kind)
        {
            case InputKind.RightClick:
                world.HandleMove(pid, im.targetX, im.targetY);
                break;
            case InputKind.Q:
            case InputKind.W:
            case InputKind.E:
            case InputKind.R:
                world.EnsurePlayer(pid);
                var key = im.kind == InputKind.Q ? "Q" : im.kind == InputKind.W ? "W" : im.kind == InputKind.E ? "E" : "R";
                world.TryCastAbility(pid, key, im.targetX, im.targetY);
                break;
            default:
                break;
        }
    }

    private List<StateMessage> BuildStateMessages(int tick)
    {
        var list = new List<StateMessage>(world.Players.Count + 1);
        foreach (var p in world.Players.Values)
        {
            list.Add(new StateMessage
            {
                playerId = p.playerId,
                hit = p.hit,
                posX = p.posX,
                posY = p.posY,
                rotZ = p.rotZ,
                tick = tick
            });
        }
        list.Add(new StateMessage { playerId = world.Npc.id, posX = world.Npc.posX, posY = world.Npc.posY, rotZ = 0, tick = tick });
        return list;
    }

    private List<AbilityEventMessage> BuildAbilityEvents(int tick)
    {
        var list = new List<AbilityEventMessage>();

        var instant = world.ConsumePendingAbilityEvents();
        if (instant != null)
        {
            foreach (var e in instant)
            {
                e.serverTick = tick;
                list.Add(e);
            }
        }

        var spawned = world.ConsumeRecentlySpawnedEffects();
        if (spawned != null)
        {
            foreach (var id in spawned)
            {
                if (!world.AbilityEffects.TryGetValue(id, out var eff)) continue;
                var spawn = new AbilityEventMessage { casterId = eff.ownerPlayerId, serverTick = tick };
                if (eff.type == AbilityEffectType.Projectile)
                {
                    spawn.eventType = AbilityEventType.SpawnProjectile;
                    spawn.abilityIdOrKey = eff.abilityId;
                    spawn.projectileId = eff.id;
                    spawn.posX = eff.posX; spawn.posY = eff.posY;
                    spawn.dirX = eff.dirX; spawn.dirY = eff.dirY;
                    spawn.speed = eff.speed; spawn.lifeMs = eff.lifeMs;
                }
                else if (eff.type == AbilityEffectType.Area)
                {
                    spawn.eventType = AbilityEventType.SpawnArea;
                    spawn.abilityIdOrKey = eff.abilityId;
                    spawn.posX = eff.posX; spawn.posY = eff.posY;
                    spawn.lifeMs = eff.lifeMs;
                }
                list.Add(spawn);
            }
        }

        foreach (var eff in world.AbilityEffects.Values)
        {
            if (eff.type == AbilityEffectType.Projectile)
            {
                list.Add(new AbilityEventMessage
                {
                    abilityIdOrKey = eff.abilityId,
                    casterId = eff.ownerPlayerId,
                    eventType = AbilityEventType.ProjectileUpdate,
                    projectileId = eff.id,
                    posX = eff.posX,
                    posY = eff.posY,
                    dirX = eff.dirX,
                    dirY = eff.dirY,
                    speed = eff.speed,
                    lifeMs = eff.lifeMs,
                    serverTick = tick
                });
            }
        }

        var despawned = world.ConsumeRecentlyDespawnedEffects();
        if (despawned != null)
        {
            foreach (var id in despawned)
            {
                list.Add(new AbilityEventMessage
                {
                    abilityIdOrKey = "",
                    casterId = 0,
                    eventType = AbilityEventType.ProjectileDespawn,
                    projectileId = id,
                    serverTick = tick
                });
            }
        }

        return list;
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







