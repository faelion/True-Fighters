using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EntityViewReplicator : MonoBehaviour
{
    public GameObject playerVisualPrefab;
    public GameObject serverObjectVisualPrefab;

    private GameObject localPlayerGO;
    private GameObject serverObjectGO;
    private readonly Dictionary<int, GameObject> otherPlayers = new Dictionary<int, GameObject>();
    private int localPlayerId = -1;

    void OnEnable()
    {
        ClientEventBus.OnJoinResponse += OnJoinResponse;
        ClientEventBus.OnEntityState += OnEntityState;
    }

    void OnDisable()
    {
        ClientEventBus.OnJoinResponse -= OnJoinResponse;
        ClientEventBus.OnEntityState -= OnEntityState;
    }

    private void OnJoinResponse(JoinResponseMessage jr)
    {
        localPlayerId = jr.assignedPlayerId;
        if (otherPlayers.TryGetValue(localPlayerId, out var go) && go != null)
        {
            localPlayerGO = go;
            otherPlayers.Remove(localPlayerId);
            localPlayerGO.tag = "Player";
        }
    }

    private void OnEntityState(StateMessage s)
    {
        if (s.playerId == localPlayerId)
        {
            if (!localPlayerGO)
            {
                if (playerVisualPrefab)
                {
                    localPlayerGO = Instantiate(playerVisualPrefab, new Vector3(s.posX, 0f, s.posY), Quaternion.Euler(0f, s.rotZ, 0f));
                    SceneManager.MoveGameObjectToScene(localPlayerGO, gameObject.scene);
                    localPlayerGO.tag = "Player";
                    var view = localPlayerGO.GetComponent<NetEntityView>() ?? localPlayerGO.AddComponent<NetEntityView>();
                    view.entityId = s.playerId;
                    if (!localPlayerGO.GetComponent<HitFlash>()) localPlayerGO.AddComponent<HitFlash>();
                }
            }
            else
            {
                localPlayerGO.transform.position = new Vector3(s.posX, 0f, s.posY);
                localPlayerGO.transform.rotation = Quaternion.Euler(0f, s.rotZ, 0f);
            }
            return;
        }

        if (s.playerId == 999)
        {
            if (!serverObjectGO && serverObjectVisualPrefab)
            {
                serverObjectGO = Instantiate(serverObjectVisualPrefab, new Vector3(s.posX, 0f, s.posY), Quaternion.Euler(0f, s.rotZ, 0f));
                SceneManager.MoveGameObjectToScene(serverObjectGO, gameObject.scene);
            }
            else if (serverObjectGO)
            {
                serverObjectGO.transform.position = new Vector3(s.posX, 0f, s.posY);
                serverObjectGO.transform.rotation = Quaternion.Euler(0f, s.rotZ, 0f);
            }
            return;
        }

        if (!otherPlayers.TryGetValue(s.playerId, out var otherGO) || otherGO == null)
        {
            if (playerVisualPrefab)
            {
                var go = Instantiate(playerVisualPrefab, new Vector3(s.posX, 0f, s.posY), Quaternion.Euler(0f, s.rotZ, 0f));
                SceneManager.MoveGameObjectToScene(go, gameObject.scene);
                var view = go.GetComponent<NetEntityView>() ?? go.AddComponent<NetEntityView>();
                view.entityId = s.playerId;
                if (!go.GetComponent<HitFlash>()) go.AddComponent<HitFlash>();
                otherPlayers[s.playerId] = go;
            }
        }
        else
        {
            otherGO.transform.position = new Vector3(s.posX, 0f, s.posY);
            otherGO.transform.rotation = Quaternion.Euler(0f, s.rotZ, 0f);
        }
    }
}

