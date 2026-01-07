using System.Collections.Generic;
using UnityEngine;

namespace Client.Replicator
{
    public static class CastingVfxHandover
    {
        private class Entry
        {
            public string ArchetypeId;
            public GameObject Instance;
            public Vector2 Position; // Position where the cast expected the entity to be
            public float Timestamp;
        }

        private static List<Entry> entries = new List<Entry>();
        private const float TIMEOUT = 1.0f; // Seconds to keep an orphan alive waiting for a spawn

        public static void Register(string archetypeId, GameObject instance, Vector2 position)
        {
            if (instance == null) return;
            
            // Clean up old entries
            float now = Time.time;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (now - entries[i].Timestamp > TIMEOUT)
                {
                    if (entries[i].Instance) Object.Destroy(entries[i].Instance);
                    entries.RemoveAt(i);
                }
            }

            // Register new
            entries.Add(new Entry 
            {
                ArchetypeId = archetypeId,
                Instance = instance,
                Position = position,
                Timestamp = now
            });
        }

        public static bool TryClaim(string archetypeId, Vector2 spawnPos, out GameObject instance)
        {
            instance = null;
            float now = Time.time;
            
            int bestIndex = -1;
            float bestDistSq = 1.0f; // Max distance to match (squared) - e.g. 1 unit tolerance

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                // Check Timeout
                if (now - e.Timestamp > TIMEOUT)
                {
                    if (e.Instance) Object.Destroy(e.Instance);
                    entries.RemoveAt(i);
                    continue;
                }

                // Check Match
                if (e.ArchetypeId == archetypeId)
                {
                    float d2 = (e.Position - spawnPos).sqrMagnitude;
                    if (d2 < bestDistSq)
                    {
                        bestDistSq = d2;
                        bestIndex = i;
                    }
                }
            }

            if (bestIndex != -1)
            {
                instance = entries[bestIndex].Instance;
                entries.RemoveAt(bestIndex);
                
                // Ensure instance is still valid (it might have been destroyed externally)
                if (instance == null) return false;
                
                return true;
            }

            return false;
        }
    }
}
