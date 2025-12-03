using UnityEngine;

public class HitFlash : MonoBehaviour
{
    private Color normalColor = Color.blue;
    public Color hitColor = Color.red;
    public float flashDuration = 0.2f;

    private Renderer cachedRenderer;
    private NetEntityView view;
    private float lastHp = -1f;
    private bool flashing = false;
    private float flashTimer = 0f;

    private void Start()
    {
        view = GetComponent<NetEntityView>();
        cachedRenderer = GetComponentInChildren<Renderer>();
        normalColor = cachedRenderer.material.color;
    }

    void OnEnable()
    {
        ClientMessageRouter.OnEntityState += OnEntityState;
    }

    void OnDisable()
    {
        ClientMessageRouter.OnEntityState -= OnEntityState;
        flashing = false;
        lastHp = -1f;
        SetHit(false);
    }

    void Update()
    {
        if (!flashing) return;
        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
        {
            flashing = false;
            SetHit(false);
        }
    }

    private void OnEntityState(StateMessage m)
    {
        if (view == null) return;
        if (m.entityId != view.entityId) return;
        if (lastHp < 0f)
        {
            lastHp = m.hp;
            return;
        }
        if (m.hp < lastHp - 0.01f)
        {
            flashing = true;
            flashTimer = flashDuration;
            SetHit(true);
        }
        lastHp = m.hp;
    }

    public void SetHit(bool isHit)
    {
        if (cachedRenderer == null) return;
        var mat = cachedRenderer.material;
        if (mat == null) return;
        mat.color = isHit ? hitColor : normalColor;
    }
}
