using UnityEngine;
using Client.Replicator;
using ServerGame.Entities;
using System.Collections.Generic;

public class NetworkNpcAnimator : NetworkBaseAnimator
{
    private NetEntityView view;

    private void OnEnable()
    {
        view = GetComponent<NetEntityView>();
        if (view)
        {
            view.OnGameEvent += HandleGameEvent;
        }
    }

    private void OnDisable()
    {
        if (view)
        {
            view.OnGameEvent -= HandleGameEvent;
        }
    }

    // This method is called by NetEntityView when an event matches this entity
    public void HandleGameEvent(IGameEvent evt)
    {
        Debug.Log($"[NetworkNpcAnimator] Handling Game Event: {evt.Type}, SourceId: {evt.SourceId}");
        if (evt is AbilityCastedEvent castEvt && castEvt.SourceId == "melee_attack")
        {
            Debug.Log($"[NetworkNpcAnimator] Received Attack Event. Triggering Slot 0 (Attack).");
            TriggerAttack();
        }
    }

    public void TriggerAttack()
    {
        FireSlotTrigger(0);
    }

    public void Initialize(ClientContent.NeutralEntitySO neutralDef)
    {
        TryFindAnimator();
        if (!animator || neutralDef == null) return;

        RuntimeAnimatorController controllerToUse = neutralDef.baseControllerTemplate;
        if (controllerToUse == null) controllerToUse = animator.runtimeAnimatorController;

        if (controllerToUse != null)
        {
            // Gather Slot Clips
            // Map Attack -> Slot 0
            var slotClips = new List<AnimationClip>();
            if (neutralDef.attackClip != null)
            {
                 slotClips.Add(neutralDef.attackClip);
                Debug.Log($"[NetworkNpcAnimator] Mapped Attack Clip '{neutralDef.attackClip.name}' to Slot 0.");
            }
            // Add nulls for other slots if needed, or just list with 1 item.
            
            ApplyOverrides(controllerToUse, slotClips, neutralDef.idleClip, neutralDef.walkClip);
            Debug.Log($"[NetworkNpcAnimator] Initialized overrides for {neutralDef.name}.");
        }
        else
        {
             Debug.LogError($"[NetworkNpcAnimator] Failed to initialize overrides. No BaseController found!");
        }
    }
}
