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
        private Dictionary<int, int> gameModeToSceneTeamMap = new Dictionary<int, int>();
        private List<Shared.NetworkSpawner> playerSpawnersCache = new List<Shared.NetworkSpawner>();
        private int ffaSpawnIndex = 0;
        
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
            spawnerTrackers.Clear();
            gameModeToSceneTeamMap.Clear();
            playerSpawnersCache.Clear();
            ffaSpawnIndex = 0;

            var spawners = UnityEngine.Object.FindObjectsByType<Shared.NetworkSpawner>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            
            UnityEngine.Debug.Log($"[GameEntityManager] Found {spawners.Length} NetworkSpawners in scene.");
            
            if (spawners.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[GameEntityManager] No NetworkSpawners found! Creating a fallback neutral NPC at (0,0).");
                CreateNeutralNpc(ContentAssetRegistry.DefaultNeutralId, 2f, 0f);
                return;
            }

            // 1. Analyze Scene Spawners
            HashSet<int> scenePlayerTeamIds = new HashSet<int>();
            Dictionary<int, int> playerSpawnerCounts = new Dictionary<int, int>();

            foreach (var spawner in spawners)
            {
                var tracker = new SpawnerTracker { Config = spawner };
                spawnerTrackers.Add(tracker);

                if (spawner.spawnerType == Shared.SpawnerType.Npc)
                {
                    UnityEngine.Debug.Log($"[GameEntityManager] Spawning Neutral '{spawner.archetypeId}' at {spawner.transform.position}");
                    var npc = CreateNeutralNpc(spawner.archetypeId, spawner.transform.position.x, spawner.transform.position.z);
                    tracker.CurrentEntityId = npc.Id;
                }
                else if (spawner.spawnerType == Shared.SpawnerType.Player)
                {
                    if (!playerSpawnerCounts.ContainsKey(spawner.teamId)) playerSpawnerCounts[spawner.teamId] = 0;
                    playerSpawnerCounts[spawner.teamId]++;
                    scenePlayerTeamIds.Add(spawner.teamId);
                    playerSpawnersCache.Add(spawner);
                }
            }
            
            // Sort cache for deterministic round-robin
            playerSpawnersCache.Sort((a, b) => a.teamId.CompareTo(b.teamId));

            // 2. Validate Player Spawners (Max 1 per team)
            foreach(var kvp in playerSpawnerCounts)
            {
                if (kvp.Value > 1)
                {
                    UnityEngine.Debug.LogError($"[GameEntityManager] Error: Multiple Player Spawners found for Team ID {kvp.Key}. Only 1 allowed per team.");
                }
            }

            // 3. Map GameMode Teams to Scene Teams
            if (world.GameMode != null && world.GameMode.teams != null && world.GameMode.teams.Length > 0)
            {
                List<int> validSceneTeams = new List<int>(scenePlayerTeamIds);
                validSceneTeams.Sort(); // Ensure deterministic order

                if (validSceneTeams.Count == 0)
                {
                    UnityEngine.Debug.LogError("[GameEntityManager] GameMode requires teams, but no Player Spawners found in scene!");
                }
                else
                {
                    int gameModeTeams = world.GameMode.teams.Length;
                    UnityEngine.Debug.Log($"[GameEntityManager] Mapping {gameModeTeams} GameMode Teams to {validSceneTeams.Count} Scene Teams.");

                    for (int i = 0; i < gameModeTeams; i++)
                    {
                        // GameMode Team Index (1-based usually in my logic, let's stick to 1-based = i+1)
                        int gmTeamId = i + 1;
                        
                        // Map to Scene Team
                        // If GM Teams > Scene Teams, we reuse scene teams (modulo)
                        int sceneIndex = i % validSceneTeams.Count;
                        int targetSceneTeam = validSceneTeams[sceneIndex];

                        gameModeToSceneTeamMap[gmTeamId] = targetSceneTeam;
                        UnityEngine.Debug.Log($"[GameEntityManager] GameMode Team {gmTeamId} ({world.GameMode.teams[i].teamName}) -> Scene Team {targetSceneTeam}");
                    }
                }
            }
            else
            {
                UnityEngine.Debug.Log("[GameEntityManager] FFA or No Teams defined. Using direct mapping if possible.");
                // For FFA, we usually map PlayerID directly?? Or random spawns?
                // User requirement was specific about Teams. For FFA, let's assume we mapped something or just use direct lookup?
                // If FFA, teamId usually 0.
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
                        if (spawner.spawnerType == Shared.SpawnerType.Npc)
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

        public GameEntity EnsurePlayer(int id, string name, string heroId, int teamId, ServerWorld world)
        {
            string resolvedHeroId = string.IsNullOrEmpty(heroId) ? ContentAssetRegistry.DefaultHeroId : heroId;
            
            if (!heroEntities.TryGetValue(id, out var entity))
            {
                entity = CreateHeroEntity(id, name, resolvedHeroId);
                var heroSo = GetHeroAsset(resolvedHeroId);
                AddHeroComponents(entity, heroSo, id, teamId);

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

        private void AddHeroComponents(GameEntity entity, HeroSO hero, int playerId, int gameModeTeamId)
        {
            // Transform & Spawn
            var spawnPos = GetSpawnPosition(playerId, gameModeTeamId);
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

            entity.AddComponent(health);

            entity.AddComponent(new TeamComponent { teamId = gameModeTeamId });
            entity.AddComponent(new CombatComponent());
            entity.AddComponent(new CombatComponent());
            entity.AddComponent(new CollisionComponent { radius = 0.5f });
            entity.AddComponent(new StatusEffectComponent());
        }

        private Vector2 GetSpawnPosition(int playerId, int gameModeTeamId)
        {
            // 1. Team Based Logic
            if (gameModeToSceneTeamMap.ContainsKey(gameModeTeamId))
            {
                int targetSceneTeamId = gameModeToSceneTeamMap[gameModeTeamId];
                // Find spawner for this team
                foreach (var sp in playerSpawnersCache)
                {
                    if (sp.teamId == targetSceneTeamId) 
                    {
                        // UnityEngine.Debug.Log($"[GameEntityManager] Spawn Player {playerId} at Team {gameModeTeamId} (Scene Team {targetSceneTeamId})");
                        return new Vector2(sp.transform.position.x, sp.transform.position.z);
                    }
                }
                UnityEngine.Debug.LogWarning($"[GameEntityManager] Mapped Team {gameModeTeamId} to Scene {targetSceneTeamId} but no spawner found!");
            }
            // 2. FFA / Fallback Logic (Round Robin)
            else if (playerSpawnersCache.Count > 0)
            {
                var sp = playerSpawnersCache[ffaSpawnIndex % playerSpawnersCache.Count];
                UnityEngine.Debug.Log($"[GameEntityManager] FFA Spawn Player {playerId} at Index {ffaSpawnIndex} (Team {sp.teamId})");
                ffaSpawnIndex++;
                return new Vector2(sp.transform.position.x, sp.transform.position.z);
            }

            UnityEngine.Debug.LogError("[GameEntityManager] No Player Spawners found anywhere!");
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

        public void RespawnPlayer(GameEntity entity, Shared.ScriptableObjects.GameModeSO gameMode)
        {
            if (entity.Type != EntityType.Hero) return;
            
            int teamId = 0;
            if (entity.TryGetComponent(out TeamComponent tc)) teamId = tc.teamId;

            var spawnPos = GetSpawnPosition(entity.OwnerPlayerId, teamId); // teamId mapping handled inside
            
            if (entity.TryGetComponent(out TransformComponent t))
            {
                t.posX = spawnPos.x;
                t.posY = spawnPos.y;
            }
            // Health reset handled by caller (HealthSystem)
        }

        public bool TryGetEntity(int entityId, out GameEntity entity) => Repo.TryGetEntity(entityId, out entity);
    }
}
