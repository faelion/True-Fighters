using System.Collections.Generic;

namespace ServerGame.Entities
{
    public class GameEntity
    {
        public int Id;
        public string Name;
        public EntityType Type; 
        public int OwnerPlayerId = -1;
        public string ArchetypeId;

        private readonly Dictionary<ComponentType, IGameComponent> components = new Dictionary<ComponentType, IGameComponent>();
        public IEnumerable<IGameComponent> AllComponents => components.Values;

        public void AddComponent(IGameComponent component)
        {
            if (component == null) return;
            components[component.Type] = component;
        }

        public T GetComponent<T>() where T : class, IGameComponent
        {
            foreach (var kv in components)
            {
                if (kv.Value is T) return kv.Value as T;
            }
            return null;
        }

        public bool TryGetComponent<T>(out T component) where T : class, IGameComponent
        {
            component = GetComponent<T>();
            return component != null;
        }

        public IGameComponent GetComponentByType(ComponentType type)
        {
            if (components.TryGetValue(type, out var comp)) return comp;
            return null;
        }
    }
}
