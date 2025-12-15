using UnityEngine;


public class AssetEventRouter : MonoBehaviour
{
    public string databaseResourcePath = "ContentDatabase";

    void OnEnable()
    {
        ClientMessageRouter.OnServerEvent += OnServerEvent;
        Debug.Log("[AssetEventRouter] Subscribed to server events.");
    }

    void OnDisable()
    {
        ClientMessageRouter.OnServerEvent -= OnServerEvent;
        Debug.Log("[AssetEventRouter] Unsubscribed from server events.");
    }

    private void OnServerEvent(IGameEvent evt)
    {
        if (evt == null || string.IsNullOrEmpty(evt.SourceId)) return;
        
        // Only route Ability events for now
        Debug.Log($"[AssetEventRouter] Received Event: {evt.Type}, Source: {evt.SourceId}");

        if (evt.Type == GameEventType.AbilityCasted && ClientContent.ContentAssetRegistry.Abilities.TryGetValue(evt.SourceId, out var ability))
        {
             var castedEvent = (AbilityCastedEvent)evt;
             var spawner = FindFirstObjectByType<NetEntitySpawner>();
             GameObject casterVisual = null;
             
             if (spawner)
             {
                 var view = spawner.GetView(castedEvent.CasterId);
                 if (view) casterVisual = view.gameObject;
             }

             if (casterVisual)
             {
                 Debug.Log($"[AssetEventRouter] Routing Cast to Ability '{ability.name}'. Caster: {casterVisual.name}");
                 ability.ClientOnCast(castedEvent, casterVisual);
             }
             else
             {
                 Debug.LogWarning($"[AssetEventRouter] Caster View {castedEvent.CasterId} not found for ability {evt.SourceId}");
             }
        }
        else if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(evt.SourceId, out var asset) && asset != null)
        {
            asset.ClientHandleEvent(evt, gameObject);
        }
    }
}
