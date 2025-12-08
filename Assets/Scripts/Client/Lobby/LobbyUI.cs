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
    public Image heroPreviewImage;
    public TMP_Text heroPreviewName;
    public TMP_Text heroPreviewDescription;
    public Toggle readyToggle;
    public Button startGameButton;
    public TMP_InputField mapNameInput;
    public TMP_Text lobbyInfoText; // "Waiting for players..." or IP

    private LobbyManager manager;
    private Dictionary<string, AbilityAsset> loadedHeroes; // Using AbilityAsset struct? No, need HeroSO or equivalent. 
    // ContentAssetRegistry seems to define Hero IDs but maybe not full data yet. 
    // For now, we simulate hero data or fetch from Registry if available.

    public void Init(LobbyManager mgr)
    {
        manager = mgr;
        PopulateHeroGrid();
        
        if (readyToggle) readyToggle.onValueChanged.AddListener(OnReadyToggled);
        if (startGameButton) startGameButton.onClick.AddListener(OnStartGameClicked);
        
        // Host controls visibility check
        bool isHost = NetworkConfig.playerName == "HostPlayer"; // Simple check
        if (hostControlsPanel) hostControlsPanel.SetActive(isHost);
    }

    private void PopulateHeroGrid()
    {
        // Clean existing
        foreach (Transform child in heroGridContent) Destroy(child.gameObject);

        // This should come from a Heroes Registry. Using hardcoded for prototype or Registry if possible.
        var heroes = new string[] { "Warrior", "Mage", "Rogue", "Cleric" }; // Example IDs

        foreach (var heroId in heroes)
        {
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
                var go = Instantiate(playerListEntryPrefab, playerListContent);
                var txt = go.GetComponentInChildren<TMP_Text>();
                string status = p.IsReady ? "<color=green>READY</color>" : "<color=red>WAITING</color>";
                string team = p.TeamId == 0 ? "Spectator" : $"Team {p.TeamId}";
                
                if (txt) txt.text = $"[{team}] {p.PlayerName} ({p.SelectedHeroId}) - {status}";
                
                // Add Team Change Buttons logic here if desired directly on the entry
            }
        }

        // Update Host Controls state
        if (startGameButton)
        {
            bool allReady = true;
            if (data.Players != null)
            {
                foreach (var p in data.Players) if (!p.IsReady) allReady = false;
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
        string map = mapNameInput ? mapNameInput.text : "Map1";
        manager.StartGameRequest(map);
    }

    // UI Helper for Team Selection (linked to buttons in scene)
    public void OnJoinTeam1() => manager.ChangeTeam(1);
    public void OnJoinTeam2() => manager.ChangeTeam(2);
}
