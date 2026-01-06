using ServerGame.Entities;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using Client.Replicator;

public class NetEntityView : MonoBehaviour
{
    public static Dictionary<int, NetEntityView> AllViews = new Dictionary<int, NetEntityView>();

    public int entityId;
    public string ArchetypeId;
    
    // Mapping ComponentType ID -> List of Visual Handlers
    private Dictionary<int, List<INetworkComponentVisual>> visualHandlers = new Dictionary<int, List<INetworkComponentVisual>>();

    void Awake()
    {
        // 1. Find existing handlers (Custom scripts attached in Editor)
        var handlers = GetComponentsInChildren<INetworkComponentVisual>(true);
        foreach (var h in handlers)
        {
            RegisterHandler(h);
        }

        // 2. Add Defaults if missing (Backward Compatibility)
        if (!HasHandler((int)ComponentType.Transform)) RegisterHandler(gameObject.AddComponent<NetworkTransformVisual>());
        if (!HasHandler((int)ComponentType.Health)) RegisterHandler(gameObject.AddComponent<NetworkHealthVisual>());
        if (!HasHandler((int)ComponentType.StatusEffect)) RegisterHandler(gameObject.AddComponent<NetworkStatusEffectsVisual>());
        if (!HasHandler((int)ComponentType.Movement)) RegisterHandler(gameObject.AddComponent<NetworkMovementVisual>());
    }

    private void RegisterHandler(INetworkComponentVisual handler)
    {
        if (!visualHandlers.ContainsKey(handler.TargetComponentType))
        {
            visualHandlers[handler.TargetComponentType] = new List<INetworkComponentVisual>();
        }
        visualHandlers[handler.TargetComponentType].Add(handler);
    }

    private bool HasHandler(int typeId)
    {
        return visualHandlers.ContainsKey(typeId) && visualHandlers[typeId].Count > 0;
    }

    void OnEnable()
    {
        ClientMessageRouter.OnEntityState += OnEntityState;
        ClientMessageRouter.OnServerEvent += OnServerEvent;
        if (entityId != 0) AllViews[entityId] = this;
    }

    void OnDisable()
    {
        ClientMessageRouter.OnEntityState -= OnEntityState;
        ClientMessageRouter.OnServerEvent -= OnServerEvent;
        if (entityId != 0) AllViews.Remove(entityId);
    }
    
    public System.Action<IGameEvent> OnGameEvent;

    private void OnServerEvent(IGameEvent evt)
    {
        if (evt.CasterId != entityId) return;
        Debug.Log($"[NetEntityView] Invoking Game Event for Entity ID {entityId}, Event Type: {evt.Type}.");
        OnGameEvent?.Invoke(evt);
    }

    public void Initialize(EntityStateData m)
    {
        entityId = m.entityId;
        ArchetypeId = m.archetypeId;
        
        // Initialize Animation logic based on Archetype
        if (ClientContent.ContentAssetRegistry.Heroes.TryGetValue(m.archetypeId, out var hero))
        {
             GetComponentInChildren<NetworkHeroAnimator>()?.Initialize(hero);
        }
        else if (ClientContent.ContentAssetRegistry.Neutrals.TryGetValue(m.archetypeId, out var neutral))
        {
            GetComponentInChildren<NetworkNpcAnimator>()?.Initialize(neutral);
        }

        Debug.Log($"[NetEntityView] Initialized Entity View for ID {entityId}, Archetype '{ArchetypeId}'.");

        UpdateState(m);
        if (entityId != 0) AllViews[entityId] = this;
    }

    private void OnEntityState(EntityStateData m)
    {
        if (m.entityId != entityId) return;
        UpdateState(m);
    }
    
    // Reusable buffer to avoid allocations
    private byte[] buffer = new byte[1024]; 

    private void UpdateState(EntityStateData m)
    {
        if (m.components == null) return;

        foreach (var compData in m.components)
        {
            // If we have handlers for this component type
            if (visualHandlers.TryGetValue(compData.type, out var handlers))
            {
                // We wrap the data in a MemoryStream/BinaryReader
                // Since compData.data is already a byte[], we can just use it.
                // However, for safety, multiple readers might be needed if handlers advance the stream.
                // But typically we can just reset position or instantiate new readers.
                // Given the GC implication, let's just pass a new generic reader for now, optimization later.
                
                foreach(var handler in handlers)
                {
                    using (var ms = new MemoryStream(compData.data))
                    using (var reader = new BinaryReader(ms))
                    {
                        handler.OnNetworkUpdate(reader);
                    }
                }
            }
        }
    }
}
