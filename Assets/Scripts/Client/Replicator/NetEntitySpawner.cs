using System.Collections.Generic;
using UnityEngine;
using ServerGame.Entities;
using ClientContent;
using Unity.Cinemachine;

public class NetEntitySpawner : MonoBehaviour
{
    [SerializeField] private GameObject basePlayerPrefab;
    // Removed defaultHeroPrefab in favor of Registry lookups

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

        // 1. Determine Root Object
        if (type == ServerGame.Entities.EntityType.Hero)
        {
            // Use BasePlayer generic prefab
            if (basePlayerPrefab) go = Instantiate(basePlayerPrefab);
            else 
            {
                Debug.LogWarning("[NetEntitySpawner] BasePlayerPrefab not assigned! Using primitive.");
                go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            }
        }
        else if (type == ServerGame.Entities.EntityType.Projectile)
        {
            // Projectiles are currently single prefab
            if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(m.archetypeId, out var ability) && ability is ClientContent.ProjectileAbilityAsset projAbility)
                go = Instantiate(projAbility.projectilePrefab);         
        }
        else if (type == ServerGame.Entities.EntityType.Neutral)
        {
             // Neutrals might also use a BaseNPC prefab in future, for now using direct prefab
             var neutral = ClientContent.ContentAssetRegistry.GetNeutral(m.archetypeId);
             if (neutral) go = Instantiate(neutral.prefab);
        }
        else if(type == ServerGame.Entities.EntityType.Melee)
        {
            if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(m.archetypeId, out var ability) && ability is ClientContent.AttackCaCAbilityAsset cacAbility)
                go = Instantiate(cacAbility.CaCPrefab);
        }
        else if(type == ServerGame.Entities.EntityType.AoE)
        {
            if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(m.archetypeId, out var ability) && ability is ClientContent.AoEAbilityAsset aoeAbility)
                go = Instantiate(aoeAbility.aoePrefab);
        }

        if (go == null) go = new GameObject($"Entity_{m.entityId}");

        // 2. Setup Components
        NetEntityView view = go.GetComponent<NetEntityView>() ?? go.AddComponent<NetEntityView>();
        view.Initialize(m);
        views[m.entityId] = view;

        // 3. Instantiate Visuals (For Heroes)
        if (type == ServerGame.Entities.EntityType.Hero)
        {
            GameObject visualPrefab = null; // Look up from Registry
            
            // Try explicit ID
            if (!string.IsNullOrEmpty(m.archetypeId) && ClientContent.ContentAssetRegistry.Heroes.TryGetValue(m.archetypeId, out var hero) && hero.heroPrefab)
            {
                 visualPrefab = hero.heroPrefab;
            }
            // Fallback to Default ID
            else if (ClientContent.ContentAssetRegistry.Heroes.TryGetValue(ClientContent.ContentAssetRegistry.DefaultHeroId, out var defHero) && defHero.heroPrefab)
            {
                 visualPrefab = defHero.heroPrefab;
            }
            // Last resort: Any hero
            else 
            {
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

            // 4. Handle Camera for Local Player
            var clientNet = FindFirstObjectByType<ClientNetwork>();
            var vcam = go.GetComponentInChildren<CinemachineCamera>(true);

            if (clientNet != null && clientNet.AssignedPlayerId == m.entityId)
            {
                if (vcam != null)
                {
                    Debug.Log($"[NetEntitySpawner] Activating Camera for Local Player {m.entityId}");
                    vcam.gameObject.SetActive(true);
                    vcam.enabled = true; // Ensure component is enabled
                }
                else
                {
                    Debug.LogWarning("[NetEntitySpawner] No CinemachineVirtualCamera found on BasePlayer for local player!");
                }
            }
            else
            {
                // Not local player, destroy camera
                if (vcam != null) Destroy(vcam.gameObject);
            }
        }
    }
}
