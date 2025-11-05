using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Networking.Serialization;
using UnityEngine;

namespace Networking.Transport
{
    public class UdpTransport : INetworkTransport
    {
        private Socket udp;
        private Thread recvThread;
        private volatile bool running;
        private ISerializer serializer;

        public event Action<IPEndPoint, object> OnReceive;

        public UdpTransport(ISerializer serializer = null)
        {
            this.serializer = serializer ?? new BinaryFormatterSerializer();
        }

        public void Start(IPEndPoint localBind)
        {
            if (udp != null) throw new InvalidOperationException("Transport already started");
            udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udp.ReceiveTimeout = 1000;
            udp.Bind(localBind ?? new IPEndPoint(IPAddress.Any, 0));

            running = true;
            recvThread = new Thread(RecvLoop) { IsBackground = true };
            recvThread.Start();
        }

        public void Stop()
        {
            running = false;
            try { udp?.Close(); } catch { }
            if (recvThread != null && recvThread.IsAlive) recvThread.Join(500);
            recvThread = null;
            udp = null;
        }

        public void Send(IPEndPoint remote, object message)
        {
            if (udp == null) throw new InvalidOperationException("Transport not started");
            try
            {
                byte[] data = serializer.Serialize(message);
                udp.SendTo(data, remote);
            }
            catch (Exception e)
            {
                Debug.LogError("[UdpTransport] send failed: " + e);
            }
        }

        private void RecvLoop()
        {
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            byte[] buffer = new byte[8192];
            while (running)
            {
                try
                {
                    int r = udp.ReceiveFrom(buffer, ref remoteEP);
                    if (r > 0)
                    {
                        byte[] payload = new byte[r];
                        Buffer.BlockCopy(buffer, 0, payload, 0, r);
                        object msg = serializer.Deserialize(payload);
                        OnReceive?.Invoke((IPEndPoint)remoteEP, msg);
                    }
                }
                catch (SocketException se)
                {
                    if (se.SocketErrorCode == SocketError.TimedOut) { continue; }
                    Debug.LogWarning("[UdpTransport] recv error: " + se);
                    Thread.Sleep(50);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogError("[UdpTransport] recv loop error: " + e);
                    break;
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

