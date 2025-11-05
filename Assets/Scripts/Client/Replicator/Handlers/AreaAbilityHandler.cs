using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Handles SpawnArea visuals; prefabs map per ability id
public class AreaAbilityHandler : MonoBehaviour, IAbilityEventHandler
{
    [Serializable]
    public struct Map
    {
        public string abilityId;
        public GameObject prefab;
        public float scalePerRadius; // how to scale prefab given radius (optional)
    }

    public List<Map> mappings = new List<Map>();
    public GameObject defaultAreaPrefab;

    private readonly Dictionary<string, GameObject> abilityToPrefab = new Dictionary<string, GameObject>();
    private readonly List<GameObject> spawned = new List<GameObject>();

    void Awake()
    {
        abilityToPrefab.Clear();
        foreach (var m in mappings)
            if (!string.IsNullOrEmpty(m.abilityId) && m.prefab)
                abilityToPrefab[m.abilityId] = m.prefab;
    }

    public void Handle(AbilityEventMessage evt)
    {
        if (evt.eventType != AbilityEventType.SpawnArea) return;

        GameObject prefab = null;
        if (!string.IsNullOrEmpty(evt.abilityIdOrKey))
            abilityToPrefab.TryGetValue(evt.abilityIdOrKey, out prefab);
        if (!prefab) prefab = defaultAreaPrefab;

        GameObject go;
        if (prefab)
            go = Instantiate(prefab, new Vector3(evt.posX, 0f, evt.posY), Quaternion.identity);
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.position = new Vector3(evt.posX, 0f, evt.posY);
            go.transform.localScale = new Vector3(2f, 0.1f, 2f);
        }
        SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        spawned.Add(go);
        if (evt.lifeMs > 0)
            Destroy(go, evt.lifeMs / 1000f);
    }
}

