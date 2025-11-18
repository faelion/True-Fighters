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

    private GameObject SpawnOrUpdate(GameObject current, GameObject prefab, Vector3 pos, float rotY, string tag = null, int? entityId = null, bool addHitFlash = false)
    {
        if (!current)
        {
            if (!prefab) return null;
            var go = Instantiate(prefab, pos, Quaternion.Euler(0f, rotY, 0f));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            if (!string.IsNullOrEmpty(tag)) go.tag = tag;
            if (entityId.HasValue)
            {
                var view = go.GetComponent<NetEntityView>() ?? go.AddComponent<NetEntityView>();
                view.entityId = entityId.Value;
            }
            if (addHitFlash && !go.GetComponent<HitFlash>()) go.AddComponent<HitFlash>();
            return go;
        }
        else
        {
            current.transform.position = pos;
            current.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            return current;
        }
    }

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
            localPlayerGO = SpawnOrUpdate(localPlayerGO, playerVisualPrefab, new Vector3(s.posX, 0f, s.posY), s.rotZ, "Player", s.playerId, addHitFlash: true);
            return;
        }

        if (s.playerId == 999)
        {
            serverObjectGO = SpawnOrUpdate(serverObjectGO, serverObjectVisualPrefab, new Vector3(s.posX, 0f, s.posY), s.rotZ);
            return;
        }

        if (!otherPlayers.TryGetValue(s.playerId, out var otherGO) || !otherGO)
        {
            var go = SpawnOrUpdate(null, playerVisualPrefab, new Vector3(s.posX, 0f, s.posY), s.rotZ, null, s.playerId, addHitFlash: true);
            if (go) otherPlayers[s.playerId] = go;
        }
        else
        {
            SpawnOrUpdate(otherGO, playerVisualPrefab, new Vector3(s.posX, 0f, s.posY), s.rotZ);
        }
    }
}
