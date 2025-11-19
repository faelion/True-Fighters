// Network message DTOs shared between client and server

public enum InputKind { None, RightClick, Q, W, E, R }

public class InputMessage
{
    public int playerId;
    public InputKind kind;
    public float targetX;
    public float targetY;
    public InputMessage() { }
}

public class StateMessage
{
    public int playerId;
    public bool hit;
    public float posX;
    public float posY;
    public float rotZ;
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

public enum AbilityTargetType { None, Point, Unit, Direction }

// Game events are now strongly typed payloads identified by GameEventType.
public enum GameEventType { ProjectileSpawn = 1, ProjectileUpdate = 2, ProjectileDespawn = 3, Dash = 4 }

public interface IGameEvent
{
    GameEventType Type { get; }
    string SourceId { get; }
    int CasterId { get; }
    int ServerTick { get; set; }
}

public class ProjectileSpawnEvent : IGameEvent
{
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
    public GameEventType Type => GameEventType.ProjectileSpawn;
}

public class ProjectileUpdateEvent : IGameEvent
{
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
    public GameEventType Type => GameEventType.ProjectileUpdate;
}

public class ProjectileDespawnEvent : IGameEvent
{
    public string SourceId { get; set; }
    public int CasterId { get; set; }
    public int ServerTick { get; set; }
    public int ProjectileId { get; set; }
    public GameEventType Type => GameEventType.ProjectileDespawn;
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
    public GameEventType Type => GameEventType.Dash;
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

