// Network message DTOs shared between client and server

public enum InputKind { None, RightClick, Q, W, E, R }

public class InputMessage
{
    public int playerId;
    public InputKind kind;
    public float targetX;
    public float targetY;
    public int lastReceivedTick;
    public InputMessage() { }
}

public class ComponentData
{
    public int type;
    public byte[] data;
    public ComponentData() { }
}

public class EntityStateData
{
    public int entityId;
    public int entityType; // TODO: Verify if this is still needed for identification (e.g. initial spawn)
    public string archetypeId;
    public int tick;
    public ComponentData[] components;
    public EntityStateData() { }
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
    public string gameModeId;
    public StartGameMessage() { }
}

public class LobbyUpdateMessage
{
    public Shared.LobbyStateData data;
    public LobbyUpdateMessage() { }
}

public class LobbyActionMessage
{
    public int actionType; // 0=None, 1=SelectHero, 2=ToggleReady, 3=ChangeTeam, 4=SetGameMode
    public string payload; // HeroId, "1"/"0", TeamId string, or GameModeId
    public LobbyActionMessage() { }
}

public enum GameEventType { EntityDespawn = 1, EntitySpawn = 2 }

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
    public int CasterId { get; set; }
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

public class TickPacketMessage
{
    public int serverTick;
    public EntityStateData[] states;
    public IGameEvent[] events;
    public int statesCount;
    public int eventsCount;
    public TickPacketMessage() { }
}

