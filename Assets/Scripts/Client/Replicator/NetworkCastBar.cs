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

        public void OnNetworkUpdate(BinaryReader reader)
        {
            isCasting = reader.ReadBoolean();
            if (isCasting)
            {
                string abilityId = reader.ReadString();
                timer = reader.ReadSingle();
                totalTime = reader.ReadSingle();
            }
            else
            {
                timer = 0;
                totalTime = 0;
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
            }
            else
            {
                UpdateVisibility(false);
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
