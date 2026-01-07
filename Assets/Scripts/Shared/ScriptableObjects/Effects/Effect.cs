using System;
using UnityEngine;
using ServerGame;
using ServerGame.Entities;

namespace Shared.Effects
{
    public abstract class Effect : ScriptableObject
    {
        // Identification
        public string id;
        public float Duration = 0f;

        public virtual void Apply(ServerWorld world, GameEntity source, GameEntity target, Vector3? targetPos = null)
        {
            if (target.TryGetComponent(out StatusEffectComponent status))
            {
                status.AddEffect(this, Duration, source, targetPos);
            }
        }

        // --- Server Logic ---
        public virtual void OnStart(ServerWorld world, ActiveEffect runtime, GameEntity target) { }
        public virtual void OnTick(ServerWorld world, ActiveEffect runtime, GameEntity target, float dt) { }
        public virtual void OnRemove(ServerWorld world, ActiveEffect runtime, GameEntity target) { }

        // --- Client Visuals ---
        [Header("Client Visuals")]
        public GameObject vfxPrefab;
        [Tooltip("If true, the Vfx instance is destroyed when the effect ends. If false, it acts as a 'One-Shot' and the instance must destroy itself.")]
        public bool destroyVfxOnEnd = true;
        [Tooltip("If DestroyVfxOnEnd is false, this defines how long the VFX lasts. If 0, it relies on the Prefab to destroy itself.")]
        public float vfxDuration = 0f;

        // Runtime dictionary to track instances: Key = Target GameObject InstanceID
        private System.Collections.Generic.Dictionary<int, GameObject> activeVfxInstances = new System.Collections.Generic.Dictionary<int, GameObject>();

        public virtual void ClientOnStart(GameObject targetVisuals) 
        { 
            if (vfxPrefab != null)
            {
                // Determine parent
                Transform parent = targetVisuals.transform;
                
                var instance = Instantiate(vfxPrefab, parent);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;

                // Handle Timed Destruction for One-Shots
                if (!destroyVfxOnEnd && vfxDuration > 0f)
                {
                    Destroy(instance, vfxDuration);
                }

                int id = targetVisuals.GetInstanceID();
                if (activeVfxInstances.ContainsKey(id))
                {
                    // Cleanup collision (shouldn't happen with correct logic but safety first)
                    // If overwrite happens, we force destroy the old one regardless of flag to avoid leak
                    if (activeVfxInstances[id] != null) Destroy(activeVfxInstances[id]);
                    activeVfxInstances.Remove(id);
                }
                activeVfxInstances[id] = instance;
            }
        }
        
        public virtual void ClientOnTick(GameObject targetVisuals, float dt) { }
        
        public virtual void ClientOnRemove(GameObject targetVisuals) 
        { 
            int id = targetVisuals.GetInstanceID();
            if (activeVfxInstances.TryGetValue(id, out var instance))
            {
                if (instance != null && destroyVfxOnEnd) 
                {
                    Destroy(instance);
                }
                // If destroyVfxOnEnd is false, we just 'forget' it. The prefab must destroy itself.
                activeVfxInstances.Remove(id);
            }
        }
    }
}
