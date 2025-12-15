using UnityEngine;
using System.IO;
using ServerGame.Entities;
using Client.Replicator;
using System.Collections.Generic;

public class NetworkHeroAnimator : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;

    [Header("Parameters")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private float runSpeedThreshold = 0.1f; // Units per second approx
    [SerializeField] private float smoothTime = 0.1f;

    // Remove INetworkComponentVisual implementation
    // public int TargetComponentType => (int)ComponentType.Transform;

    private Vector3 lastPos;
    private float currentSpeed;
    private float speedVel; // For SmoothDamp

    void Awake()
    {
        TryFindAnimator();
        lastPos = transform.position;
    }

    private void TryFindAnimator()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator) 
        {
            animator.applyRootMotion = false; // Fix drift
        }
    }

    // Removed OnNetworkUpdate

    void Update()
    {
        if (animator)
        {
            // Calculate speed based on actual transform movement
            float dist = Vector3.Distance(transform.position, lastPos);
            float instantSpeed = dist / Time.deltaTime;
            
            // Normalize: If moving faster than threshold, considered "Run" (1.0)
            // You could also map this linearly if you wanted.
            float targetSpeed = (instantSpeed > runSpeedThreshold) ? 1f : 0f;

            // Smoothly move the parameter towards target
            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVel, smoothTime);
            animator.SetFloat(speedParam, currentSpeed);
            
            lastPos = transform.position;
        }
    }

    public void TriggerAbility(string abilityId)
    {
        if (!animator) return;
        
        var view = GetComponent<NetEntityView>();
        if (!view || string.IsNullOrEmpty(view.ArchetypeId)) 
        {
            // Fallback if no archetype info
            animator.SetTrigger("Base_Action_Slot1");
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
                // Slot 0 -> Slot1, Slot 1 -> Slot2, etc.
                string triggerName = $"Base_Action_Slot{slotIndex + 1}";
                animator.SetTrigger(triggerName);
                // reset other triggers to prevent state conflict? no, usually fine.
                Debug.Log($"[NetworkHeroAnimator] Found binding for ability '{abilityId}' at Slot {slotIndex}. Firing Trigger '{triggerName}'.");
            }
            else
            {
                 Debug.LogWarning($"[NetworkHeroAnimator] Ability '{abilityId}' NOT found in bindings for hero '{view.ArchetypeId}'. Bindings Count: {hero.bindings.Count}");
            }
        }
    }
    [Header("Override Config")]
    [SerializeField] private AnimationClip[] slotPlaceholders; // Assign dummy clips here to be replaced

    public void Initialize(ClientContent.HeroSO heroDef)
    {
        TryFindAnimator();
        if (!animator || heroDef == null) return;

        // If the Hero defines a specific controller or shared template, use it
        RuntimeAnimatorController controllerToUse = heroDef.baseControllerTemplate; 
        
        // If the prefab already has a controller, we might override it, or use the one from SO.
        if (controllerToUse == null) controllerToUse = animator.runtimeAnimatorController;
        
        if (controllerToUse != null)
        {
            // Validation
            if (slotPlaceholders == null || slotPlaceholders.Length == 0)
            {
                Debug.LogWarning($"[NetworkHeroAnimator] 'Slot Placeholders' is empty on {name}! Skill animations will NOT override. Please assign Dummy clips in the Inspector.");
            }

            // Create Override Controller to swap clips at runtime
            var overrideController = new AnimatorOverrideController(controllerToUse);
            var overrides = new System.Collections.Generic.List<KeyValuePair<AnimationClip, AnimationClip>>();

            Debug.Log($"[NetworkHeroAnimator] Processing Overrides. Bindings: {heroDef.bindings.Count}, Placeholders: {slotPlaceholders?.Length ?? 0}");

            // Link Slots
            for (int i = 0; i < heroDef.bindings.Count; i++)
            {
                // Ensure we have a placeholder for this slot index
                // We assume slotPlaceholders[0] maps to Binding[0] (Q), etc.
                if (slotPlaceholders != null && i < slotPlaceholders.Length && slotPlaceholders[i] != null)
                {
                    var binding = heroDef.bindings[i];
                    if (binding.castClip != null)
                    {
                        overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(slotPlaceholders[i], binding.castClip));
                        Debug.Log($"[NetworkHeroAnimator] Mapping Slot {i}: '{slotPlaceholders[i].name}' -> '{binding.castClip.name}'");
                    }
                    else
                    {
                        Debug.Log($"[NetworkHeroAnimator] Slot {i} has NO override clip in HeroSO.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[NetworkHeroAnimator] Slot {i} has no corresponding Placeholder in Inspector list!");
                }
            }

            Debug.Log($"[NetworkHeroAnimator] Applying {overrides.Count} overrides.");
            overrideController.ApplyOverrides(overrides);

            // Link Base Movement (Using string lookup for default clips "Base_Idle", "Base_Run")
            // This assumes the BaseController uses validation clips with these names.
            if (heroDef.idleClip) overrideController["Base_Idle"] = heroDef.idleClip;
            if (heroDef.walkClip) overrideController["Base_Run"] = heroDef.walkClip;

            // Apply overrides once
            animator.runtimeAnimatorController = overrideController;
            Debug.Log($"[NetworkHeroAnimator] FINISHED Initializing Overrides for {heroDef.name}. Total Overrides Count: {overrides.Count}");
        }
    }
}
