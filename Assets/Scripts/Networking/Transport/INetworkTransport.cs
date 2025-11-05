using System;
using System.Net;

namespace Networking.Transport
{
    public interface INetworkTransport : IDisposable
    {
        void Start(IPEndPoint localBind);
        void Stop();
        void Send(IPEndPoint remote, object message);
        event Action<IPEndPoint, object> OnReceive;
    }
}

