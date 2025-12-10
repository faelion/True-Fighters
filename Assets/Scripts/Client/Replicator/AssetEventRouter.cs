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
        Debug.Log("[AssetEventRouter] Received server event, attempting to route.");
        if (evt == null || string.IsNullOrEmpty(evt.SourceId)) return;
        Debug.Log($"[AssetEventRouter] Routing event of type {evt.Type} from source {evt.SourceId}");
        if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(evt.SourceId, out var asset) && asset != null)
        {
            Debug.Log($"[AssetEventRouter] Found AbilityAsset for source {evt.SourceId}, routing event.");
            asset.ClientHandleEvent(evt, gameObject);
        }
    }
}
