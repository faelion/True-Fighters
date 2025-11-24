using System;
using System.Collections.Generic;
using ServerGame.Entities;
using ServerGame.Managers;
using ClientContent;

namespace ServerGame
{
    public class ServerWorld
    {
        public const float HitFlashDuration = 0.2f;
        
        // Managers
        public readonly GameEntityManager EntityManager = new GameEntityManager();
        
        public readonly Dictionary<int, AbilityEffect> AbilityEffects = new Dictionary<int, AbilityEffect>();
        
        // Expose EntityRepo via Manager for compatibility if needed, or just use Manager
        public EntityRepository EntityRepo => EntityManager.Repo;

        private readonly List<IGameEvent> pendingEvents = new List<IGameEvent>();
        private int nextEffectId = 1;

        // Per-player ability book: key (Q,W,E,R) -> AbilityAsset
        public readonly Dictionary<int, Dictionary<string, ClientContent.AbilityAsset>> AbilityBooks = new Dictionary<int, Dictionary<string, ClientContent.AbilityAsset>>();

        private Systems.AbilitySystem abilitySystem;

        public ServerWorld()
        {
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
    }
}
