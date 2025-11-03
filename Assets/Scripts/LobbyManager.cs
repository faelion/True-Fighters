using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public string serverSceneName = "ServerScene";
    public string clientSceneName = "ClientScene";

    public InputField hostInput;
    public InputField portInput;
    public InputField playerNameInput;
    public Button joinButton;

    IEnumerator Start()
    {
        if (hostInput) hostInput.text = NetworkConfig.serverHost;
        if (portInput) portInput.text = NetworkConfig.serverPort.ToString();
        if (playerNameInput) playerNameInput.text = NetworkConfig.playerName;

        if (!IsSceneLoaded(serverSceneName))
        {
            var loadOp = SceneManager.LoadSceneAsync(serverSceneName, LoadSceneMode.Additive);
            while (!loadOp.isDone) yield return null;
            Debug.Log("[LobbyManager] Server scene loaded additively: " + serverSceneName);
        }
        else
        {
            Debug.Log("[LobbyManager] Server scene already loaded: " + serverSceneName);
        }

        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinClicked);
        }
    }

    bool IsSceneLoaded(string name)
    {
        var s = SceneManager.GetSceneByName(name);
        return s.IsValid() && s.isLoaded;
    }

    public void OnJoinClicked()
    {
        if (hostInput != null) NetworkConfig.serverHost = hostInput.text.Trim();
        if (portInput != null && int.TryParse(portInput.text, out int p)) NetworkConfig.serverPort = p;
        if (playerNameInput != null) NetworkConfig.playerName = playerNameInput.text.Trim();

        StartCoroutine(LoadClientAndUnloadLobby());
    }

    IEnumerator LoadClientAndUnloadLobby()
    {
        var loadOp = SceneManager.LoadSceneAsync(clientSceneName, LoadSceneMode.Additive);
        while (!loadOp.isDone) yield return null;
        Debug.Log("[LobbyManager] Client scene loaded additively: " + clientSceneName);

        var clientScene = SceneManager.GetSceneByName(clientSceneName);
        if (clientScene.IsValid())
        {
            SceneManager.SetActiveScene(clientScene);
        }

        yield return null;

        var lobbyScene = gameObject.scene;
        foreach (var root in lobbyScene.GetRootGameObjects())
        {
            var al = root.GetComponentInChildren<AudioListener>(true);
            if (al != null)
                al.enabled = false;
        }

        var unloadOp = SceneManager.UnloadSceneAsync(lobbyScene);
        while (!unloadOp.isDone) yield return null;
        Debug.Log("[LobbyManager] Unloaded lobby scene.");
    }

}
