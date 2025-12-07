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
        
        private class SpawnerTracker
        {
            public Shared.NetworkSpawner Config;
            public int CurrentEntityId = -1;
            public float RespawnTimer = 0f;
        }
        private readonly List<SpawnerTracker> spawnerTrackers = new List<SpawnerTracker>();
        

        public IEnumerable<GameEntity> AllEntities => Repo.AllEntities;
        public IEnumerable<GameEntity> HeroEntities => heroEntities.Values;

        public GameEntityManager()
        {
        }

        public void InitializeMapSpawners(ServerWorld world)
        {

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
                var tracker = new SpawnerTracker { Config = spawner };
                spawnerTrackers.Add(tracker);

                if (spawner.entityType == EntityType.Neutral)
                {
                    UnityEngine.Debug.Log($"[GameEntityManager] Spawning Neutral '{spawner.archetypeId}' at {spawner.transform.position}");
                    var npc = CreateNeutralNpc(spawner.archetypeId, spawner.transform.position.x, spawner.transform.position.z);
                    tracker.CurrentEntityId = npc.Id;
                }
            }
        }

        public void Tick(ServerWorld world, float dt)
        {
            foreach (var tracker in spawnerTrackers)
            {
                if (tracker.CurrentEntityId != -1)
                {
                    if (!Repo.TryGetEntity(tracker.CurrentEntityId, out var entity) || !IsAlive(entity))
                    {
                        tracker.CurrentEntityId = -1;
                        tracker.RespawnTimer = tracker.Config.respawnTime;
                    }
                }
                else if (tracker.Config.respawnTime > 0f)
                {
                    tracker.RespawnTimer -= dt;
                    if (tracker.RespawnTimer <= 0f)
                    {
                        var spawner = tracker.Config;
                        if (spawner.entityType == EntityType.Neutral)
                        {
                            var npc = CreateNeutralNpc(spawner.archetypeId, spawner.transform.position.x, spawner.transform.position.z);
                            tracker.CurrentEntityId = npc.Id;
                            
                            var t = npc.GetComponent<TransformComponent>();
                            var team = npc.GetComponent<TeamComponent>();
                            world.EnqueueEvent(new EntitySpawnEvent
                            {
                                CasterId = npc.Id,
                                PosX = t.posX,
                                PosY = t.posY,
                                ArchetypeId = npc.ArchetypeId,
                                TeamId = team != null ? team.teamId : -1
                            });
                        }
                    }
                }
            }
        }
        
        private bool IsAlive(GameEntity entity)
        {
            if (entity.TryGetComponent(out HealthComponent h)) return h.IsAlive;
            return true;
        }

        public GameEntity EnsurePlayer(int id, string name, string heroId, ServerWorld world)
        {
            string resolvedHeroId = string.IsNullOrEmpty(heroId) ? ContentAssetRegistry.DefaultHeroId : heroId;
            
            if (!heroEntities.TryGetValue(id, out var entity))
            {
                entity = CreateHeroEntity(id, name, resolvedHeroId);
                var heroSo = GetHeroAsset(resolvedHeroId);
                AddHeroComponents(entity, heroSo, id);

                heroEntities[id] = entity;
                
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

        private GameEntity CreateHeroEntity(int id, string name, string heroId)
        {
            var entity = Repo.CreateEntity(EntityType.Hero, forcedId: id);
            entity.OwnerPlayerId = id;
            entity.Name = name ?? $"Player{id}";
            entity.ArchetypeId = heroId;
            return entity;
        }

        private HeroSO GetHeroAsset(string heroId)
        {
            return ContentAssetRegistry.Heroes != null && !string.IsNullOrEmpty(heroId) && ContentAssetRegistry.Heroes.TryGetValue(heroId, out var heroSo)
                    ? heroSo : null;
        }

        private void AddHeroComponents(GameEntity entity, HeroSO hero, int playerId)
        {
            // Transform & Spawn
            var spawnPos = GetSpawnPosition(playerId);
            entity.AddComponent(new TransformComponent { posX = spawnPos.x, posY = spawnPos.y });

            // Movement
            float moveSpeed = hero != null ? hero.moveSpeed : 3.5f;
            var movement = new MovementComponent();
            movement.moveSpeed = moveSpeed;
            movement.strategy = hero != null && hero.movementStrategy != null 
                ? hero.movementStrategy 
                : ContentAssetRegistry.DefaultMovementStrategy; 
            entity.AddComponent(movement);

            // Other Stats
            float hp = hero != null ? hero.baseHp : 500f;
            var health = new HealthComponent();
            health.Reset(hp);
            entity.AddComponent(health);

            entity.AddComponent(new TeamComponent { teamId = playerId });
            entity.AddComponent(new CombatComponent());
            entity.AddComponent(new CollisionComponent { radius = 0.5f });
            entity.AddComponent(new StatusEffectComponent());
        }

        private Vector2 GetSpawnPosition(int teamId)
        {
            var spawners = UnityEngine.Object.FindObjectsByType<Shared.NetworkSpawner>(UnityEngine.FindObjectsSortMode.None);
            foreach (var sp in spawners)
            {
                if (sp.entityType == EntityType.Hero && sp.teamId == teamId)
                {
                    return new Vector2(sp.transform.position.x, sp.transform.position.z);
                }
            }
            return Vector2.zero;
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
            
            var t = new TransformComponent { posX = posX, posY = posY };
            npc.AddComponent(t);

            float speed = config != null ? config.moveSpeed : 2f;
            var movement = new MovementComponent { moveSpeed = speed };
            movement.strategy = config != null && config.movementStrategy != null 
                ? config.movementStrategy 
                : ContentAssetRegistry.DefaultMovementStrategy;
            npc.AddComponent(movement);
            
            float hp = config != null ? config.baseHp : 400f;
            var h = new HealthComponent();
            h.Reset(hp);
            npc.AddComponent(h);

            npc.AddComponent(new TeamComponent { teamId = -1 });
            npc.AddComponent(new AIBehaviorComponent());
            npc.AddComponent(new CollisionComponent { radius = 0.5f });
            npc.AddComponent(new StatusEffectComponent());

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
