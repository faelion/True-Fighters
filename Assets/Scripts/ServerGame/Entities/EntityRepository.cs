using System.Collections.Generic;

namespace ServerGame.Entities
{
    public class EntityRepository
    {
        private readonly Dictionary<int, GameEntity> entities = new Dictionary<int, GameEntity>();
        private readonly Dictionary<EntityType, List<GameEntity>> byType = new Dictionary<EntityType, List<GameEntity>>();
        private int nextEntityId = 1000;

        public IEnumerable<GameEntity> AllEntities => entities.Values;

        public GameEntity CreateEntity(EntityType type, int? forcedId = null)
        {
            int id = forcedId ?? nextEntityId++;
            if (forcedId.HasValue && forcedId.Value >= nextEntityId)
                nextEntityId = forcedId.Value + 1;
            var entity = new GameEntity { Id = id, Type = type };
            entities[id] = entity;
            if (!byType.TryGetValue(type, out var list))
            {
                list = new List<GameEntity>();
                byType[type] = list;
            }
            list.Add(entity);
            return entity;
        }

        public bool TryGetEntity(int id, out GameEntity entity) => entities.TryGetValue(id, out entity);

        public IEnumerable<GameEntity> GetByType(EntityType type)
        {
            if (byType.TryGetValue(type, out var list)) return list;
            return System.Array.Empty<GameEntity>();
        }

        public void Remove(int id)
        {
            if (!entities.TryGetValue(id, out var entity)) return;
            entities.Remove(id);
            if (byType.TryGetValue(entity.Type, out var list))
            {
                list.Remove(entity);
            }
        }
    }
}
