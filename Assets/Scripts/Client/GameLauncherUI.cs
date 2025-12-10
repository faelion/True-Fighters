using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameLauncherUI : MonoBehaviour
{
    [Header("Manager Reference")]
    public GameLauncherManager manager;

    [Header("UI References")]
    public GameObject mainMenuPanel;
    public GameObject clientPanel;
    public GameObject hostPanel;
    public GameObject serverPanel;
    public GameObject lobbyPanel;

    [Header("Client Inputs")]
    public TMP_InputField clientIpInput;
    public TMP_InputField clientNameInput;
    // Hero input removed (handled in Lobby)

    [Header("Host/Server Inputs")]
    // Map/Hero inputs removed (handled in Lobby)
    public TMP_InputField hostNameInput;
    public TMP_Text hostIpDisplay;
    public TMP_Text serverIpDisplay;

    void Start()
    {
        ShowPanel(mainMenuPanel);
        if (hostIpDisplay) hostIpDisplay.text = "Local IP: " + GetLocalIPAddress();
        if (serverIpDisplay) serverIpDisplay.text = "Local IP: " + GetLocalIPAddress();

        if (manager == null)
            manager = GetComponent<GameLauncherManager>() ?? FindFirstObjectByType<GameLauncherManager>();
    }

    public void OnClickHostMode()
    {
        ShowPanel(hostPanel);
    }

    public void OnClickClientMode()
    {
        ShowPanel(clientPanel);
    }

    public void OnClickServerMode()
    {
        ShowPanel(serverPanel);
    }

    public void OnClickBack()
    {
        ShowPanel(mainMenuPanel);
    }


    public void OnClickJoin()
    {
        string ip = string.IsNullOrEmpty(clientIpInput.text) ? "127.0.0.1" : clientIpInput.text;
        string name = string.IsNullOrEmpty(clientNameInput.text) ? "Player" : clientNameInput.text;
        
        // Hero defaults to "Warrior" or similar until selected in Lobby
        string defaultHero = NetworkConfig.heroId ?? "Warrior";

        manager.JoinGame(ip, name, defaultHero);
        clientPanel.SetActive(false);
    }


    public void OnClickStartHost()
    {
        // Manager starts Server in Lobby Mode. Map is selected later in Lobby UI.
        string defaultMap = "Map1"; 
        string defaultHero = "Warrior";
        
        string hostName = (hostNameInput != null && !string.IsNullOrEmpty(hostNameInput.text)) ? hostNameInput.text : "Host";
        NetworkConfig.playerName = hostName;
        
        manager.StartHost(defaultMap, defaultHero);
    }


    public void OnClickStartServer()
    {
        string defaultMap = "Map1";
        
        manager.StartServer(defaultMap);
    }
    private string GetPath(GameObject go)
    {
        if (go == null) return "null";
        return go.scene.name + "::" + go.transform.parent?.name + "/" + go.name;
    }

    private void ShowPanel(GameObject panel)
    {
        Debug.Log($"[UI] Request to show: {GetPath(panel)}");
        
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (clientPanel) clientPanel.SetActive(false);
        
        if (hostPanel && hostPanel.activeSelf) 
        { 
            Debug.Log($"[UI] Disabling Host Panel: {GetPath(hostPanel)}");
            hostPanel.SetActive(false); 
        }
        
        // Brute force safety check
        var rootHost = GameObject.Find("HostCanvas");
        if (rootHost != null && rootHost.activeSelf)
        {
             Debug.LogError($"[UI] BRUTE FORCE: HostCanvas was still active! Disabling {GetPath(rootHost)}");
             rootHost.SetActive(false);
        }

        if (serverPanel) serverPanel.SetActive(false);
        if (lobbyPanel) lobbyPanel.SetActive(false);
        
        if (panel != null) 
        {
            Debug.Log($"[UI] Enabling Panel: {GetPath(panel)}");
            panel.SetActive(true);
        }
    }

    public void ShowLobby()
    {
        ShowPanel(lobbyPanel);
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
}
