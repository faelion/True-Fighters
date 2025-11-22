using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameLauncher : MonoBehaviour
{
    [Header("UI References")]
    public GameObject mainMenuPanel;
    public GameObject clientPanel;
    public GameObject hostPanel;
    public GameObject serverPanel;

    [Header("Client Inputs")]
    public TMP_InputField clientIpInput;
    public TMP_InputField clientNameInput;
    public TMP_InputField clientHeroInput;

    [Header("Host/Server Inputs")]
    public TMP_InputField hostMapInput;
    public TMP_InputField hostHeroInput;
    public TMP_Text hostIpDisplay;
    public TMP_InputField serverMapInput;
    public TMP_Text serverIpDisplay;

    [Header("Prefabs")]
    public GameObject serverNetworkPrefab;
    public GameObject clientNetworkPrefab;

    private ServerNetwork activeServer;
    private ClientNetwork activeClient;

    void Start()
    {
        ShowPanel(mainMenuPanel);
        if (hostIpDisplay) hostIpDisplay.text = "Local IP: " + GetLocalIPAddress();
        if (serverIpDisplay) serverIpDisplay.text = "Local IP: " + GetLocalIPAddress();
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

    // --- Client Logic ---
    public void OnClickJoin()
    {
        string ip = string.IsNullOrEmpty(clientIpInput.text) ? "127.0.0.1" : clientIpInput.text;
        string name = string.IsNullOrEmpty(clientNameInput.text) ? "Player" : clientNameInput.text;
        string hero = string.IsNullOrEmpty(clientHeroInput.text) ? NetworkConfig.heroId : clientHeroInput.text;

        NetworkConfig.playerName = name;
        NetworkConfig.heroId = hero;

        // Spawn ClientNetwork
        var go = Instantiate(clientNetworkPrefab);
        activeClient = go.GetComponent<ClientNetwork>();
        activeClient.Connect(ip, 9050);

        // Disable UI, wait for game start
        clientPanel.SetActive(false);
    }

    // --- Host Logic ---
    public void OnClickStartHost()
    {
        string map = string.IsNullOrEmpty(hostMapInput.text) ? "Map1" : hostMapInput.text;
        string hero = string.IsNullOrEmpty(hostHeroInput.text) ? NetworkConfig.heroId : hostHeroInput.text;
        NetworkConfig.playerName = "HostPlayer";
        NetworkConfig.heroId = hero;

        StartCoroutine(HostStartRoutine(map));
    }

    private System.Collections.IEnumerator HostStartRoutine(string map)
    {
        // 1. Start Server (Bind port immediately)
        var srvGo = Instantiate(serverNetworkPrefab);
        activeServer = srvGo.GetComponent<ServerNetwork>();
        activeServer.Init();

        // 2. Start Client (Connect to localhost)
        var cliGo = Instantiate(clientNetworkPrefab);
        activeClient = cliGo.GetComponent<ClientNetwork>();
        activeClient.Connect("127.0.0.1", 9050);

        // 3. Wait for client to successfully join
        float timeout = 5f;
        while (!activeClient.HasAssignedId && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (!activeClient.HasAssignedId)
        {
            Debug.LogError("Host client failed to join local server within timeout.");
        }
        else
        {
            // 4. Start Game (Load scene and notify clients)
            activeServer.StartGame(map);
        }
    }

    // --- Server Logic ---
    public void OnClickStartServer()
    {
        string map = string.IsNullOrEmpty(serverMapInput.text) ? "Map1" : serverMapInput.text;
        
        var srvGo = Instantiate(serverNetworkPrefab);
        activeServer = srvGo.GetComponent<ServerNetwork>();
        
        activeServer.StartGame(map);
    }

    private void ShowPanel(GameObject panel)
    {
        mainMenuPanel.SetActive(false);
        clientPanel.SetActive(false);
        hostPanel.SetActive(false);
        serverPanel.SetActive(false);
        panel.SetActive(true);
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
