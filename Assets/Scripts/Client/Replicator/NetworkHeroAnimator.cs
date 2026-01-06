using UnityEngine;
using Client.Replicator;
using System.Collections.Generic;

public class NetworkHeroAnimator : NetworkBaseAnimator
{
    public void TriggerAbility(string abilityId)
    {
        if (!animator) return;
        
        var view = GetComponent<NetEntityView>();
        if (!view || string.IsNullOrEmpty(view.ArchetypeId)) 
        {
            // Fallback
            FireSlotTrigger(0);
            return;
        }

        if (ClientContent.ContentAssetRegistry.Heroes.TryGetValue(view.ArchetypeId, out var hero))
        {
            int slotIndex = -1;
            for(int i=0; i<hero.bindings.Count; i++)
            {
                if (hero.bindings[i].ability != null && hero.bindings[i].ability.id == abilityId)
                {
                    slotIndex = i;
                    break;
                }
            }

            if (slotIndex >= 0)
            {
                FireSlotTrigger(slotIndex);
                Debug.Log($"[NetworkHeroAnimator] Firing ability '{abilityId}' at Slot {slotIndex}.");
            }
            else
            {
                 Debug.LogWarning($"[NetworkHeroAnimator] Ability '{abilityId}' NOT found in bindings for hero '{view.ArchetypeId}'.");
            }
        }
    }

    public void Initialize(ClientContent.HeroSO heroDef)
    {
        TryFindAnimator();
        if (!animator || heroDef == null) return;

        RuntimeAnimatorController controllerToUse = heroDef.baseControllerTemplate; 
        if (controllerToUse == null) controllerToUse = animator.runtimeAnimatorController;
        
        if (controllerToUse != null)
        {
            // Gather Slot Clips
            var slotClips = new List<AnimationClip>();
            foreach (var b in heroDef.bindings)
            {
                slotClips.Add(b.castClip);
            }

            ApplyOverrides(controllerToUse, slotClips, heroDef.idleClip, heroDef.walkClip);
            Debug.Log($"[NetworkHeroAnimator] Initialized overrides for {heroDef.name}.");
        }
    }
}
