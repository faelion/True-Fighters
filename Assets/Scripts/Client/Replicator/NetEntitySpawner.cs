using System.Collections.Generic;
using UnityEngine;
using ServerGame.Entities;
using ClientContent;
using Unity.Cinemachine;

public class NetEntitySpawner : MonoBehaviour
{
    [SerializeField] private GameObject basePlayerPrefab;

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

        if (type == ServerGame.Entities.EntityType.Hero)
        {
            if (basePlayerPrefab) go = Instantiate(basePlayerPrefab);
            else 
            {
                Debug.LogWarning("[NetEntitySpawner] BasePlayerPrefab not assigned! Using primitive.");
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            }
        }
        else if (type == ServerGame.Entities.EntityType.Projectile)
        {
            if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(m.archetypeId, out var ability) && ability is ClientContent.ProjectileAbilityAsset projAbility)
                go = Instantiate(projAbility.projectilePrefab);         
        }
        else if (type == ServerGame.Entities.EntityType.Neutral)
        {
             var neutral = ClientContent.ContentAssetRegistry.GetNeutral(m.archetypeId);
             if (neutral) go = Instantiate(neutral.prefab);
        }
        else if(type == ServerGame.Entities.EntityType.Melee)
        {
            if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(m.archetypeId, out var ability) && ability is ClientContent.AttackCaCAbilityAsset cacAbility)
                go = Instantiate(cacAbility.CaCPrefab);
            else if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(m.archetypeId, out var cone) && cone is ClientContent.ConeCaCAbilityAsset coneAbility)
                go = Instantiate(coneAbility.conePrefab);
        }
        else if(type == ServerGame.Entities.EntityType.AoE)
        {
            if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(m.archetypeId, out var ability) && ability is ClientContent.AoEAbilityAsset aoeAbility)
                go = Instantiate(aoeAbility.aoePrefab);
        }

        if (go == null) go = new GameObject($"Entity_{m.entityId}");

        if (go == null) go = new GameObject($"Entity_{m.entityId}");
        
        NetEntityView view = go.GetComponent<NetEntityView>() ?? go.AddComponent<NetEntityView>();
        view.Initialize(m);
        views[m.entityId] = view;

        if (type == ServerGame.Entities.EntityType.Hero)
        {
            GameObject visualPrefab = null;
            
            // Try explicit ID
            if (!string.IsNullOrEmpty(m.archetypeId) && ClientContent.ContentAssetRegistry.Heroes.TryGetValue(m.archetypeId, out var hero) && hero.heroPrefab)
            {
                 visualPrefab = hero.heroPrefab;
            }
            // Fallback to Default ID
            else if (ClientContent.ContentAssetRegistry.Heroes.TryGetValue(ClientContent.ContentAssetRegistry.DefaultHeroId, out var defHero) && defHero.heroPrefab)
            {
                 Debug.LogWarning($"[NetEntitySpawner] HeroID '{m.archetypeId}' not found. Falling back to default '{ClientContent.ContentAssetRegistry.DefaultHeroId}'. Available Keys: {string.Join(", ", ClientContent.ContentAssetRegistry.Heroes.Keys)}");
                 visualPrefab = defHero.heroPrefab;
            }
            // Last resort: Any hero
            else 
            {
                Debug.LogError($"[NetEntitySpawner] Requested '{m.archetypeId}' AND Default not found. Using random first available. Keys: {string.Join(", ", ClientContent.ContentAssetRegistry.Heroes.Keys)}");
                foreach(var h in ClientContent.ContentAssetRegistry.Heroes.Values) 
                { 
                    if(h.heroPrefab) { visualPrefab = h.heroPrefab; break; } 
                }
            }

            if (visualPrefab)
            {
                var visuals = Instantiate(visualPrefab, go.transform);
                visuals.transform.localPosition = Vector3.zero;
                visuals.transform.localRotation = Quaternion.identity;
            }

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
