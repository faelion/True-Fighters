using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Shared;
using ClientContent;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject hostControlsPanel;
    
    [Header("Prefabs")]
    public GameObject heroButtonPrefab;
    public GameObject playerListEntryPrefab;

    [Header("References")]
    public Transform heroGridContent;
    public Transform playerListContent;
    public GameObject previewArea;
    public Image heroPreviewImage;
    public TMP_Text heroPreviewName;
    public TMP_Text heroPreviewDescription;
    public Toggle readyToggle;
    public Button startGameButton;
    public TMP_Dropdown mapDropdown;
    public TMP_Dropdown gameModeDropdown;
    public TMP_Dropdown teamDropdown;
    public Toggle upnpToggle;
    public TMP_Text lobbyInfoText;

    [Header("Debug Logs")]
    public Toggle showLogsToggle;
    public TMP_Text logsText;

    private LobbyManager manager;
        
    public void Init(LobbyManager mgr)
    {
        manager = mgr;
        
        if (readyToggle) readyToggle.onValueChanged.AddListener(OnReadyToggled);
        if (startGameButton) startGameButton.onClick.AddListener(OnStartGameClicked);
        if (gameModeDropdown) gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);
        if (teamDropdown) teamDropdown.onValueChanged.AddListener(OnTeamDropdownChanged);
        
        if (upnpToggle) upnpToggle.onValueChanged.AddListener((val) => manager.ToggleUPnP(val));

        if (showLogsToggle) 
        {
            showLogsToggle.onValueChanged.AddListener(OnShowLogsToggled);
            OnShowLogsToggled(showLogsToggle.isOn); // Init state
        }
        if (logsText) 
        {
             // logsText.text = ""; // Don't clear logs on init
             logsText.raycastTarget = false; // IMPORTANT: Prevent text from blocking clicks on buttons behind it
        }
        
        // Ensure state match
        if (showLogsToggle) OnShowLogsToggled(showLogsToggle.isOn);

        PopulateGameModes();
        
        // RefreshHostState(); // REMOVED: Unsafe to call here if GameObject is disabled (StartCoroutine fails)
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        RefreshHostState(); // Called when panel becomes visible
    }

    private void OnShowLogsToggled(bool show)
    {
        if (logsText) logsText.gameObject.SetActive(show);
    }

    public void RefreshHostState()
    {
        if (manager == null) return;
        if (!gameObject.activeInHierarchy) return; // Prevent Coroutine errors


        // Host controls visibility check
        bool isHost = manager.IsHost || NetworkConfig.playerName == "Server"; 
        
        if (hostControlsPanel) 
        {
            hostControlsPanel.SetActive(isHost);
        }

        // Server does not select heroes
        if (NetworkConfig.playerName != "Server")
        {
            PopulateHeroGrid();
            if (readyToggle) readyToggle.gameObject.SetActive(true);
            if (previewArea) previewArea.SetActive(true);
        }
        else
        {
            if (readyToggle) readyToggle.gameObject.SetActive(false);
            if (previewArea) previewArea.SetActive(false);
            if (heroGridContent) heroGridContent.gameObject.SetActive(false);
            foreach (Transform child in heroGridContent) Destroy(child.gameObject);
        }

        // Info Text
        if (lobbyInfoText)
        {
            if (isHost || NetworkConfig.playerName == "Server")
            {
                if (!hasAttemptedFetch && fetchCoroutine == null) 
                {
                    lobbyInfoText.text = $"Local IP: {GetLocalIPAddress()}\nFetching Public IP...";
                    fetchCoroutine = StartCoroutine(FetchPublicIP());
                }
            }
            else
            {
                if (manager.clientNetwork != null)
                     lobbyInfoText.text = $"Connected to: {manager.clientNetwork.serverHost}:{manager.clientNetwork.serverPort}";
                else
                     lobbyInfoText.text = "Connected";
            }
        }
    }

    private Coroutine fetchCoroutine;
    private bool hasAttemptedFetch = false;

    private System.Collections.IEnumerator FetchPublicIP()
    {
        Debug.Log("[UPnP] Coroutine Started: FetchPublicIP");
        hasAttemptedFetch = true; // Mark strict start

        string localIP = GetLocalIPAddress();
        if (lobbyInfoText) lobbyInfoText.text = $"Local IP: {localIP}\nPublic IP: Contacting Service...";

        string publicIP = null;
        string error = null;

        // Wrap network calls in absolute safety
        yield return null; // Wait one frame to ensure UI updated

        // Service 1
        var req1 = UnityEngine.Networking.UnityWebRequest.Get("https://api.ipify.org");
        req1.timeout = 5;
        yield return req1.SendWebRequest();

        if (req1.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
             publicIP = req1.downloadHandler.text;
        }
        else
        {
             Debug.LogWarning($"[UPnP] Service 1 Failed: {req1.error}");
             // Service 2
             var req2 = UnityEngine.Networking.UnityWebRequest.Get("https://icanhazip.com");
             req2.timeout = 5;
             yield return req2.SendWebRequest();
             if (req2.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
             {
                 publicIP = req2.downloadHandler.text.Trim();
             }
             else
             {
                 error = req2.error;
                 Debug.LogWarning($"[UPnP] Service 2 Failed: {req2.error}");
             }
             req2.Dispose();
        }
        req1.Dispose();

        // Final UI Update
        if (lobbyInfoText)
        {
            if (!string.IsNullOrEmpty(publicIP))
                lobbyInfoText.text = $"Local IP: {localIP}\nPublic IP: {publicIP}";
            else
                lobbyInfoText.text = $"Local IP: {localIP}\nPublic IP: Check Logs"; 
        }

        Debug.Log($"[UPnP] IP Fetch Result: {publicIP ?? error ?? "Unknown"}");
        fetchCoroutine = null;
    }

    private string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    private void PopulateHeroGrid()
    {
        // Clean existing
        foreach (Transform child in heroGridContent) Destroy(child.gameObject);
        
        // Load Registry
        ContentAssetRegistry.EnsureLoaded();

        if (ContentAssetRegistry.Heroes.Count == 0)
        {
            Debug.LogWarning("[LobbyUI] No heroes found in Registry!");
            return;
        }

        foreach (var kvp in ContentAssetRegistry.Heroes)
        {
            var heroId = kvp.Key;
            
            var go = Instantiate(heroButtonPrefab, heroGridContent);
            var btn = go.GetComponent<Button>();
            var txt = go.GetComponentInChildren<TMP_Text>();
            if (txt) txt.text = heroId;

            btn.onClick.AddListener(() => {
                manager.SelectHero(heroId);
                UpdatePreview(heroId);
            });
        }
    }

    private void UpdatePreview(string heroId)
    {
        if (heroPreviewName) heroPreviewName.text = heroId;
        // Load image/model here
    }

    private List<string> availableGameModeIds = new List<string>();

    private void PopulateGameModes()
    {
        if (!gameModeDropdown) return;

        gameModeDropdown.ClearOptions();
        availableGameModeIds.Clear();
        List<string> displayNames = new List<string>();
        
        ContentAssetRegistry.EnsureLoaded();
        Debug.Log($"[LobbyUI] PopulateGameModes - Registry count: {ContentAssetRegistry.GameModes.Count}");
        
        if (ContentAssetRegistry.GameModes.Count > 0)
        {
            foreach(var pair in ContentAssetRegistry.GameModes) 
            {
                Debug.Log($"[LobbyUI] Adding Option: {pair.Value.displayName}");
                 availableGameModeIds.Add(pair.Key);
                 displayNames.Add(pair.Value.displayName);
            }
        }
        else
        {
            availableGameModeIds.Add(ContentAssetRegistry.DefaultGameModeId);
            displayNames.Add("Default");
        }
        gameModeDropdown.AddOptions(displayNames);
        
        // Force valid selection if possible
        if (availableGameModeIds.Count > 0)
        {
            OnGameModeChanged(0);
        }
    }

    private void OnGameModeChanged(int index)
    {
        if (index < 0 || index >= availableGameModeIds.Count) return;
        string id = availableGameModeIds[index];
        RefreshMapsForMode(id);

        // Host notifies server of change
        if (manager.IsHost || NetworkConfig.playerName == "Server")
        {
             manager.clientNetwork?.SendLobbyAction(4, id);
        }
    }

    private void RefreshMapsForMode(string gameModeId)
    {
        if (!mapDropdown) return;
        mapDropdown.ClearOptions();

        if (ContentAssetRegistry.GameModes.TryGetValue(gameModeId, out var gm))
        {
            if (gm.maps != null && gm.maps.Count > 0)
            {
                List<string> mapNames = new List<string>();
                foreach(var m in gm.maps) mapNames.Add(m.SceneName);
                mapDropdown.AddOptions(mapNames);
            }
            else
            {
                mapDropdown.AddOptions(new List<string> { "Arena", "Map1", "SceneReferenceMissing" }); 
            }
        }
    }

    private void OnTeamDropdownChanged(int index)
    {
        // 0 = Spectator/Auto? Or should we map directly to Team IDs defined in GameMode?
        // Let's assume Dropdown Index 0 = "No Team" / "Spectator" if applicable, or Team 1.
        // User said: "dropdown... seleccionar el equipo al que quieres ir".
        // If GameMode has teams: "Red", "Blue". Index 0 -> Red (Team 1), Index 1 -> Blue (Team 2).
        // If GameMode has No Teams: Dropdown should be disabled or "No Teams".
        
        string gmId = manager.clientNetwork?.LastLobbyState.SelectedGameModeId ?? ContentAssetRegistry.DefaultGameModeId;
        if (ContentAssetRegistry.GameModes.TryGetValue(gmId, out var gm))
        {
            if (gm.teams != null && gm.teams.Length > 0)
            {
               int teamId = index + 1; // 1-based team ID usually
               manager.ChangeTeam(teamId);
            }
            else
            {
               // Free for all, maybe team 0?
               manager.ChangeTeam(0);
            }
        }
    }

    private string lastTeamGmId = "";

    private void RefreshTeamDropdown(string gameModeId)
    {
        if (!teamDropdown) return;
        if (lastTeamGmId == gameModeId && teamDropdown.options.Count > 0) return;

        lastTeamGmId = gameModeId;
        teamDropdown.ClearOptions();
        List<string> options = new List<string>();

        if (ContentAssetRegistry.GameModes.TryGetValue(gameModeId, out var gm) && gm.teams != null && gm.teams.Length > 0)
        {
            teamDropdown.interactable = true;
            foreach(var team in gm.teams)
            {
                options.Add(team.teamName);
            }
        }
        else
        {
            options.Add("No Teams");
            teamDropdown.interactable = false;
        }
        teamDropdown.AddOptions(options);
    }

    public void UpdateLobby(LobbyStateData data, int myPlayerId)
    {
        // SYNC UI with Server State
        // GameMode Dropdown (Host checks against logic, Client updates visuals)
        string gmId = string.IsNullOrEmpty(data.SelectedGameModeId) ? ContentAssetRegistry.DefaultGameModeId : data.SelectedGameModeId;
        
        // If I am not the host, I should sync my gameModeDropdown to match the server's selection
        if (manager != null && !(manager.IsHost || NetworkConfig.playerName == "Server"))
        {
            int index = availableGameModeIds.IndexOf(gmId);
            if (index != -1 && gameModeDropdown && gameModeDropdown.value != index)
            {
                gameModeDropdown.SetValueWithoutNotify(index);
                RefreshMapsForMode(gmId); 
            }
            // Also refresh maps if first time
            if (mapDropdown && mapDropdown.options.Count == 0) RefreshMapsForMode(gmId);
        }

        // Always refresh teams if mode changed
        RefreshTeamDropdown(gmId); 
        
        // Sync my team selection with the dropdown
        if (teamDropdown && data.Players != null)
        {
             foreach (var p in data.Players)
             {
                 if (p.ConnectionId == myPlayerId && p.PlayerName != "Server")
                 {
                     // Convert TeamID (1-based or 0?) to Dropdown Index (0-based)
                     // Convert TeamID to Dropdown Index
                     int targetIndex = (p.TeamId > 0) ? p.TeamId - 1 : 0;
                     if (teamDropdown.value != targetIndex)
                     {
                         teamDropdown.SetValueWithoutNotify(targetIndex);
                     }
                     break;
                 }
             }
        }

        // Update Player List
        UpdatePlayerList(data, gmId);
         
        // Host Controls Check
        if (hostControlsPanel)
        {
            hostControlsPanel.SetActive(manager.IsHost || NetworkConfig.playerName == "Server");
        }

        if (startGameButton)
        {
             bool allReady = true;
             if (data.Players != null)
             {
                 foreach (var p in data.Players) 
                 {
                     if (p.PlayerName != "Server" && p.PlayerName != "HostPlayer" && !p.IsReady) allReady = false; 
                 }
             }
             startGameButton.interactable = allReady;
        }
    }

    private void UpdatePlayerList(LobbyStateData data, string gmId)
    {
        foreach (Transform child in playerListContent) Destroy(child.gameObject);

        ContentAssetRegistry.GameModes.TryGetValue(gmId, out var gm);
        
        if (data.Players != null)
        {
            foreach (var p in data.Players)
            {
                if (p.PlayerName == "Server") continue;

                var go = Instantiate(playerListEntryPrefab, playerListContent);
                var txt = go.GetComponentInChildren<TMP_Text>();
                string status = p.IsReady ? "<color=green>READY</color>" : "<color=red>WAITING</color>";
                
                string teamName = "FFA";
                string colorHex = "#FFFFFF";

                if (gm != null && gm.teams != null && p.TeamId > 0 && p.TeamId <= gm.teams.Length)
                {
                    var teamDef = gm.teams[p.TeamId - 1];
                    teamName = teamDef.teamName;
                    colorHex = "#" + ColorUtility.ToHtmlStringRGB(teamDef.teamColor);
                }

                if (txt) txt.text = $"<color={colorHex}>[{teamName}] {p.PlayerName}</color> ({p.SelectedHeroId}) - {status}";
            }
        }
    }

    private void OnReadyToggled(bool isOn)
    {
        manager.ToggleReady(isOn);
    }

    private void OnStartGameClicked()
    {
        string map = "Map1";
        if (mapDropdown && mapDropdown.options.Count > 0)
        {
            map = mapDropdown.options[mapDropdown.value].text;
        }
        string mode = ContentAssetRegistry.DefaultGameModeId;
        if (gameModeDropdown && gameModeDropdown.options.Count > 0 && availableGameModeIds.Count > gameModeDropdown.value)
        {
            mode = availableGameModeIds[gameModeDropdown.value];
        }
        manager.StartGameRequest(map, mode);
    }

    // UI Helper for Team Selection (linked to buttons in scene)
    public void OnJoinTeam1() => manager.ChangeTeam(1);
    public void OnJoinTeam2() => manager.ChangeTeam(2);



    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
        
        // Reset fetch state so we can retry if we re-enter the lobby
        if (fetchCoroutine != null)
        {
            StopCoroutine(fetchCoroutine);
            fetchCoroutine = null;
        }
        hasAttemptedFetch = false;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (logString.StartsWith("[UPnP]") && logsText)
        {
            // Append log to the dedicated UI text
            logsText.text += logString + "\n";
            
            // Optional: Force canvas update if inside a layout group
            // Canvas.ForceUpdateCanvases(); 
        }
    }
}
