using System;
using System.Collections.Generic;

namespace ServerGame
{
    public class ServerWorld
    {
        public const float HitFlashDuration = 0.2f;
        public readonly Dictionary<int, ServerPlayer> Players = new Dictionary<int, ServerPlayer>();
        public readonly Dictionary<int, AbilityEffect> AbilityEffects = new Dictionary<int, AbilityEffect>();
        public readonly ServerNPC Npc = new ServerNPC { id = 999, posX = 2f, posY = 0f, speed = 2.0f };

        private int nextEffectId = 1;
        private readonly List<(int id, string abilityId)> recentlyDespawnedEffects = new List<(int id, string abilityId)>();
        private readonly List<int> recentlySpawnedEffects = new List<int>();

        // Per-player ability book: key (Q,W,E,R) -> AbilityAsset
        public readonly Dictionary<int, Dictionary<string, ClientContent.AbilityAsset>> AbilityBooks = new Dictionary<int, Dictionary<string, ClientContent.AbilityAsset>>();

        public ServerPlayer EnsurePlayer(int id, string name = null)
        {
            if (!Players.TryGetValue(id, out var p))
            {
                p = new ServerPlayer { playerId = id, name = name ?? $"Player{id}", posX = 0f, posY = 0f, speed = 3.5f };
                Players[id] = p;
                // attach default abilities on first creation via asset registry
                if (!AbilityBooks.ContainsKey(id))
                    AbilityBooks[id] = ClientContent.AbilityAssetRegistry.GetDefaultBindings();
            }
            else if (!string.IsNullOrEmpty(name))
            {
                p.name = name;
            }
            return p;
        }

        public void HandleMove(int playerId, float targetX, float targetY)
        {
            var p = EnsurePlayer(playerId);
            p.destX = targetX;
            p.destY = targetY;
            p.hasDest = true;
        }

        public int RegisterAbilityEffect(AbilityEffect effect, bool markSpawn)
        {
            int id = effect.id;
            if (id == 0) id = nextEffectId++;
            effect.id = id;
            AbilityEffects[id] = effect;
            if (markSpawn) MarkEffectSpawned(id);
            return id;
        }

        // Instant (non-persistent) ability events queue
        private readonly List<AbilityEventMessage> pendingAbilityEvents = new List<AbilityEventMessage>();
        public void EnqueueAbilityEvent(AbilityEventMessage ev) => pendingAbilityEvents.Add(ev);
        public List<AbilityEventMessage> ConsumePendingAbilityEvents()
        {
            var copy = new List<AbilityEventMessage>(pendingAbilityEvents);
            pendingAbilityEvents.Clear();
            return copy;
        }

        public void Simulate(float dt)
        {
            // Systems are invoked externally now; this remains as a convenience if needed.
            movementSystem ??= new Systems.MovementSystem();
            npcSystem ??= new Systems.NpcSystem();
            abilitySystem ??= new Systems.AbilitySystem();
            abilityEffectSystem ??= new Systems.AbilityEffectSystem();

            movementSystem.Tick(this, dt);
            npcSystem.Tick(this, dt);
            abilitySystem.Tick(this, dt);
            abilityEffectSystem.Tick(this, dt);
        }

        private Systems.MovementSystem movementSystem;
        private Systems.NpcSystem npcSystem;
        private Systems.AbilitySystem abilitySystem;
        private Systems.AbilityEffectSystem abilityEffectSystem;

        public IReadOnlyList<(int id, string abilityId)> ConsumeRecentlyDespawnedEffects()
        {
            var copy = new List<(int id, string abilityId)>(recentlyDespawnedEffects);
            recentlyDespawnedEffects.Clear();
            return copy;
        }

        public void MarkEffectDespawned(int id, string abilityId)
        {
            recentlyDespawnedEffects.Add((id, abilityId));
        }

        public IReadOnlyList<int> ConsumeRecentlySpawnedEffects()
        {
            var copy = new List<int>(recentlySpawnedEffects);
            recentlySpawnedEffects.Clear();
            return copy;
        }

        public void MarkEffectSpawned(int id)
        {
            recentlySpawnedEffects.Add(id);
        }

        // Convenience entrypoint for server to request a cast immediately
        public bool TryCastAbility(int playerId, string key, float targetX, float targetY)
        {
            abilitySystem ??= new Systems.AbilitySystem();
            return abilitySystem.TryCast(this, playerId, key, targetX, targetY);
        }
    }

    public class ServerPlayer
    {
        public int playerId;
        public string name;
        public float posX, posY;
        public float rotZ;
        public float speed = 3.5f;
        public float destX, destY;
        public bool hasDest = false;
        public bool hit = false;
        public float hitTimer = 0f;
    }

    public class ServerNPC
    {
        public int id;
        public float posX, posY;
        public float speed = 3f;
        public float followRange = 6f;
        public float stopRange = 2f;
        public ServerPlayer target = null;
    }
}
