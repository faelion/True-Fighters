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
        string hero = string.IsNullOrEmpty(clientHeroInput.text) ? NetworkConfig.heroId : clientHeroInput.text;

        manager.JoinGame(ip, name, hero);
        clientPanel.SetActive(false);
    }


    public void OnClickStartHost()
    {
        string map = string.IsNullOrEmpty(hostMapInput.text) ? "Map1" : hostMapInput.text;
        string hero = string.IsNullOrEmpty(hostHeroInput.text) ? NetworkConfig.heroId : hostHeroInput.text;
        
        manager.StartHost(map, hero);
    }


    public void OnClickStartServer()
    {
        string map = string.IsNullOrEmpty(serverMapInput.text) ? "Map1" : serverMapInput.text;
        
        manager.StartServer(map);
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
