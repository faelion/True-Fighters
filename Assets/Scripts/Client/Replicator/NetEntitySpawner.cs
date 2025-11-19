using System.Collections.Generic;
using UnityEngine;

// Spawns and updates net entity views based on StateMessage stream.
public class NetEntitySpawner : MonoBehaviour
{
    [SerializeField] private ClientMessageRouter router;
    [SerializeField] private GameObject defaultHeroPrefab;

    private readonly Dictionary<int, NetEntityView> views = new Dictionary<int, NetEntityView>();

    void OnEnable()
    {
        if (router == null)
            router = FindFirstObjectByType<ClientMessageRouter>();
        if (router != null)
            router.OnEntityState += OnEntityState;

        ClientContent.AbilityAssetRegistry.EnsureLoaded();
    }

    void OnDisable()
    {
        if (router != null)
            router.OnEntityState -= OnEntityState;
    }

    private void OnEntityState(StateMessage m)
    {
        if (m == null) return;

        if (!views.TryGetValue(m.playerId, out var view) || view == null)
        {
            var prefab = GetPrefabForPlayer(m.playerId);
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

    private GameObject GetPrefabForPlayer(int playerId)
    {
        // For ahora usamos el héroe por defecto del registro; si no hay, fallback a defaultHeroPrefab.
        ClientContent.AbilityAssetRegistry.EnsureLoaded();
        ClientContent.HeroSO hero = null;
        if (!string.IsNullOrEmpty(ClientContent.AbilityAssetRegistry.DefaultHeroId))
            ClientContent.AbilityAssetRegistry.Heroes.TryGetValue(ClientContent.AbilityAssetRegistry.DefaultHeroId, out hero);

        if (hero == null && ClientContent.AbilityAssetRegistry.Heroes.Count > 0)
        {
            foreach (var h in ClientContent.AbilityAssetRegistry.Heroes.Values) { hero = h; break; }
        }
        return hero != null ? hero.heroPrefab : defaultHeroPrefab;
    }
}
