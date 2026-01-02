using System;
using System.Net;
using Networking.Transport;
using Shared;
using Shared.Networking;
using UnityEngine;
using ServerGame;

namespace ServerGame.Networking
{
    public class UdpNetworkProxy : IServerTransport
    {
        private ConnectionRegistry connections;
        private UdpTransport transport;
        
        public event Action<int> OnPlayerJoined;
        public event Action<int> OnPlayerLeft;
        public event Action<int, object> OnDataReceived;

        public ConnectionRegistry Registry => connections;

        public void Init(int port)
        {
            connections = new ConnectionRegistry();
            transport = new UdpTransport();
            transport.Start(new IPEndPoint(IPAddress.Any, port));
            transport.OnReceive += OnLowLevelReceive;
            Debug.Log($"[UdpNetworkProxy] Listening on port {port}");
        }

        public void Shutdown()
        {
            if (transport != null)
            {
                transport.Stop();
                transport.Dispose();
                transport = null;
            }
        }

        public void PollEvents()
        {
            transport?.Update();
        }

        public void SendToClient(int playerId, object message)
        {
            if (connections.PlayerEndpoints.TryGetValue(playerId, out var endpoint))
            {
                SendInternal(endpoint, message);
            }
        }

        public void Broadcast(object message)
        {
            foreach (var endpoint in connections.PlayerEndpoints.Values)
            {
                SendInternal(endpoint, message);
            }
        }

        private void SendInternal(IPEndPoint endpoint, object message)
        {
            try
            {
                transport.Send(endpoint, message);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UdpNetworkProxy] Send error to {endpoint}: {e.Message}");
            }
        }

        private void OnLowLevelReceive(IPEndPoint remote, object msg)
        {
            if (msg is JoinRequestMessage jr)
            {
                int assignedId = connections.EnsurePlayer(remote, jr, null);

                string heroId = connections.GetHeroId(assignedId);
                var resp = new JoinResponseMessage 
                { 
                    assignedPlayerId = assignedId, 
                    serverTick = Environment.TickCount, 
                    heroId = heroId 
                };
                SendInternal(remote, resp);

                OnPlayerJoined?.Invoke(assignedId);
                return;
            }

            if (connections.TryGetPlayerId(remote, out int pid))
            {
                OnDataReceived?.Invoke(pid, msg);
            }
            else
            {
                Debug.LogWarning($"[UdpNetworkProxy] Received content from unknown source: {remote}");
            }
        }
    }
}
