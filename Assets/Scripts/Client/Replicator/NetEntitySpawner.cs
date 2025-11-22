using System.Collections.Generic;
using UnityEngine;
using ServerGame.Entities;
using ClientContent;

// Spawns and updates net entity views based on StateMessage stream.
public class NetEntitySpawner : MonoBehaviour
{
    [SerializeField] private ClientMessageRouter router;
    [SerializeField] private GameObject defaultHeroPrefab;

    private readonly Dictionary<int, NetEntityView> views = new Dictionary<int, NetEntityView>();

    void OnEnable()
    {
        // Try to find router, but also retry in Update if not found yet (e.g. scene load race condition)
        if (router == null)
            router = FindFirstObjectByType<ClientMessageRouter>();
        
        if (router != null)
        {
            router.OnEntityState += OnEntityState;
            router.OnServerEvent += OnServerEvent;
        }

        ClientContent.ContentAssetRegistry.EnsureLoaded();
    }

    void Update()
    {
        if (router == null)
        {
            router = FindFirstObjectByType<ClientMessageRouter>();
            if (router != null)
            {
                router.OnEntityState += OnEntityState;
                router.OnServerEvent += OnServerEvent;
            }
        }
    }

    void OnDisable()
    {
        if (router != null)
        {
            router.OnEntityState -= OnEntityState;
            router.OnServerEvent -= OnServerEvent;
        }
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

    private void OnEntityState(StateMessage m)
    {
        if (m == null) return;

        if (!views.TryGetValue(m.playerId, out var view) || view == null)
        {
            var prefab = GetPrefabForMessage(m);
            GameObject go;
            if (prefab != null)
                go = Instantiate(prefab);
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.transform.localScale = Vector3.one * 0.9f;
            }
            view = go.GetComponent<NetEntityView>() ?? go.AddComponent<NetEntityView>();
            view.entityId = m.playerId;
            views[m.playerId] = view;
        }

        var t = view.transform;
        t.position = new Vector3(m.posX, t.position.y, m.posY);
        t.rotation = Quaternion.Euler(0f, m.rotZ, 0f);
    }

    private GameObject GetPrefabForMessage(StateMessage m)
    {
        var type = (ServerGame.Entities.EntityType)m.entityType;
        switch (type)
        {
            case ServerGame.Entities.EntityType.Hero:
                if (!string.IsNullOrEmpty(m.archetypeId) && ClientContent.ContentAssetRegistry.Heroes.TryGetValue(m.archetypeId, out var hero) && hero.heroPrefab)
                    return hero.heroPrefab;
                break;
            case ServerGame.Entities.EntityType.Neutral:
                var neutral = ClientContent.ContentAssetRegistry.GetNeutral(m.archetypeId);
                if (neutral != null && neutral.prefab)
                    return neutral.prefab;
                break;
            default:
                break;
        }
        // fallback
        ClientContent.ContentAssetRegistry.Heroes.TryGetValue(ClientContent.ContentAssetRegistry.DefaultHeroId, out var defHero);
        if (defHero != null && defHero.heroPrefab)
            return defHero.heroPrefab;
        return defaultHeroPrefab;
    }
}
