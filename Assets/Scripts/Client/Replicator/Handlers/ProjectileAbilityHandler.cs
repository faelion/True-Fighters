using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Handles Spawn/Update/Despawn for projectile abilities. Prefab can vary per ability id.
public class ProjectileAbilityHandler : MonoBehaviour, IAbilityEventHandler
{
    [Serializable]
    public struct Map
    {
        public string abilityId;
        public GameObject prefab;
    }

    public List<Map> mappings = new List<Map>();
    public GameObject defaultProjectilePrefab;

    private readonly Dictionary<string, GameObject> abilityToPrefab = new Dictionary<string, GameObject>();
    private readonly Dictionary<int, GameObject> live = new Dictionary<int, GameObject>();

    void Awake()
    {
        abilityToPrefab.Clear();
        foreach (var m in mappings)
        {
            if (!string.IsNullOrEmpty(m.abilityId) && m.prefab)
                abilityToPrefab[m.abilityId] = m.prefab;
        }
    }

    public void Handle(AbilityEventMessage evt)
    {
        switch (evt.eventType)
        {
            case AbilityEventType.SpawnProjectile:
                OnSpawn(evt);
                break;
            case AbilityEventType.ProjectileUpdate:
                OnUpdate(evt);
                break;
            case AbilityEventType.ProjectileDespawn:
                OnDespawn(evt);
                break;
        }
    }

    void OnSpawn(AbilityEventMessage evt)
    {
        if (live.ContainsKey(evt.projectileId)) return;
        GameObject prefab = null;
        if (!string.IsNullOrEmpty(evt.abilityIdOrKey))
            abilityToPrefab.TryGetValue(evt.abilityIdOrKey, out prefab);
        if (!prefab) prefab = defaultProjectilePrefab;

        GameObject go;
        if (prefab)
        {
            go = Instantiate(prefab, new Vector3(evt.posX, 0f, evt.posY), Quaternion.identity);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.position = new Vector3(evt.posX, 0f, evt.posY);
            go.transform.localScale = Vector3.one * 0.3f;
        }
        SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        float angle = Mathf.Atan2(evt.dirY, evt.dirX) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0f, angle, 0f);
        live[evt.projectileId] = go;
    }

    void OnUpdate(AbilityEventMessage evt)
    {
        if (!live.TryGetValue(evt.projectileId, out var go) || !go) return;
        go.transform.position = new Vector3(evt.posX, 0f, evt.posY);
        if (evt.lifeMs <= 0)
        {
            Destroy(go);
            live.Remove(evt.projectileId);
        }
    }

    void OnDespawn(AbilityEventMessage evt)
    {
        if (live.TryGetValue(evt.projectileId, out var go))
        {
            if (go) Destroy(go);
            live.Remove(evt.projectileId);
        }
    }
}

