using UnityEngine;
using System.IO;
using ServerGame.Entities;
using System.Collections.Generic;
using ClientContent;

namespace Client.Replicator
{
    public class NetworkStatusEffectsVisual : MonoBehaviour, INetworkComponentVisual
    {
        public int TargetComponentType => (int)ComponentType.StatusEffect;

        private readonly HashSet<string> currentEffects = new HashSet<string>();

        public void OnNetworkUpdate(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            var serverEffects = new HashSet<string>();

            for (int i = 0; i < count; i++)
            {
                string effectId = reader.ReadString();
                float remainingTime = reader.ReadSingle();
                int casterId = reader.ReadInt32();

                serverEffects.Add(effectId);

                // Fetch Asset
                if (ContentAssetRegistry.Effects.TryGetValue(effectId, out var effectAsset))
                {
                    if (!currentEffects.Contains(effectId))
                    {
                        // NEW
                        effectAsset.ClientOnStart(gameObject);
                    }
                    else
                    {
                        // EXISTING
                        effectAsset.ClientOnTick(gameObject, Time.deltaTime); // Approximate DT
                    }
                }
            }

            // Check for Removed
            foreach (var oldId in currentEffects)
            {
                if (!serverEffects.Contains(oldId))
                {
                    if (ContentAssetRegistry.Effects.TryGetValue(oldId, out var effectAsset))
                    {
                        effectAsset.ClientOnRemove(gameObject);
                    }
                }
            }

            currentEffects.Clear();
            foreach(var id in serverEffects) currentEffects.Add(id);
        }
    }
}
