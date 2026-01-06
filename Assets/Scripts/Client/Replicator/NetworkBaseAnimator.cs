using UnityEngine;
using System.Collections.Generic;

namespace Client.Replicator
{
    public abstract class NetworkBaseAnimator : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] protected Animator animator;

        [Header("Parameters")]
        [SerializeField] protected string speedParam = "Speed";
        [SerializeField] protected float runSpeedThreshold = 0.1f;
        [SerializeField] protected float smoothTime = 0.1f;

        [Header("Override Config")]
        [Tooltip("Dummy clips in the controller to be replaced by Ability clips.")]
        [SerializeField] protected AnimationClip[] slotPlaceholders;

        private Vector3 lastPos;
        private float currentSpeed;
        private float speedVel;

        protected virtual void Awake()
        {
            TryFindAnimator();
            lastPos = transform.position;
        }

        protected virtual void Update()
        {
            if (animator)
            {
                // Calculate speed only on XZ plane to avoid Y-jitter (gravity/jumping)
                Vector3 curPos = transform.position;
                float dist = Vector2.Distance(new Vector2(curPos.x, curPos.z), new Vector2(lastPos.x, lastPos.z));
                float instantSpeed = dist / Time.deltaTime;

                float targetSpeed = (instantSpeed > runSpeedThreshold) ? 1f : 0f;

                currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVel, smoothTime);
                animator.SetFloat(speedParam, currentSpeed);

                lastPos = curPos;
            }
        }

        public void TryFindAnimator()
        {
            if (!animator) animator = GetComponentInChildren<Animator>();
            if (animator)
            {
                animator.applyRootMotion = false;
            }
        }

        /// <summary>
        /// Fires the trigger associated with a specific slot index (0-based).
        /// Slot 0 triggers "Base_Action_Slot1", Slot 1 triggers "Base_Action_Slot2", etc.
        /// </summary>
        protected void FireSlotTrigger(int slotIndex)
        {
            if (!animator) return;
            // Map 0 -> Slot1
            string triggerName = $"Base_Action_Slot{slotIndex + 1}";
            animator.SetTrigger(triggerName);
            Debug.Log($"[NetworkAnimator] Fired Trigger '{triggerName}'.");
        }

        /// <summary>
        /// Applies overrides to the current animator.
        /// </summary>
        /// <param name="baseController">The template controller.</param>
        /// <param name="slotClips">List of clips to put into the slots.</param>
        /// <param name="idleClip">Specific Idle clip override.</param>
        /// <param name="walkClip">Specific Walk clip override.</param>
        protected void ApplyOverrides(RuntimeAnimatorController baseController, List<AnimationClip> slotClips, AnimationClip idleClip, AnimationClip walkClip)
        {
            if (!animator || baseController == null) return;
            
            // If the prefab already has a controller, we might use it as base if none provided, 
            // but usually we rely on the one passed in.
            
            var overrideController = new AnimatorOverrideController(baseController);
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

            // 1. Map Slots
            if (slotClips != null)
            {
                for (int i = 0; i < slotClips.Count; i++)
                {
                    if (slotPlaceholders != null && i < slotPlaceholders.Length && slotPlaceholders[i] != null)
                    {
                        var clip = slotClips[i];
                        if (clip != null)
                        {
                            overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(slotPlaceholders[i], clip));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[NetworkBaseAnimator] Cannot map Slot {i} (Clip: {slotClips[i]?.name}). Missing Placeholder in Inspector.");
                    }
                }
            }

            overrideController.ApplyOverrides(overrides);

            // 2. Map Base Movement (String based)
            // We use string indexing for these standard names "Base_Idle", "Base_Run"
            // This assumes the Controller has them.
            if (idleClip) overrideController["Base_Idle"] = idleClip;
            if (walkClip) overrideController["Base_Run"] = walkClip;

            animator.runtimeAnimatorController = overrideController;
        }
    }
}
