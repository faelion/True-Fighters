using System.Runtime.Serialization;

public enum NetMessageType { Input, State, ProjectileSpawn, ProjectileState }

public class InputMessage
{
    public int playerId;
    public bool isMove;
    public float targetX; 
    public float targetY;
    public string skillKey;
    public int seq;
    public InputMessage() { }
}

public class StateMessage
{
    public int playerId;
    public float posX;
    public float posY;
    public float rotZ;
    public int tick;
    public StateMessage() { }
}

public class ProjectileSpawnMessage
{
    public int projectileId;
    public float posX;
    public float posY;
    public float dirX;
    public float dirY;
    public float speed;
    public int lifeMs;
    public ProjectileSpawnMessage() { }
}

public class ProjectileStateMessage
{
    public int projectileId;
    public float posX;
    public float posY;
    public int lifeMsRemaining;
    public ProjectileStateMessage() { }
}

public class InputMessageSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext ctx)
    {
        var m = obj as InputMessage;
        info.AddValue("playerId", m.playerId);
        info.AddValue("isMove", m.isMove);
        info.AddValue("targetX", m.targetX);
        info.AddValue("targetY", m.targetY);
        info.AddValue("skillKey", m.skillKey ?? "");
        info.AddValue("seq", m.seq);
    }
    public object SetObjectData(object obj, SerializationInfo info, StreamingContext ctx, ISurrogateSelector selector)
    {
        var m = obj as InputMessage ?? new InputMessage();
        m.playerId = info.GetInt32("playerId");
        m.isMove = info.GetBoolean("isMove");
        m.targetX = info.GetSingle("targetX");
        m.targetY = info.GetSingle("targetY");
        m.skillKey = info.GetString("skillKey");
        m.seq = info.GetInt32("seq");
        return m;
    }
}

public class StateMessageSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext ctx)
    {
        var m = obj as StateMessage;
        info.AddValue("playerId", m.playerId);
        info.AddValue("posX", m.posX);
        info.AddValue("posY", m.posY);
        info.AddValue("rotZ", m.rotZ);
        info.AddValue("tick", m.tick);
    }
    public object SetObjectData(object obj, SerializationInfo info, StreamingContext ctx, ISurrogateSelector selector)
    {
        var m = obj as StateMessage ?? new StateMessage();
        m.playerId = info.GetInt32("playerId");
        m.posX = info.GetSingle("posX");
        m.posY = info.GetSingle("posY");
        m.rotZ = info.GetSingle("rotZ");
        m.tick = info.GetInt32("tick");
        return m;
    }
}

public class ProjectileSpawnSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext ctx)
    {
        var m = obj as ProjectileSpawnMessage;
        info.AddValue("projectileId", m.projectileId);
        info.AddValue("posX", m.posX);
        info.AddValue("posY", m.posY);
        info.AddValue("dirX", m.dirX);
        info.AddValue("dirY", m.dirY);
        info.AddValue("speed", m.speed);
        info.AddValue("lifeMs", m.lifeMs);
    }
    public object SetObjectData(object obj, SerializationInfo info, StreamingContext ctx, ISurrogateSelector selector)
    {
        var m = obj as ProjectileSpawnMessage ?? new ProjectileSpawnMessage();
        m.projectileId = info.GetInt32("projectileId");
        m.posX = info.GetSingle("posX");
        m.posY = info.GetSingle("posY");
        m.dirX = info.GetSingle("dirX");
        m.dirY = info.GetSingle("dirY");
        m.speed = info.GetSingle("speed");
        m.lifeMs = info.GetInt32("lifeMs");
        return m;
    }
}

public class ProjectileStateSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext ctx)
    {
        var m = obj as ProjectileStateMessage;
        info.AddValue("projectileId", m.projectileId);
        info.AddValue("posX", m.posX);
        info.AddValue("posY", m.posY);
        info.AddValue("lifeMsRemaining", m.lifeMsRemaining);
    }
    public object SetObjectData(object obj, SerializationInfo info, StreamingContext ctx, ISurrogateSelector selector)
    {
        var m = obj as ProjectileStateMessage ?? new ProjectileStateMessage();
        m.projectileId = info.GetInt32("projectileId");
        m.posX = info.GetSingle("posX");
        m.posY = info.GetSingle("posY");
        m.lifeMsRemaining = info.GetInt32("lifeMsRemaining");
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
        selector.AddSurrogate(typeof(ProjectileSpawnMessage), ctx, new ProjectileSpawnSurrogate());
        selector.AddSurrogate(typeof(ProjectileStateMessage), ctx, new ProjectileStateSurrogate());

        return selector;
    }
}
