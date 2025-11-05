using System.Runtime.Serialization;

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
    public TickPacketMessage() { }
}
public class InputMessageSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext ctx)
    {
        var m = obj as InputMessage;
        info.AddValue("playerId", m.playerId);
        info.AddValue("kind", (int)m.kind);
        info.AddValue("targetX", m.targetX);
        info.AddValue("targetY", m.targetY);
    }
    public object SetObjectData(object obj, SerializationInfo info, StreamingContext ctx, ISurrogateSelector selector)
    {
        var m = obj as InputMessage ?? new InputMessage();
        m.playerId = info.GetInt32("playerId");
        m.kind = (InputKind)info.GetInt32("kind");
        m.targetX = info.GetSingle("targetX");
        m.targetY = info.GetSingle("targetY");
        return m;
    }
}

public class StateMessageSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext ctx)
    {
        var m = obj as StateMessage;
        info.AddValue("playerId", m.playerId);
        info.AddValue("hit", m.hit);
        info.AddValue("posX", m.posX);
        info.AddValue("posY", m.posY);
        info.AddValue("rotZ", m.rotZ);
        info.AddValue("tick", m.tick);
    }
    public object SetObjectData(object obj, SerializationInfo info, StreamingContext ctx, ISurrogateSelector selector)
    {
        var m = obj as StateMessage ?? new StateMessage();
        m.playerId = info.GetInt32("playerId");
        m.hit = info.GetBoolean("hit");
        m.posX = info.GetSingle("posX");
        m.posY = info.GetSingle("posY");
        m.rotZ = info.GetSingle("rotZ");
        m.tick = info.GetInt32("tick");
        return m;
    }
}


public class JoinRequestSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext ctx)
    {
        var m = obj as JoinRequestMessage;
        info.AddValue("playerName", m?.playerName ?? "");
    }
    public object SetObjectData(object obj, SerializationInfo info, StreamingContext ctx, ISurrogateSelector selector)
    {
        var m = obj as JoinRequestMessage ?? new JoinRequestMessage();
        m.playerName = info.GetString("playerName");
        return m;
    }
}

public class JoinResponseSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext ctx)
    {
        var m = obj as JoinResponseMessage;
        info.AddValue("assignedPlayerId", m.assignedPlayerId);
        info.AddValue("serverTick", m.serverTick);
    }
    public object SetObjectData(object obj, SerializationInfo info, StreamingContext ctx, ISurrogateSelector selector)
    {
        var m = obj as JoinResponseMessage ?? new JoinResponseMessage();
        m.assignedPlayerId = info.GetInt32("assignedPlayerId");
        m.serverTick = info.GetInt32("serverTick");
        return m;
    }
}

public static class NetSurrogateRegistry
{
    public static SurrogateSelector CreateSelectorWithNetMessages()
    {
        var selector = new SurrogateSelector();
        var ctx = new StreamingContext(StreamingContextStates.All);

        selector.AddSurrogate(typeof(InputMessage), ctx, new InputMessageSurrogate());
        selector.AddSurrogate(typeof(StateMessage), ctx, new StateMessageSurrogate());
        // Projectile spawns are unified via AbilityEventMessage (SpawnProjectile)
        // Projectile state/despawn unified via AbilityEventMessage
        selector.AddSurrogate(typeof(JoinRequestMessage), ctx, new JoinRequestSurrogate());
        selector.AddSurrogate(typeof(JoinResponseMessage), ctx, new JoinResponseSurrogate());
                selector.AddSurrogate(typeof(AbilityEventMessage), ctx, new AbilityEventSurrogate());
        selector.AddSurrogate(typeof(TickPacketMessage), ctx, new TickPacketSurrogate());

        return selector;
    }
}

public class TickPacketSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext ctx)
    {
        var m = obj as TickPacketMessage;
        info.AddValue("serverTick", m.serverTick);
        int sc = m.states != null ? m.states.Length : 0;
        int ec = m.abilityEvents != null ? m.abilityEvents.Length : 0;
        info.AddValue("statesCount", sc);
        for (int i = 0; i < sc; i++) info.AddValue($"state_{i}", m.states[i]);
        info.AddValue("eventsCount", ec);
        for (int i = 0; i < ec; i++) info.AddValue($"event_{i}", m.abilityEvents[i]);
    }
    public object SetObjectData(object obj, SerializationInfo info, StreamingContext ctx, ISurrogateSelector selector)
    {
        var m = obj as TickPacketMessage ?? new TickPacketMessage();
        m.serverTick = info.GetInt32("serverTick");
        int sc = info.GetInt32("statesCount");
        m.states = new StateMessage[sc];
        for (int i = 0; i < sc; i++) m.states[i] = (StateMessage)info.GetValue($"state_{i}", typeof(StateMessage));
        int ec = info.GetInt32("eventsCount");
        m.abilityEvents = new AbilityEventMessage[ec];
        for (int i = 0; i < ec; i++) m.abilityEvents[i] = (AbilityEventMessage)info.GetValue($"event_{i}", typeof(AbilityEventMessage));
        return m;
    }
}

public class AbilityEventSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext ctx)
    {
        var m = obj as AbilityEventMessage;
        info.AddValue("abilityIdOrKey", m.abilityIdOrKey ?? "");
        info.AddValue("casterId", m.casterId);
        info.AddValue("eventType", (int)m.eventType);
        info.AddValue("castTime", m.castTime);
        info.AddValue("serverTick", m.serverTick);
        info.AddValue("projectileId", m.projectileId);
        info.AddValue("posX", m.posX);
        info.AddValue("posY", m.posY);
        info.AddValue("dirX", m.dirX);
        info.AddValue("dirY", m.dirY);
        info.AddValue("speed", m.speed);
        info.AddValue("lifeMs", m.lifeMs);
        info.AddValue("value", m.value);
    }
    public object SetObjectData(object obj, SerializationInfo info, StreamingContext ctx, ISurrogateSelector selector)
    {
        var m = obj as AbilityEventMessage ?? new AbilityEventMessage();
        m.abilityIdOrKey = info.GetString("abilityIdOrKey");
        m.casterId = info.GetInt32("casterId");
        m.eventType = (AbilityEventType)info.GetInt32("eventType");
        m.castTime = info.GetSingle("castTime");
        m.serverTick = info.GetInt32("serverTick");
        m.projectileId = info.GetInt32("projectileId");
        m.posX = info.GetSingle("posX");
        m.posY = info.GetSingle("posY");
        m.dirX = info.GetSingle("dirX");
        m.dirY = info.GetSingle("dirY");
        m.speed = info.GetSingle("speed");
        m.lifeMs = info.GetInt32("lifeMs");
        m.value = info.GetSingle("value");
        return m;
    }
}

// ProjectileDespawnSurrogate removed; unified via AbilityEventMessage

