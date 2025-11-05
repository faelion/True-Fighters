using System;

public static class ClientEventBus
{
    public static event Action<AbilityEventMessage> OnAbilityEvent;
    public static event Action<JoinResponseMessage> OnJoinResponse;
    public static event Action<StateMessage> OnEntityState;

    public static void RaiseJoinResponse(JoinResponseMessage m) => OnJoinResponse?.Invoke(m);
    public static void RaiseAbilityEvent(AbilityEventMessage m) => OnAbilityEvent?.Invoke(m);
    public static void RaiseEntityState(StateMessage m) => OnEntityState?.Invoke(m);
}
