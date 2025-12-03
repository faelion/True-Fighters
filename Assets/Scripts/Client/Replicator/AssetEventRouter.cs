using UnityEngine;


public class AssetEventRouter : MonoBehaviour
{
    public string databaseResourcePath = "ContentDatabase";

    void OnEnable()
    {
        ClientContent.ContentAssetRegistry.EnsureLoaded(databaseResourcePath);
        ClientMessageRouter.OnServerEvent += OnServerEvent;
    }

    void OnDisable()
    {
        ClientMessageRouter.OnServerEvent -= OnServerEvent;
    }

    private void OnServerEvent(IGameEvent evt)
    {
        if (evt == null || string.IsNullOrEmpty(evt.SourceId)) return;
        if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(evt.SourceId, out var asset) && asset != null)
        {
            asset.ClientHandleEvent(evt, gameObject);
        }
    }
}
