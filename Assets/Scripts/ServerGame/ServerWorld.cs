using System;
using System.Collections.Generic;
using ServerGame.Entities;
using ClientContent;

namespace ServerGame
{
    public class ServerWorld
    {
        public const float HitFlashDuration = 0.2f;
        public readonly Dictionary<int, AbilityEffect> AbilityEffects = new Dictionary<int, AbilityEffect>();
        public readonly EntityRepository EntityRepo = new EntityRepository();
        // public GameEntity NeutralNpc { get; } // Removed hardcoded NPC

        private readonly Dictionary<int, GameEntity> heroEntities = new Dictionary<int, GameEntity>();
        private readonly List<IGameEvent> pendingEvents = new List<IGameEvent>();
        private int nextEffectId = 1;

        // Per-player ability book: key (Q,W,E,R) -> AbilityAsset
        public readonly Dictionary<int, Dictionary<string, ClientContent.AbilityAsset>> AbilityBooks = new Dictionary<int, Dictionary<string, ClientContent.AbilityAsset>>();

        public ServerWorld()
        {
            // NeutralNpc = CreateNeutralNpc(ContentAssetRegistry.DefaultNeutralId, 2f, 0f);
            SpawnMapEntities();
        }

        private void SpawnMapEntities()
        {
            // FindObjectsByType is the modern API. We include inactive objects just in case.
            var spawners = UnityEngine.Object.FindObjectsByType<Shared.NetworkSpawner>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            
            UnityEngine.Debug.Log($"[ServerWorld] Found {spawners.Length} NetworkSpawners in scene.");
            
            if (spawners.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[ServerWorld] No NetworkSpawners found! Creating a fallback neutral NPC at (0,0).");
                CreateNeutralNpc(ClientContent.ContentAssetRegistry.DefaultNeutralId, 2f, 0f);
                return;
            }

            foreach (var spawner in spawners)
            {
                if (spawner.entityType == EntityType.Neutral)
                {
                    UnityEngine.Debug.Log($"[ServerWorld] Spawning Neutral '{spawner.archetypeId}' at {spawner.transform.position}");
                    CreateNeutralNpc(spawner.archetypeId, spawner.transform.position.x, spawner.transform.position.z);
                }
                // We can also store player spawn points here if needed
            }
        }

        public GameEntity EnsurePlayer(int id, string name = null, string heroId = null)
        {
            string resolvedHeroId = string.IsNullOrEmpty(heroId) ? ClientContent.ContentAssetRegistry.DefaultHeroId : heroId;
            if (!heroEntities.TryGetValue(id, out var entity))
            {
                entity = EntityRepo.CreateEntity(EntityType.Hero, forcedId: id);
                entity.OwnerPlayerId = id;
                entity.Name = name ?? $"Player{id}";
                entity.Team.teamId = id;
                entity.ArchetypeId = resolvedHeroId;
                
                // Find spawn point
                var spawners = UnityEngine.Object.FindObjectsByType<Shared.NetworkSpawner>(UnityEngine.FindObjectsSortMode.None);
                float spawnX = 0f, spawnY = 0f;
                foreach (var sp in spawners)
                {
                    if (sp.entityType == EntityType.Hero) // Could check teamId here
                    {
                        spawnX = sp.transform.position.x;
                        spawnY = sp.transform.position.z; // Using Z as Y in 2D logic
                        break;
                    }
                }

                var hero = ClientContent.ContentAssetRegistry.Heroes != null && !string.IsNullOrEmpty(resolvedHeroId) && ClientContent.ContentAssetRegistry.Heroes.TryGetValue(resolvedHeroId, out var heroSo)
                    ? heroSo : null;
                float hp = hero != null ? hero.baseHp : 500f;
                float moveSpeed = hero != null ? hero.baseMoveSpeed : 3.5f;
                entity.Health.Reset(hp);
                entity.Movement.moveSpeed = moveSpeed;
                entity.Transform.posX = spawnX;
                entity.Transform.posY = spawnY;

                heroEntities[id] = entity;
                // attach default abilities on first creation via asset registry
                if (!AbilityBooks.ContainsKey(id))
                    AbilityBooks[id] = ClientContent.ContentAssetRegistry.GetBindingsForHero(resolvedHeroId);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                entity.Name = name;
                entity.ArchetypeId = resolvedHeroId;
            }
            return entity;
        }

        public GameEntity GetHeroEntity(int playerId)
        {
            heroEntities.TryGetValue(playerId, out var entity);
            return entity;
        }

        public IEnumerable<GameEntity> HeroEntities => heroEntities.Values;

        public GameEntity CreateNeutralNpc(string neutralId, float posX, float posY)
        {
            var config = ClientContent.ContentAssetRegistry.GetNeutral(neutralId);
            var npc = EntityRepo.CreateEntity(EntityType.Neutral);
            npc.ArchetypeId = config != null ? config.id : neutralId;
            npc.Transform.posX = posX;
            npc.Transform.posY = posY;
            npc.Movement.moveSpeed = config != null ? config.moveSpeed : 2f;
            npc.Health.Reset(config != null ? config.baseHp : 400f);
            npc.Team.teamId = -1;
            npc.Npc = new NpcComponent();
            return npc;
        }

        public void DespawnEntity(int entityId)
        {
            if (EntityRepo.TryGetEntity(entityId, out var entity))
            {
                EntityRepo.Remove(entityId);
                if (entity.Type == EntityType.Hero)
                {
                    heroEntities.Remove(entity.OwnerPlayerId);
                }
                EnqueueEvent(new EntityDespawnEvent { CasterId = entityId });
            }
        }

        public bool TryGetEntity(int entityId, out GameEntity entity) => EntityRepo.TryGetEntity(entityId, out entity);

        public void HandleMove(int playerId, float targetX, float targetY)
        {
            var entity = EnsurePlayer(playerId);
            entity.Movement.destX = targetX;
            entity.Movement.destY = targetY;
            entity.Movement.hasDestination = true;
        }

        public int RegisterAbilityEffect(AbilityEffect effect, ClientContent.AbilityAsset sourceAsset = null)
        {
            int id = effect.id;
            if (id == 0) id = nextEffectId++;
            effect.id = id;
            AbilityEffects[id] = effect;
            sourceAsset?.OnEffectSpawn(this, effect);
            return id;
        }

        // Instant (non-persistent) ability events queue
        public void EnqueueEvent(IGameEvent ev) => pendingEvents.Add(ev);
        public List<IGameEvent> ConsumePendingEvents()
        {
            var copy = new List<IGameEvent>(pendingEvents);
            pendingEvents.Clear();
            return copy;
        }

        // Convenience entrypoint for server to request a cast immediately
        public bool TryCastAbility(int playerId, string key, float targetX, float targetY)
        {
            if (abilitySystem == null) return false;
            return abilitySystem.TryCast(this, playerId, key, targetX, targetY);
        }

        internal void BindAbilitySystem(Systems.AbilitySystem system) => abilitySystem = system;

        private Systems.AbilitySystem abilitySystem;
    }
}
