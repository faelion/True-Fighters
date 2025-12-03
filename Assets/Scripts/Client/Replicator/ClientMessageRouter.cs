using System;
using UnityEngine;

public static class ClientMessageRouter
{
    public static event Action<JoinResponseMessage> OnJoinResponse;
    public static event Action<StateMessage> OnEntityState;
    public static event Action<IGameEvent> OnServerEvent;

    public static void RaiseJoinResponse(JoinResponseMessage m) => OnJoinResponse?.Invoke(m);
    public static void RaiseEntityState(StateMessage m) => OnEntityState?.Invoke(m);
    public static void RaiseServerEvent(IGameEvent e) => OnServerEvent?.Invoke(e);
}
