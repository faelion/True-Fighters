using System.Collections.Generic;
using UnityEngine;
using ServerGame.Entities;
using ClientContent;


public class NetEntitySpawner : MonoBehaviour
{
    [SerializeField] private GameObject defaultHeroPrefab;

    private readonly Dictionary<int, NetEntityView> views = new Dictionary<int, NetEntityView>();

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
        else if (ev.Type == GameEventType.EntitySpawn)
        {
            // Optional: Play spawn effect
            Debug.Log($"[NetEntitySpawner] Spawn event for entity {ev.CasterId}");
        }
    }

    private void OnEntityState(EntityStateData m)
    {
        if (m == null) return;

        if (!views.TryGetValue(m.entityId, out var view) || view == null)
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
            view.Initialize(m);
            views[m.entityId] = view;
        }
    }

    private GameObject GetPrefabForMessage(EntityStateData m)
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
            case ServerGame.Entities.EntityType.Projectile:
                if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(m.archetypeId, out var ability) && ability is ClientContent.ProjectileAbilityAsset projAbility)
                {
                    return projAbility.projectilePrefab;
                }
                break;
            default:
                break;
        }

        ClientContent.ContentAssetRegistry.Heroes.TryGetValue(ClientContent.ContentAssetRegistry.DefaultHeroId, out var defHero);
        if (defHero != null && defHero.heroPrefab)
            return defHero.heroPrefab;
        return defaultHeroPrefab;
    }
}
