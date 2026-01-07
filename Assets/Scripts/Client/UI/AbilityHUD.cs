using UnityEngine;
using UnityEngine.UI;
using Client.Replicator;
using TMPro;

namespace Client.UI
{
    public class AbilityHUD : MonoBehaviour
    {
        [Header("Cooldown Overlays (Radial Fill Images)")]
        public Image overlayQ;
        public Image overlayW;
        public Image overlayE;
        public Image overlayR;

        [Header("Text Timers (Optional)")]
        public TMP_Text textQ;
        public TMP_Text textW;
        public TMP_Text textE;
        public TMP_Text textR;

        private NetworkCooldownData cachedData;
        private int lastCheckedId = -1;

        void Update()
        {
            var clientNet = FindFirstObjectByType<ClientNetwork>();
            if (clientNet == null) return;

            int localId = clientNet.AssignedPlayerId;
            if (localId == 0) return;

            // Cache lookup
            if (cachedData == null || lastCheckedId != localId)
            {
                if (NetEntityView.AllViews.TryGetValue(localId, out var view))
                {
                    cachedData = view.GetComponent<NetworkCooldownData>();
                    lastCheckedId = localId;
                }
            }

            if (cachedData != null)
            {
                UpdateSlot(overlayQ, textQ, cachedData.cdQ, cachedData.maxQ);
                UpdateSlot(overlayW, textW, cachedData.cdW, cachedData.maxW);
                UpdateSlot(overlayE, textE, cachedData.cdE, cachedData.maxE);
                UpdateSlot(overlayR, textR, cachedData.cdR, cachedData.maxR);
            }
        }

        private void UpdateSlot(Image overlay, TMP_Text text, float current, float max)
        {
            if (overlay == null) return;

            if (current > 0 && max > 0)
            {
                float ratio = current / max;
                overlay.fillAmount = ratio;
                overlay.gameObject.SetActive(true);

                if (text != null)
                {
                    text.gameObject.SetActive(true);
                    // Format nicely: 0.5s or 1.2s
                    text.text = current.ToString("0.0"); 
                }
            }
            else
            {
                overlay.fillAmount = 0;
                overlay.gameObject.SetActive(false); // Hide overlay when ready
                if (text != null) text.gameObject.SetActive(false);
            }
        }
    }
}
