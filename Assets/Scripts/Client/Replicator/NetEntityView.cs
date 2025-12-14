using ServerGame.Entities;
using System.IO;
using UnityEngine;

public class NetEntityView : MonoBehaviour
{
    public int entityId;
    
    public GameObject visualRoot;
    
    private readonly TransformComponent transformComp = new TransformComponent();
    private readonly ServerGame.Entities.HealthComponent healthComp = new ServerGame.Entities.HealthComponent();


    private bool isDead = false;
    private Renderer[] cachedRenderers;

    void Awake()
    {
        if (visualRoot == null)
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer) visualRoot = renderer.gameObject;
        }
    }

    void OnEnable()
    {
        ClientMessageRouter.OnEntityState += OnEntityState;
    }

    void OnDisable()
    {
        ClientMessageRouter.OnEntityState -= OnEntityState;
    }

    public void Initialize(EntityStateData m)
    {
        entityId = m.entityId;
        UpdateState(m);
    }

    private void OnEntityState(EntityStateData m)
    {
        if (m.entityId != entityId) return;
        UpdateState(m);
    }

    private readonly System.Collections.Generic.HashSet<string> currentEffects = new System.Collections.Generic.HashSet<string>();
    
    private void UpdateState(EntityStateData m)
    {
        if (m.components == null) return;

        foreach (var compData in m.components)
        {
            switch (compData.type)
            {
                case (int)ComponentType.Transform:
                    using (var ms = new MemoryStream(compData.data))
                    using (var reader = new BinaryReader(ms))
                    {
                        transformComp.Deserialize(reader);
                    }
                    transform.position = new Vector3(transformComp.posX, transform.position.y, transformComp.posY);
                    transform.rotation = Quaternion.Euler(0f, transformComp.rotZ, 0f);
                    break;
                case (int)ComponentType.Health:
                    using (var ms = new MemoryStream(compData.data))
                    using (var reader = new BinaryReader(ms))
                    {
                        healthComp.Deserialize(reader);
                    }
                    if (isDead != healthComp.IsDead)
                    {
                        isDead = healthComp.IsDead;
                        ToggleVisuals(!isDead);
                    }
                    break;
                case (int)ComponentType.StatusEffect:
                    using (var ms = new MemoryStream(compData.data))
                    using (var reader = new BinaryReader(ms))
                    {
                        HandleStatusEffects(reader);
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private void HandleStatusEffects(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        var serverEffects = new System.Collections.Generic.HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            string effectId = reader.ReadString();
            float remainingTime = reader.ReadSingle();
            int casterId = reader.ReadInt32();

            serverEffects.Add(effectId);

            // Fetch Asset
            if (ClientContent.ContentAssetRegistry.Effects.TryGetValue(effectId, out var effectAsset))
            {
                if (!currentEffects.Contains(effectId))
                {
                    // NEW
                    effectAsset.ClientOnStart(gameObject);
                }
                else
                {
                    // EXISTING
                    effectAsset.ClientOnTick(gameObject, Time.deltaTime); // Approximate DT
                }
            }
        }

        // Check for Removed
        foreach (var oldId in currentEffects)
        {
            if (!serverEffects.Contains(oldId))
            {
                if (ClientContent.ContentAssetRegistry.Effects.TryGetValue(oldId, out var effectAsset))
                {
                    effectAsset.ClientOnRemove(gameObject);
                }
            }
        }

        currentEffects.Clear();
        foreach(var id in serverEffects) currentEffects.Add(id);
    }

    private void ToggleVisuals(bool isActive)
    {
        if (visualRoot)
        {
            visualRoot.SetActive(isActive);
        }
        else
        {
            if (cachedRenderers == null) cachedRenderers = GetComponentsInChildren<Renderer>(true);
            foreach(var r in cachedRenderers) r.enabled = isActive;
        }
    }
}
