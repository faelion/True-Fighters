using UnityEngine;

// Routes ability events to the corresponding AbilityAsset for client-side view handling
public class AbilityAssetEventRouter : MonoBehaviour
{
    public string databaseResourcePath = "ContentDatabase";

    void OnEnable()
    {
        ClientContent.AbilityAssetRegistry.EnsureLoaded(databaseResourcePath);
        ClientEventBus.OnAbilityEvent += OnAbilityEvent;
    }

    void OnDisable()
    {
        ClientEventBus.OnAbilityEvent -= OnAbilityEvent;
    }

    private void OnAbilityEvent(AbilityEventMessage evt)
    {
        if (string.IsNullOrEmpty(evt.abilityIdOrKey)) return;
        if (ClientContent.AbilityAssetRegistry.Abilities.TryGetValue(evt.abilityIdOrKey, out var asset) && asset != null)
        {
            asset.ClientHandleEvent(evt, gameObject);
        }
    }
}

