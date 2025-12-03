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
        activeClient.Connect(ip, 9050);
    }

    public void StartServer(string map)
    {
        var srvGo = Instantiate(serverNetworkPrefab);
        activeServer = srvGo.GetComponent<ServerNetwork>();
        activeServer.StartGame(map);
    }

    public void StartHost(string map, string hero)
    {
        NetworkConfig.playerName = "HostPlayer";
        NetworkConfig.heroId = hero;

        StartCoroutine(HostStartRoutine(map));
    }

    private IEnumerator HostStartRoutine(string map)
    {
        var srvGo = Instantiate(serverNetworkPrefab);
        activeServer = srvGo.GetComponent<ServerNetwork>();
        activeServer.Init();

        var cliGo = Instantiate(clientNetworkPrefab);
        activeClient = cliGo.GetComponent<ClientNetwork>();
        activeClient.Connect("127.0.0.1", 9050);

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
            activeServer.StartGame(map);
        }
    }
}
