using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Networking.Transport;
using ServerGame;
using ServerGame.Content;

public class ServerNetwork : MonoBehaviour
{
    public int listenPort = 9050;

    private INetworkTransport transport;

    private ConcurrentQueue<(object msg, IPEndPoint remote)> incoming = new ConcurrentQueue<(object, IPEndPoint)>();

    private ServerWorld world;

    private Dictionary<IPEndPoint, int> endpointToPlayerId = new Dictionary<IPEndPoint, int>();
    private Dictionary<int, IPEndPoint> playerIdToEndpoint = new Dictionary<int, IPEndPoint>();
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
            else if (msg is AbilityRequestMessage ar)
            {
                HandleAbilityRequest(ar, remote);
            }

        }

        float dt = Time.deltaTime;
        world.Simulate(dt);

        // Collect and broadcast messages in a single pass
        int tickNow = Environment.TickCount;
        var states = BuildStateMessages(tickNow);
        var abilityEvents = BuildAbilityEvents(tickNow);

        var pkt = new TickPacketMessage
        {
            serverTick = tickNow,
            states = states.ToArray(),
            abilityEvents = abilityEvents.ToArray()
        };
        foreach (var endpoint in endpointToPlayerId.Keys)
        {
            SendMessageToClient(pkt, endpoint);
        }

    }

    private void HandleInput(InputMessage im, IPEndPoint remote)
    {
        if (!endpointToPlayerId.TryGetValue(remote, out int pid))
        {
            Debug.LogWarning($"[Server] Input from unknown endpoint {remote}");
            return;
        }

        if (im.isMove)
        {
            world.HandleMove(pid, im.targetX, im.targetY);
        }
        // abilities arrive via AbilityRequestMessage only
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
        list.Add(new StateMessage
        {
            playerId = world.Npc.id,
            posX = world.Npc.posX,
            posY = world.Npc.posY,
            rotZ = 0,
            tick = tick
        });
        return list;
    }

    private List<AbilityEventMessage> BuildAbilityEvents(int tick)
    {
        var list = new List<AbilityEventMessage>();

        // 0) Instant ability events enqueued by systems this tick
        var instant = world.ConsumePendingAbilityEvents();
        if (instant != null && instant.Count > 0)
        {
            foreach (var e in instant)
            {
                // normalize tick
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
                var spawn = new AbilityEventMessage
                {
                    abilityIdOrKey = "",
                    casterId = eff.ownerPlayerId,
                    serverTick = tick
                };
                if (eff.type == AbilityEffectType.Projectile)
                {
                    spawn.eventType = AbilityEventType.SpawnProjectile;
                    spawn.projectileId = eff.id;
                    spawn.posX = eff.posX;
                    spawn.posY = eff.posY;
                    spawn.dirX = eff.dirX;
                    spawn.dirY = eff.dirY;
                    spawn.speed = eff.speed;
                    spawn.lifeMs = eff.lifeMs;
                    spawn.abilityIdOrKey = eff.abilityId;
                }
                else if (eff.type == AbilityEffectType.Area)
                {
                    spawn.eventType = AbilityEventType.SpawnArea;
                    spawn.posX = eff.posX;
                    spawn.posY = eff.posY;
                    spawn.lifeMs = eff.lifeMs;
                    spawn.abilityIdOrKey = eff.abilityId;
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
                    abilityIdOrKey = "",
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

    private void HandleAbilityRequest(AbilityRequestMessage ar, IPEndPoint remote)
    {
        if (!endpointToPlayerId.TryGetValue(remote, out int pid))
        {
            Debug.LogWarning($"[Server] AbilityRequest from unknown endpoint {remote}");
            return;
        }

        world.TryCastAbility(pid, ar.abilityIdOrKey, ar.targetX, ar.targetY);
    }

    private void SendMessageToClient(object msg, IPEndPoint remote)
    {
        try
        {
            transport.Send(remote, msg);
        }
        catch (Exception e)
        {
            Debug.LogError("Server send failed: " + e);
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

        world.EnsurePlayer(assigned, jr.playerName);

        Debug.Log($"Assigned playerId {assigned} to {remote} with name '{jr.playerName}'");

        SendJoinResponse(assigned, remote);

        // Initial states will arrive in the next TickPacket
    }

    private void SendJoinResponse(int assignedId, IPEndPoint remote)
    {
        var resp = new JoinResponseMessage() { assignedPlayerId = assignedId, serverTick = Environment.TickCount };
        try
        {
            transport.Send(remote, resp);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to send JoinResponse: " + e);
        }
    }


}
