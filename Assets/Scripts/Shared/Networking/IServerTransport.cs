using System;

namespace Shared.Networking
{
    public interface IServerTransport
    {
        // Lifecycle
        void Init(int port);
        void Shutdown();
        void PollEvents();

        // Messaging
        void SendToClient(int playerId, object message);
        void Broadcast(object message);

        // Events
        event Action<int> OnPlayerJoined;           // returns new PlayerId
        event Action<int> OnPlayerLeft;             // returns PlayerId
        event Action<int, object> OnDataReceived;   // returns PlayerId, Message
    }
}
