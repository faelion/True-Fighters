using UnityEngine;
using Shared;
using System.Net;
using System;

public class ServerLobbyManager : MonoBehaviour
{
    private ServerNetwork serverNetwork;
    private string currentGameModeId = ClientContent.ContentAssetRegistry.DefaultGameModeId;

    public event Action<LobbyStateData> OnLobbyStateUpdated;

    public void Init(ServerNetwork net)
    {
        serverNetwork = net;
        serverNetwork.OnClientMessage += HandleMessage;
        serverNetwork.OnPlayerJoined += HandlePlayerJoined;
        
        // Initial empty state for Server UI
        BroadcastLobbyState();
    }

    public void TryEnableUPnP()
    {
        serverNetwork?.AttemptUPnP();
    }

    void OnDestroy()
    {
        if (serverNetwork != null)
        {
            serverNetwork.OnClientMessage -= HandleMessage;
            serverNetwork.OnPlayerJoined -= HandlePlayerJoined;
        }
    }

    private void HandlePlayerJoined(int playerId)
    {
        BroadcastLobbyState();
    }

    private void HandleMessage(IPEndPoint remote, object msg)
    {
        if (msg is LobbyActionMessage lam)
        {
            if (serverNetwork.Connections.TryGetPlayerId(remote, out int pid))
            {
                HandleLobbyAction(pid, lam);
            }
        }
    }

    private void HandleLobbyAction(int pid, LobbyActionMessage lam)
    {
        var connections = serverNetwork.Connections;
        bool changed = false;

        if (lam.actionType == 1 && !string.IsNullOrEmpty(lam.payload)) // Select Hero
        {
            connections.UpdateHero(pid, lam.payload);
            changed = true;
        }
        else if (lam.actionType == 2) // Toggle Ready
        {
            bool r = lam.payload == "1";
            connections.SetReady(pid, r);
            changed = true;
        }
        else if (lam.actionType == 3) // Change Team
        {
            if (int.TryParse(lam.payload, out int tid))
            {
                connections.SetTeam(pid, tid);
                changed = true;
            }
        }
        else if (lam.actionType == 4) // SetGameMode
        {
            // Only allow if it's a known game mode
            if (ClientContent.ContentAssetRegistry.GameModes.TryGetValue(lam.payload, out var gm))
            {
                currentGameModeId = lam.payload;
                changed = true;

                // Shuffle & Assign Teams
                var allPlayers = new System.Collections.Generic.List<int>();
                foreach (var kvp in connections.PlayerEndpoints) allPlayers.Add(kvp.Key);

                // Fisher-Yates Shuffle
                var rng = new System.Random();
                int n = allPlayers.Count;
                while (n > 1)
                {
                    n--;
                    int k = rng.Next(n + 1);
                    int value = allPlayers[k];
                    allPlayers[k] = allPlayers[n];
                    allPlayers[n] = value;
                }

                if (gm.teams != null && gm.teams.Length > 0)
                {
                    // Distribute Round-Robin
                    for (int i = 0; i < allPlayers.Count; i++)
                    {
                        int teamIndex = i % gm.teams.Length;
                        connections.SetTeam(allPlayers[i], teamIndex + 1); // 1-based teams
                    }
                }
                else
                {
                    // FFA -> Reset to 0
                    foreach (var id in allPlayers)
                    {
                        connections.SetTeam(id, 0);
                    }
                }
            }
        }

        if (changed) BroadcastLobbyState();
    }

    private void BroadcastLobbyState()
    {
        var info = serverNetwork.Connections.GetLobbyInfo();
        var data = new LobbyStateData { Players = info, SelectedGameModeId = currentGameModeId };

        // Local event for Server UI (if any)
        OnLobbyStateUpdated?.Invoke(data);

        // Network Broadcast
        var msg = new LobbyUpdateMessage { data = data };
        serverNetwork.SendToAll(msg);
    }
}
