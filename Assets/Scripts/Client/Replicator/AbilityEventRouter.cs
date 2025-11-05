using System.Collections.Generic;
using UnityEngine;

// Subscribes to ability events and dispatches to attached handlers
public class AbilityEventRouter : MonoBehaviour
{
    private readonly List<IAbilityEventHandler> handlers = new List<IAbilityEventHandler>();

    void Awake()
    {
        GetComponentsInChildren(true, handlers);
    }

    void OnEnable()
    {
        ClientEventBus.OnAbilityEvent += OnAbilityEvent;
    }

    void OnDisable()
    {
        ClientEventBus.OnAbilityEvent -= OnAbilityEvent;
    }

    private void OnAbilityEvent(AbilityEventMessage evt)
    {
        for (int i = 0; i < handlers.Count; i++)
            handlers[i]?.Handle(evt);
    }
}

