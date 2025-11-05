using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Handles Heal ability visuals (uses evt.value as amount)
public class HealAbilityHandler : MonoBehaviour, IAbilityEventHandler
{
    [System.Serializable]
    public struct Map { public string abilityId; public GameObject vfxPrefab; }
    public List<Map> mappings = new List<Map>();
    public GameObject defaultVfx;

    private readonly Dictionary<string, GameObject> map = new Dictionary<string, GameObject>();

    void Awake()
    {
        map.Clear();
        foreach (var m in mappings)
            if (!string.IsNullOrEmpty(m.abilityId) && m.vfxPrefab)
                map[m.abilityId] = m.vfxPrefab;
    }

    public void Handle(AbilityEventMessage evt)
    {
        if (evt.eventType != AbilityEventType.Heal) return;
        GameObject prefab = null;
        if (!string.IsNullOrEmpty(evt.abilityIdOrKey)) map.TryGetValue(evt.abilityIdOrKey, out prefab);
        if (!prefab) prefab = defaultVfx;
        if (!prefab) return;

        var go = Instantiate(prefab, new Vector3(evt.posX, 0f, evt.posY), Quaternion.identity);
        SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        Destroy(go, 1.0f);
    }
}

