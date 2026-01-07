using UnityEngine;
using UnityEngine.UI;
using ServerGame.Entities;
using System.IO;

namespace Client.Replicator
{
    public class NetworkCastBar : MonoBehaviour, INetworkComponentVisual
    {
        public int TargetComponentType => (int)ComponentType.Casting;

        [Header("UI References")]
        public Slider slider;
        public CanvasGroup canvasGroup; // Optional: For cleaner fade in/out

        private bool isCasting;
        private float timer;
        private float totalTime;

        private NetEntityView view;

        void Awake()
        {
            view = GetComponent<NetEntityView>();
            UpdateVisibility(false);
        }

        void Start()
        {
            var clientNet = FindFirstObjectByType<ClientNetwork>();
            if (view != null && clientNet != null)
            {
                if (clientNet.clientPlayerId == view.entityId)
                {
                    // Local Player: Find the UI Castbar in the Scene
                    var go = GameObject.FindGameObjectWithTag("castbar");
                    if (go)
                    {
                        slider = go.GetComponent<Slider>();
                        if (slider) canvasGroup = slider.GetComponent<CanvasGroup>();
                    }
                }
                // Remote Players: Do nothing (slider remains null), so no UI is shown.
            }
        }

        private GameObject currentPreviewVfx;
        private GameObject currentCasterVfx;
        private string lastAbilityId = "";

        void OnDisable()
        {
            if (currentPreviewVfx) Destroy(currentPreviewVfx);
            if (currentCasterVfx) Destroy(currentCasterVfx);
        }

        private float targetX, targetY; // Store target for placement

        public void OnNetworkUpdate(BinaryReader reader)
        {
            bool wasCasting = isCasting;
            isCasting = reader.ReadBoolean();
            
            if (isCasting)
            {
                string abilityId = reader.ReadString();
                timer = reader.ReadSingle();
                totalTime = reader.ReadSingle();
                targetX = reader.ReadSingle();
                targetY = reader.ReadSingle();

                // Handle VFX Start
                if (!wasCasting || abilityId != lastAbilityId)
                {
                    // Cleanup old
                    if (currentPreviewVfx) Destroy(currentPreviewVfx);
                    if (currentCasterVfx) Destroy(currentCasterVfx);
                    
                    if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(abilityId, out var ability))
                    {
                        // 1. Caster VFX (Glowing Hands, Magic Circle)
                        if (ability.castingEffectPrefab != null)
                        {
                            currentCasterVfx = Instantiate(ability.castingEffectPrefab, transform);
                            currentCasterVfx.transform.localPosition = Vector3.zero;
                            currentCasterVfx.transform.localRotation = Quaternion.identity;
                        }

                        // 2. Main Prefab Preview (Windup)
                        if (ability.castingPreviewMode != ClientContent.AbilityAsset.CastingPreviewMode.None)
                        {
                            var prefab = ability.GetPreviewPrefab();
                            if (prefab != null)
                            {
                                if (ability.castingPreviewMode == ClientContent.AbilityAsset.CastingPreviewMode.MainPrefabAtCaster)
                                {
                                    // Parented to Caster
                                    currentPreviewVfx = Instantiate(prefab, transform);
                                    currentPreviewVfx.transform.localPosition = Vector3.zero;
                                    currentPreviewVfx.transform.localRotation = Quaternion.identity;
                                }
                                else if (ability.castingPreviewMode == ClientContent.AbilityAsset.CastingPreviewMode.MainPrefabAtTarget)
                                {
                                    // At Target Location (Unparented)
                                    currentPreviewVfx = Instantiate(prefab, new Vector3(targetX, 0, targetY), Quaternion.identity);
                                }
                                else if (ability.castingPreviewMode == ClientContent.AbilityAsset.CastingPreviewMode.MainPrefabAtCasterNoFollow)
                                {
                                    // At Caster Location (Unparented)
                                    currentPreviewVfx = Instantiate(prefab, transform.position, Quaternion.identity);
                                }
                            }
                        }
                    }
                }
                lastAbilityId = abilityId;
            }
            else
            {
                // Cast Ended (Finished or Interrupted)
                if (wasCasting)
                {
                   // Determine if we should Handover or Destroy
                   // Heuristic: If timer was low relative to frame time, it finished. 
                   // If timer was high, it was likely interrupted.
                   // Using a small threshold like 0.1s. 
                   // (Note: Server tick rate impacts this. If server tick is 20Hz (0.05s))
                   
                   bool handedOver = false;
                   if (timer <= 0.1f && lastAbilityId != "")
                   {
                       if (ClientContent.ContentAssetRegistry.Abilities.TryGetValue(lastAbilityId, out var ability))
                       {
                           if (ability.castingPreviewMode != ClientContent.AbilityAsset.CastingPreviewMode.None && currentPreviewVfx != null)
                           {
                               // Calculate expected spawn position
                               Vector2 spawnPos = Vector2.zero;
                               if (ability.castingPreviewMode == ClientContent.AbilityAsset.CastingPreviewMode.MainPrefabAtCaster)
                                   spawnPos = new Vector2(transform.position.x, transform.position.z);
                               else if (ability.castingPreviewMode == ClientContent.AbilityAsset.CastingPreviewMode.MainPrefabAtTarget)
                                   spawnPos = new Vector2(targetX, targetY);
                               else if (ability.castingPreviewMode == ClientContent.AbilityAsset.CastingPreviewMode.MainPrefabAtCasterNoFollow)
                                   spawnPos = new Vector2(currentPreviewVfx.transform.position.x, currentPreviewVfx.transform.position.z); // Use actual position
                               
                               // Handover!
                               CastingVfxHandover.Register(ability.id, currentPreviewVfx, spawnPos);
                               currentPreviewVfx = null; // Orphan it so we don't destroy it below
                               handedOver = true;
                           }
                       }
                   }
                }

                timer = 0;
                totalTime = 0;
                if (currentPreviewVfx) Destroy(currentPreviewVfx);
                if (currentCasterVfx) Destroy(currentCasterVfx);
            }
        }

        void Update()
        {
            // Client-side smoothing could go here, but for now direct update is safest
            if (isCasting && totalTime > 0)
            {
                UpdateVisibility(true);
                if (slider != null)
                {
                    // Progress goes from 0 to 1
                    float progress = 1f - (timer / totalTime);
                    slider.value = Mathf.Clamp01(progress);
                }
                
                // Simulate local timer countdown between network ticks for smoothness
                timer -= Time.deltaTime;
                if (timer < 0) timer = 0;

                // Update Preview Position? 
                // Usually target is fixed for cast time. If we allow "homing" casts, we'd need to update.
                // For now, assume static target position snapshot at start.
            }
            else
            {
                UpdateVisibility(false);
                // Also ensure VFX is gone if we somehow missed a state update (safety)
                 if (currentPreviewVfx) Destroy(currentPreviewVfx);
                 if (currentCasterVfx) Destroy(currentCasterVfx);
            }
        }

        private void UpdateVisibility(bool visible)
        {
            if (slider != null) slider.gameObject.SetActive(visible);
            
            // If using CanvasGroup for fading (Optional polish)
            /*
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
            }
            */
        }
    }
}
