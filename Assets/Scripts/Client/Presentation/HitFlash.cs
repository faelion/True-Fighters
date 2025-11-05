using UnityEngine;

public class HitFlash : MonoBehaviour
{
    public Color normalColor = Color.blue;
    public Color hitColor = Color.red;

    private Renderer cachedRenderer;
    private NetEntityView view;

    void Awake()
    {
        view = GetComponent<NetEntityView>();
        cachedRenderer = GetComponentInChildren<Renderer>();
    }

    void OnEnable()
    {
        ClientEventBus.OnEntityState += OnEntityState;
    }

    void OnDisable()
    {
        ClientEventBus.OnEntityState -= OnEntityState;
    }

    private void OnEntityState(StateMessage m)
    {
        if (view == null) return;
        if (m.playerId != view.entityId) return;
        SetHit(m.hit);
    }

    public void SetHit(bool isHit)
    {
        if (cachedRenderer == null) return;
        var mat = cachedRenderer.material;
        if (mat == null) return;
        mat.color = isHit ? hitColor : normalColor;
    }
}

