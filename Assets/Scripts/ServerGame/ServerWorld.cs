using System;
using System.Collections.Generic;
using ServerGame.Entities;
using ServerGame.Managers;
using ClientContent;

namespace ServerGame
{
    public class ServerWorld
    {
        public readonly GameEntityManager EntityManager = new GameEntityManager();
        
        public EntityRepository EntityRepo => EntityManager.Repo;

        private readonly List<IGameEvent> pendingEvents = new List<IGameEvent>();
        private int nextEventId = 1;


        public readonly Dictionary<int, Dictionary<string, ClientContent.AbilityAsset>> AbilityBooks = new Dictionary<int, Dictionary<string, ClientContent.AbilityAsset>>();

        private Systems.AbilitySystem abilitySystem;

        public Shared.ScriptableObjects.GameModeSO GameMode { get; set; }

        public ServerWorld()
        {
            // Default GameMode initialization
            string defaultGmId = ContentAssetRegistry.DefaultGameModeId;
            if (ContentAssetRegistry.GameModes.TryGetValue(defaultGmId, out var gm))
            {
                GameMode = gm;
            }
            
            EntityManager.InitializeMapSpawners(this);
        }

        public GameEntity EnsurePlayer(int id, string name = null, string heroId = null)
        {
            return EntityManager.EnsurePlayer(id, name, heroId, this);
        }

        public GameEntity GetHeroEntity(int playerId)
        {
            return EntityManager.GetHeroEntity(playerId);
        }

        public IEnumerable<GameEntity> HeroEntities => EntityManager.HeroEntities;

        public GameEntity CreateNeutralNpc(string neutralId, float posX, float posY)
        {
            return EntityManager.CreateNeutralNpc(neutralId, posX, posY);
        }

        public void DespawnEntity(int entityId)
        {
            EntityManager.DespawnEntity(entityId, this);
        }

        public bool TryGetEntity(int entityId, out GameEntity entity) => EntityManager.TryGetEntity(entityId, out entity);

        public void HandleMove(int playerId, float targetX, float targetY)
        {
            var entity = EnsurePlayer(playerId);
            if (entity.TryGetComponent(out MovementComponent movement))
            {
                movement.destX = targetX;
                movement.destY = targetY;
                movement.hasDestination = true;
            }
        }

        public void SetTeam(int playerId, int teamId)
        {
            var entity = EnsurePlayer(playerId);
            var teamComp = entity.GetComponent<TeamComponent>();
            if (teamComp != null)
            {
                teamComp.teamId = teamId;
            }
        }

        public int RegisterAbilityEffect(object effect, ClientContent.AbilityAsset sourceAsset = null)
        {
            return 0;
        }


        public void EnqueueEvent(IGameEvent ev)
        {
            ev.EventId = nextEventId++;
            pendingEvents.Add(ev);
        }
        public List<IGameEvent> ConsumePendingEvents()
        {
            var copy = new List<IGameEvent>(pendingEvents);
            pendingEvents.Clear();
            return copy;
        }


        public bool TryCastAbility(int playerId, string key, float targetX, float targetY)
        {
            if (abilitySystem == null) return false;
            return abilitySystem.TryCast(this, playerId, key, targetX, targetY);
        }

        internal void BindAbilitySystem(Systems.AbilitySystem system) => abilitySystem = system;
    }
}
