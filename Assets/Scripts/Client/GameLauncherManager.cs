using UnityEngine;
using System.Collections;

public class GameLauncherManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject serverNetworkPrefab;
    public GameObject clientNetworkPrefab;

    private ServerNetwork activeServer;
    private ClientNetwork activeClient;

    public void JoinGame(string ip, string name, string hero)
    {
        NetworkConfig.playerName = name;
        NetworkConfig.heroId = hero;

        var go = Instantiate(clientNetworkPrefab);
        activeClient = go.GetComponent<ClientNetwork>();
        
        var lobby = FindFirstObjectByType<LobbyManager>(FindObjectsInactive.Include);
        if (lobby) lobby.SetNetwork(activeClient);

        activeClient.Connect(ip, 7777);
    }

    public void StartServer(string map)
    {
        NetworkConfig.playerName = "Server";
        var srvGo = Instantiate(serverNetworkPrefab);
        activeServer = srvGo.GetComponent<ServerNetwork>();
        activeServer.Init();

        var slm = srvGo.AddComponent<ServerLobbyManager>();
        
        var lobby = FindFirstObjectByType<LobbyManager>(FindObjectsInactive.Include);
        if (lobby) lobby.SetServer(slm);

        slm.Init(activeServer);
        
        // Server stays in Lobby Mode until StartGame message
    }

    public void StartHost(string map, string hero)
    {
        NetworkConfig.heroId = hero;

        StartCoroutine(HostStartRoutine(map));
    }

    private IEnumerator HostStartRoutine(string map)
    {
        var srvGo = Instantiate(serverNetworkPrefab);
        activeServer = srvGo.GetComponent<ServerNetwork>();
        activeServer.Init();

        var slm = srvGo.AddComponent<ServerLobbyManager>();
        slm.Init(activeServer);

        var cliGo = Instantiate(clientNetworkPrefab);
        activeClient = cliGo.GetComponent<ClientNetwork>();
        
        var lobby = FindFirstObjectByType<LobbyManager>(FindObjectsInactive.Include);
        if (lobby) 
        {
            lobby.SetNetwork(activeClient);
            lobby.SetServer(slm);
        }

        activeClient.Connect("127.0.0.1", 7777);

        // Wait for connection
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
            // The Lobby UI will appear because ClientNetwork is connected.
            Debug.Log("Host connected to Lobby.");
        }
    }
}
