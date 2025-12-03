using UnityEngine;

public class NetEntityView : MonoBehaviour
{
    public int entityId;

    void OnEnable()
    {
        ClientMessageRouter.OnEntityState += OnEntityState;
    }

    void OnDisable()
    {
        ClientMessageRouter.OnEntityState -= OnEntityState;
    }

    public void Initialize(StateMessage m)
    {
        entityId = m.entityId;
        UpdateTransform(m);
    }

    private void OnEntityState(StateMessage m)
    {
        if (m.entityId != entityId) return;
        UpdateTransform(m);
    }

    private void UpdateTransform(StateMessage m)
    {
        transform.position = new Vector3(m.posX, transform.position.y, m.posY);
        transform.rotation = Quaternion.Euler(0f, m.rotZ, 0f);
    }
}
