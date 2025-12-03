// Network message DTOs shared between client and server

public enum InputKind { None, RightClick, Q, W, E, R }

public class InputMessage
{
    public int playerId;
    public InputKind kind;
    public float targetX;
    public float targetY;
    public int lastReceivedTick; // ACK: The last server tick this client received
    public InputMessage() { }
}

public class StateMessage
{
    public int entityId;
    public float hp;
    public float maxHp;
    public float posX;
    public float posY;
    public float rotZ;
    public int teamId;
    public int entityType;
    public string archetypeId;
    public int tick;
    public StateMessage() { }
}

public class JoinRequestMessage
{
    public string playerName;
    public string heroId;
    public JoinRequestMessage() { }
}

public class JoinResponseMessage
{
    public int assignedPlayerId;
    public int serverTick;
    public string heroId;
    public JoinResponseMessage() { }
}

public class StartGameMessage
{
    public string sceneName;
    public StartGameMessage() { }
}

public enum AbilityTargetType { None, Point, Unit, Direction }

// Game events are now strongly typed payloads identified by GameEventType.
public enum ProjectileAction { Spawn, Update, Despawn }
public enum GameEventType { Projectile = 1, Dash = 4, EntityDespawn = 5, EntitySpawn = 6 }

public interface IGameEvent
{
    GameEventType Type { get; }
    string SourceId { get; }
    int CasterId { get; }
    int ServerTick { get; set; }
    int EventId { get; set; }
    bool IsReliable { get; }
}

public class EntityDespawnEvent : IGameEvent
{
    public string SourceId => "";
    public int CasterId { get; set; } // The entity being despawned
    public int ServerTick { get; set; }
    public int EventId { get; set; }
    public GameEventType Type => GameEventType.EntityDespawn;
    public bool IsReliable => true;
}

public class EntitySpawnEvent : IGameEvent
{
    public string SourceId => "";
    public int CasterId { get; set; }
    public int ServerTick { get; set; }
    public int EventId { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public string ArchetypeId { get; set; }
    public int TeamId { get; set; }
    public GameEventType Type => GameEventType.EntitySpawn;
    public bool IsReliable => true;
}

public class ProjectileEvent : IGameEvent
{
    public ProjectileAction Action;
    public string SourceId { get; set; }
    public int CasterId { get; set; }
    public int ServerTick { get; set; }
    public int ProjectileId { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float DirX { get; set; }
    public float DirY { get; set; }
    public float Speed { get; set; }
    public int LifeMs { get; set; }
    public int EventId { get; set; }
    public GameEventType Type => GameEventType.Projectile;
    public bool IsReliable => Action == ProjectileAction.Spawn || Action == ProjectileAction.Despawn;
}

public class DashEvent : IGameEvent
{
    public string SourceId { get; set; }
    public int CasterId { get; set; }
    public int ServerTick { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float DirX { get; set; }
    public float DirY { get; set; }
    public float Speed { get; set; }
    public int EventId { get; set; }
    public GameEventType Type => GameEventType.Dash;
    public bool IsReliable => true;
}

public class TickPacketMessage
{
    public int serverTick;
    public StateMessage[] states;
    public IGameEvent[] events;
    public int statesCount;
    public int eventsCount;
    public TickPacketMessage() { }
}

