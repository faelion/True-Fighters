using System.Collections.Generic;
using UnityEngine;
using ServerGame.Entities;
using ClientContent;
using Unity.Cinemachine;

public class NetEntitySpawner : MonoBehaviour
{
    [SerializeField] private GameObject basePlayerPrefab;
    [SerializeField] private GameObject baseNeutralPrefab; // Added

    private readonly Dictionary<int, NetEntityView> views = new Dictionary<int, NetEntityView>();

    public NetEntityView GetView(int id)
    {
        if (views.TryGetValue(id, out var view)) return view;
        return null;
    }

    void OnEnable()
    {
        ClientMessageRouter.OnEntityState += OnEntityState;
        ClientMessageRouter.OnServerEvent += OnServerEvent;
        ClientContent.ContentAssetRegistry.EnsureLoaded();
    }

    void OnDisable()
    {
        ClientMessageRouter.OnEntityState -= OnEntityState;
        ClientMessageRouter.OnServerEvent -= OnServerEvent;
    }

    private void OnServerEvent(IGameEvent ev)
    {
        if (ev.Type == GameEventType.EntityDespawn)
        {
            if (views.TryGetValue(ev.CasterId, out var view))
            {
                Destroy(view.gameObject);
                views.Remove(ev.CasterId);
            }
        }
    }

    private void OnEntityState(EntityStateData m)
    {
        if (m == null) return;

        if (!views.TryGetValue(m.entityId, out var view) || view == null)
        {
            CreateEntity(m);
        }
    }

    private void CreateEntity(EntityStateData m)
    {
        GameObject go = null;
        var type = (ServerGame.Entities.EntityType)m.entityType;

        // Unified Prefab Lookup
        GameObject prefab = ContentAssetRegistry.GetPrefab(m.archetypeId);

        // Special handling for Hero Base Container (which holds the visual prefab inside)
        if (type == ServerGame.Entities.EntityType.Hero)
        {
             if (basePlayerPrefab) 
             {
                 go = Instantiate(basePlayerPrefab);
                 // If we found a specific hero visual, instantiate it inside the base
                 if (prefab != null)
                 {
                     var visuals = Instantiate(prefab, go.transform);
                     visuals.transform.localPosition = Vector3.zero;
                     visuals.transform.localRotation = Quaternion.identity;
                 }
             }
             else 
             {
                 Debug.LogWarning("[NetEntitySpawner] BasePlayerPrefab not assigned! Using primitive.");
                 go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
             }
        }
        else
        {
            // Standard Entities directly use the prefab
            if (prefab != null) go = Instantiate(prefab);
        }

        if (go == null) go = new GameObject($"Entity_{m.entityId}");
        
        NetEntityView view = go.GetComponent<NetEntityView>() ?? go.AddComponent<NetEntityView>();
        view.Initialize(m);
        views[m.entityId] = view;

        // Camera Logic for Local Player
        if (type == ServerGame.Entities.EntityType.Hero)
        {
            var clientNet = FindFirstObjectByType<ClientNetwork>();
            var vcam = go.GetComponentInChildren<CinemachineCamera>(true);

            if (clientNet != null && clientNet.AssignedPlayerId == m.entityId)
            {
                if (vcam != null)
                {
                    Debug.Log($"[NetEntitySpawner] Activating Camera for Local Player {m.entityId}");
                    vcam.gameObject.SetActive(true);
                    vcam.enabled = true;
                }
                else
                {
                    Debug.LogWarning("[NetEntitySpawner] No CinemachineVirtualCamera found on BasePlayer for local player!");
                }
            }
            else
            {
                if (vcam != null) Destroy(vcam.gameObject);
            }
        }
    }
}
