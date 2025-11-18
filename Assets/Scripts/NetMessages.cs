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
    public JoinRequestMessage() { }
}

public class JoinResponseMessage
{
    public int assignedPlayerId;
    public int serverTick;
    public JoinResponseMessage() { }
}

public enum AbilityTargetType { None, Point, Unit, Direction }

public enum AbilityEventType { CastStarted, CastResolved, SpawnProjectile, ProjectileUpdate, ProjectileDespawn, SpawnArea, Dash, Heal, BuffApply, PickupSpawn }

public class AbilityEventMessage
{
    public string abilityIdOrKey;
    public int casterId;
    public AbilityEventType eventType;

    // Minimal payload fields (extend as needed)
    // For CastStarted/Resolved
    public float castTime;
    public int serverTick;

    // For SpawnProjectile
    public int projectileId;
    public float posX, posY, dirX, dirY, speed;
    public int lifeMs;
    public float value; // generic numeric payload (e.g., heal amount)

    // For SpawnArea / Dash / Heal / BuffApply / PickupSpawn add fields later

    public AbilityEventMessage() { }
}

public class TickPacketMessage
{
    public int serverTick;
    public StateMessage[] states;
    public AbilityEventMessage[] abilityEvents;
    public int statesCount;
    public int eventsCount;
    public TickPacketMessage() { }
}
// Manual serialization is used; Surrogates removed.

