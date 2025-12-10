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
                default:
                    break;
            }
        }
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
