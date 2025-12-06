using ServerGame.Entities;
using System.IO;
using UnityEngine;

public class NetEntityView : MonoBehaviour
{
    public int entityId;
    
    // Reuse server component DTOs for deserialization
    private readonly TransformComponent transformComp = new TransformComponent();

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
            if (compData.type == (int)ComponentType.Transform)
            {
                using (var ms = new MemoryStream(compData.data))
                using (var reader = new BinaryReader(ms))
                {
                    transformComp.Deserialize(reader);
                }
                transform.position = new Vector3(transformComp.posX, transform.position.y, transformComp.posY);
                transform.rotation = Quaternion.Euler(0f, transformComp.rotZ, 0f);
            }
        }
    }
}
