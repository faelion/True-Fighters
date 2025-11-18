using System;
using UnityEngine;

// Instance-based router to forward network messages to interested client systems.
public class ClientMessageRouter : MonoBehaviour
{
    public event Action<JoinResponseMessage> OnJoinResponse;
    public event Action<StateMessage> OnEntityState;
    public event Action<IGameEvent> OnServerEvent;

    public void RaiseJoinResponse(JoinResponseMessage m) => OnJoinResponse?.Invoke(m);
    public void RaiseEntityState(StateMessage m) => OnEntityState?.Invoke(m);
    public void RaiseServerEvent(IGameEvent e) => OnServerEvent?.Invoke(e);
}
