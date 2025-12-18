using UnityEngine;
using System.Collections.Generic;
using Shared;

public class LobbyManager : MonoBehaviour
{
    public ClientNetwork clientNetwork;
    public LobbyUI lobbyUI;

    // Local State
    private int myPlayerId;
    private LobbyStateData lastData;

    void Awake()
    {
        if (lobbyUI != null) lobbyUI.Init(this);
    }

    void Start()
    {
        if (clientNetwork == null)
            clientNetwork = FindFirstObjectByType<ClientNetwork>();

        if (clientNetwork != null)
        {
            clientNetwork.OnLobbyUpdate += HandleLobbyUpdate;
            myPlayerId = clientNetwork.AssignedPlayerId;
        }
    }

    private ServerLobbyManager serverLobbyManager;
    public bool IsHost => serverLobbyManager != null;

    public void SetServer(ServerLobbyManager lobbyMgr)
    {
        serverLobbyManager = lobbyMgr;
        if (serverLobbyManager != null)
        {
            serverLobbyManager.OnLobbyStateUpdated += HandleLobbyUpdate;
            myPlayerId = -1; // Server view
            if (lobbyUI) lobbyUI.RefreshHostState();
        }
    }

    public void SetNetwork(ClientNetwork net)
    {
        if (clientNetwork != null)
            clientNetwork.OnLobbyUpdate -= HandleLobbyUpdate;

        clientNetwork = net;
        if (clientNetwork != null)
        {
            clientNetwork.OnLobbyUpdate += HandleLobbyUpdate;
            if (clientNetwork.HasAssignedId)
                myPlayerId = clientNetwork.AssignedPlayerId;
            
            // Check if we missed the initial update
            if (clientNetwork.LastLobbyState.Players != null)
            {
                HandleLobbyUpdate(clientNetwork.LastLobbyState);
            }
        }
    }

    void OnDestroy()
    {
        if (clientNetwork != null)
            clientNetwork.OnLobbyUpdate -= HandleLobbyUpdate;
    }

    private bool lobbyShown = false;
    private void HandleLobbyUpdate(LobbyStateData data)
    {
        Debug.Log("[LobbyManager] Handling Lobby Update...");
        if (!lobbyShown)
        {
            var launcherUI = FindFirstObjectByType<GameLauncherUI>();
            Debug.Log($"[LobbyManager] Found LauncherUI? {launcherUI != null}");
            if (launcherUI != null) launcherUI.ShowLobby();
            lobbyShown = true;
        }

        lastData = data;
        if (clientNetwork != null && myPlayerId == 0) 
            myPlayerId = clientNetwork.AssignedPlayerId;

        lobbyUI.UpdateLobby(data, myPlayerId);
    }

    // --- Actions called by UI ---

    public void SelectHero(string heroId)
    {
        clientNetwork?.SendLobbyAction(1, heroId);
    }

    public void ToggleReady(bool isReady)
    {
        clientNetwork?.SendLobbyAction(2, isReady ? "1" : "0");
    }

    public void ChangeTeam(int teamId)
    {
        clientNetwork?.SendLobbyAction(3, teamId.ToString());
    }

    public void StartGameRequest(string mapName, string gameMode)
    {
        // Only Host calls this, but it goes via ServerNetwork directly on the host machine usually?
        // Or we can add a network message for "StartGame" if we wanted remote host.
        // For now, assuming Host uses the local ServerNetwork reference.
        var server = FindFirstObjectByType<ServerNetwork>();
        if (server != null)
        {
            server.StartGame(mapName, gameMode);
        }
    }
}
