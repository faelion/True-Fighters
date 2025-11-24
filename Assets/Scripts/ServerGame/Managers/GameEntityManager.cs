using System.Collections.Generic;
using UnityEngine;
using ServerGame.Entities;
using ClientContent;

namespace ServerGame.Managers
{
    public class GameEntityManager
    {
        public readonly EntityRepository Repo = new EntityRepository();
        private readonly Dictionary<int, GameEntity> heroEntities = new Dictionary<int, GameEntity>();
        
        // Accessor for all entities (forwarded from Repo)
        public IEnumerable<GameEntity> AllEntities => Repo.AllEntities;
        public IEnumerable<GameEntity> HeroEntities => heroEntities.Values;

        public GameEntityManager()
        {
        }

        public void InitializeMapSpawners(ServerWorld world)
        {
            // FindObjectsByType is the modern API. We include inactive objects just in case.
            var spawners = UnityEngine.Object.FindObjectsByType<Shared.NetworkSpawner>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            
            UnityEngine.Debug.Log($"[GameEntityManager] Found {spawners.Length} NetworkSpawners in scene.");
            
            if (spawners.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[GameEntityManager] No NetworkSpawners found! Creating a fallback neutral NPC at (0,0).");
                CreateNeutralNpc(ContentAssetRegistry.DefaultNeutralId, 2f, 0f);
                return;
            }

            foreach (var spawner in spawners)
            {
                if (spawner.entityType == EntityType.Neutral)
                {
                    UnityEngine.Debug.Log($"[GameEntityManager] Spawning Neutral '{spawner.archetypeId}' at {spawner.transform.position}");
                    CreateNeutralNpc(spawner.archetypeId, spawner.transform.position.x, spawner.transform.position.z);
                }
            }
        }

        public GameEntity EnsurePlayer(int id, string name, string heroId, ServerWorld world)
        {
            string resolvedHeroId = string.IsNullOrEmpty(heroId) ? ContentAssetRegistry.DefaultHeroId : heroId;
            if (!heroEntities.TryGetValue(id, out var entity))
            {
                entity = Repo.CreateEntity(EntityType.Hero, forcedId: id);
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

                var hero = ContentAssetRegistry.Heroes != null && !string.IsNullOrEmpty(resolvedHeroId) && ContentAssetRegistry.Heroes.TryGetValue(resolvedHeroId, out var heroSo)
                    ? heroSo : null;
                float hp = hero != null ? hero.baseHp : 500f;
                float moveSpeed = hero != null ? hero.baseMoveSpeed : 3.5f;
                entity.Health.Reset(hp);
                entity.Movement.moveSpeed = moveSpeed;
                entity.Transform.posX = spawnX;
                entity.Transform.posY = spawnY;

                heroEntities[id] = entity;
                
                // Initialize abilities in World (or AbilityManager if we had one)
                if (!world.AbilityBooks.ContainsKey(id))
                    world.AbilityBooks[id] = ContentAssetRegistry.GetBindingsForHero(resolvedHeroId);
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

        public GameEntity CreateNeutralNpc(string neutralId, float posX, float posY)
        {
            var config = ContentAssetRegistry.GetNeutral(neutralId);
            var npc = Repo.CreateEntity(EntityType.Neutral);
            npc.ArchetypeId = config != null ? config.id : neutralId;
            npc.Transform.posX = posX;
            npc.Transform.posY = posY;
            npc.Movement.moveSpeed = config != null ? config.moveSpeed : 2f;
            npc.Health.Reset(config != null ? config.baseHp : 400f);
            npc.Team.teamId = -1;
            npc.Npc = new NpcComponent();
            return npc;
        }

        public void DespawnEntity(int entityId, ServerWorld world)
        {
            if (Repo.TryGetEntity(entityId, out var entity))
            {
                Repo.Remove(entityId);
                if (entity.Type == EntityType.Hero)
                {
                    heroEntities.Remove(entity.OwnerPlayerId);
                }
                world.EnqueueEvent(new EntityDespawnEvent { CasterId = entityId });
            }
        }

        public bool TryGetEntity(int entityId, out GameEntity entity) => Repo.TryGetEntity(entityId, out entity);
    }
}
