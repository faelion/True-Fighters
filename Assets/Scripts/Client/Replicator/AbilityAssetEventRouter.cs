using UnityEngine;

// Routes server events to the corresponding AbilityAsset for client-side view handling
public class AbilityAssetEventRouter : MonoBehaviour
{
    public string databaseResourcePath = "ContentDatabase";
    [SerializeField] private ClientMessageRouter router;

    void OnEnable()
    {
        ClientContent.AbilityAssetRegistry.EnsureLoaded(databaseResourcePath);
        if (router == null)
            router = FindObjectOfType<ClientMessageRouter>();
        if (router != null)
            router.OnServerEvent += OnServerEvent;
    }

    void OnDisable()
    {
        if (router != null)
            router.OnServerEvent -= OnServerEvent;
    }

    private void OnServerEvent(IGameEvent evt)
    {
        if (evt == null || string.IsNullOrEmpty(evt.SourceId)) return;
        if (ClientContent.AbilityAssetRegistry.Abilities.TryGetValue(evt.SourceId, out var asset) && asset != null)
        {
            asset.ClientHandleEvent(evt, gameObject);
        }
    }
}
