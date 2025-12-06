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

    private void OnEntityState(EntityStateData m)
    {
        if (view == null) return;
        if (m.entityId != view.entityId) return;

        // Extract HP from component if available
        float currentHp = -1f;
        if (m.components != null)
        {
            foreach (var comp in m.components)
            {
                if (comp.type == (int)ServerGame.Entities.ComponentType.Health)
                {
                    // Manual deserialization of HealthComponent
                    // Format: maxHp(float), currentHp(float), invunerable(bool)
                    using (var ms = new System.IO.MemoryStream(comp.data))
                    using (var br = new System.IO.BinaryReader(ms))
                    {
                        br.ReadSingle(); // maxHp
                        currentHp = br.ReadSingle();
                    }
                    break;
                }
            }
        }

        if (currentHp < 0f) return; // No health update this tick

        if (lastHp < 0f)
        {
            lastHp = currentHp;
            return;
        }
        if (currentHp < lastHp - 0.01f)
        {
            flashing = true;
            flashTimer = flashDuration;
            SetHit(true);
        }
        lastHp = currentHp;
    }

    public void SetHit(bool isHit)
    {
        if (cachedRenderer == null) return;
        var mat = cachedRenderer.material;
        if (mat == null) return;
        mat.color = isHit ? hitColor : normalColor;
    }
}
