using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Shared;
using ClientContent;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject hostControlsPanel;  // Bottom/Host Only
    
    [Header("Prefabs")]
    public GameObject heroButtonPrefab;
    public GameObject playerListEntryPrefab;

    [Header("References")]
    public Transform heroGridContent;
    public Transform playerListContent;
    public GameObject previewArea; // New Reference for the container
    public Image heroPreviewImage;
    public TMP_Text heroPreviewName;
    public TMP_Text heroPreviewDescription;
    public Toggle readyToggle;
    public Button startGameButton;
    public TMP_Dropdown mapDropdown; // Replaces mapNameInput
    public TMP_Text lobbyInfoText; // "Waiting for players..." or IP

    private LobbyManager manager;
    private Dictionary<string, AbilityAsset> loadedHeroes;
        
    public void Init(LobbyManager mgr)
    {
        manager = mgr;
        
        if (readyToggle) readyToggle.onValueChanged.AddListener(OnReadyToggled);
        if (startGameButton) startGameButton.onClick.AddListener(OnStartGameClicked);
        
        // Populate Maps dynamically from Folder
        if (mapDropdown)
        {
            mapDropdown.ClearOptions();
            List<string> maps = new List<string>();
            
            // Try to scan folder (Works in Editor and PC builds if data is kept, otherwise fallback)
            try 
            {
                // Assuming project structure: Assets/Scenes/Maps
                // In Editor: Application.dataPath points to Assets
                // In Build: pointing to Assets might fail, usually we use scenes in BuildSettings.
                // But per user request:
                string mapPath = System.IO.Path.Combine(Application.dataPath, "Scenes", "Maps");
                if (System.IO.Directory.Exists(mapPath))
                {
                    var files = System.IO.Directory.GetFiles(mapPath, "*.unity");
                    foreach(var f in files)
                    {
                        maps.Add(System.IO.Path.GetFileNameWithoutExtension(f));
                    }
                }
                else
                {
                    Debug.LogWarning($"[LobbyUI] Map directory not found at: {mapPath}");
                     // Fallback check in case path is slightly different or not existing
                     maps.Add("Arena");
                     maps.Add("Map1");
                }
            }
            catch(System.Exception e)
            {
                Debug.LogError($"[LobbyUI] Error scanning maps: {e.Message}");
                maps.Add("Arena"); 
            }
            
            if (maps.Count == 0) maps.Add("Arena"); // Safety
            mapDropdown.AddOptions(maps);
        }

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
                lobbyInfoText.text = $"Local IP: {GetLocalIPAddress()}:9050";
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

    public void UpdateLobby(LobbyStateData data, int myPlayerId)
    {
        // Update Player List / Teams
        foreach (Transform child in playerListContent) Destroy(child.gameObject);

        if (data.Players != null)
        {
            foreach (var p in data.Players)
            {
                if (p.PlayerName == "Server") continue; // Skip displaying invisible server player

                var go = Instantiate(playerListEntryPrefab, playerListContent);
                var txt = go.GetComponentInChildren<TMP_Text>();
                string status = p.IsReady ? "<color=green>READY</color>" : "<color=red>WAITING</color>";
                string team = p.TeamId == 0 ? "Spectator" : $"Team {p.TeamId}";
                
                if (txt) txt.text = $"[{team}] {p.PlayerName} ({p.SelectedHeroId}) - {status}";
            }
        }

        // Update Host Controls state
        if (startGameButton)
        {
            bool allReady = true;
            if (data.Players != null)
            {
                // Host/Server is not required to be ready usually, checking clients
                foreach (var p in data.Players) 
                {
                    if (p.PlayerName != "Server" && p.PlayerName != "HostPlayer" && !p.IsReady) allReady = false; 
                }
            }
            startGameButton.interactable = allReady;
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
        manager.StartGameRequest(map);
    }

    // UI Helper for Team Selection (linked to buttons in scene)
    public void OnJoinTeam1() => manager.ChangeTeam(1);
    public void OnJoinTeam2() => manager.ChangeTeam(2);
}
