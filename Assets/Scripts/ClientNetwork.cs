using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Networking.Transport;

public class ClientNetwork : MonoBehaviour
{
    public string serverHost = "127.0.0.1";
    public int serverPort = 9050;

    public int clientPlayerId = 0;

    private INetworkTransport transport;
    private IPEndPoint serverEndpoint;
    [SerializeField] private ClientMessageRouter messageRouter;

    // Presentation is handled by replicators; ClientNetwork keeps no scene GameObjects

    private volatile bool hasAssignedId = false;
    private int assignedPlayerId = 0;
    private int joinAttempts = 0;
    private const int MAX_JOIN_ATTEMPTS = 8;
    private float joinRetryDelay = 0.6f;
    private float timeSinceLastJoin = 0f;

    public bool HasAssignedId => hasAssignedId;
    public int AssignedPlayerId => assignedPlayerId;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        // messageRouter will be null here if scene is not loaded yet or if it's in another scene
        // We need to find it dynamically or have it register itself
    }

    private void EnsureRouter()
    {
        if (messageRouter == null)
            messageRouter = FindFirstObjectByType<ClientMessageRouter>();
    }

    public void Connect(string host, int port)
    {
        serverHost = host;
        serverPort = port;
        
        if (transport != null) { transport.Stop(); transport.Dispose(); }

        transport = new UdpTransport();
        transport.Start(new IPEndPoint(IPAddress.Any, 0));
        transport.OnReceive += OnReceiveMessage;

        serverEndpoint = new IPEndPoint(IPAddress.Parse(serverHost), serverPort);

        joinAttempts = 0;
        hasAssignedId = false;
        SendJoinRequest();
    }

    void OnDestroy()
    {
        transport?.Stop();
        transport?.Dispose();
    }

    public void SendInput(InputMessage input)
    {
        if (!hasAssignedId) return;
        input.playerId = assignedPlayerId;
        SendToServer(input);
    }

    private void SendToServer(object msg)
    {
        try
        {
            transport.Send(serverEndpoint, msg);
        }
        catch (Exception e)
        {
            Debug.LogError("Client send failed: " + e);
        }
    }

    public void SendJoinRequest()
    {
        var jr = new JoinRequestMessage() { playerName = NetworkConfig.playerName ?? "Player", heroId = NetworkConfig.heroId };
        try
        {
            transport.Send(serverEndpoint, jr);
            joinAttempts++;
            timeSinceLastJoin = 0f;
            Debug.Log($"Client: Sent JoinRequest attempt {joinAttempts}");
        }
        catch (Exception e)
        {
            Debug.LogError("SendJoinRequest failed: " + e);
        }
    }

    void Update()
    {
        transport?.Update();

        if (!hasAssignedId && joinAttempts < MAX_JOIN_ATTEMPTS)
        {
            timeSinceLastJoin += Time.deltaTime;
            if (timeSinceLastJoin >= joinRetryDelay)
            {
                SendJoinRequest();
            }
        }
    }

    private void OnReceiveMessage(IPEndPoint remote, object msg)
    {
        EnsureRouter();
        if (msg is JoinResponseMessage jr)
        {
            assignedPlayerId = jr.assignedPlayerId;
            hasAssignedId = true;
            clientPlayerId = assignedPlayerId;
            Debug.Log($"Client: Received JoinResponse -> assigned id {assignedPlayerId}");
            messageRouter?.RaiseJoinResponse(jr);
        }
        else if (msg is StartGameMessage sgm)
        {
            Debug.Log($"Client: Received StartGameMessage -> Loading scene '{sgm.sceneName}'");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sgm.sceneName);
        }
        else if (msg is TickPacketMessage tpm)
        {
            if (tpm.states != null)
                foreach (var state in tpm.states) messageRouter?.RaiseEntityState(state);
            if (tpm.events != null)
                foreach (var ev in tpm.events) messageRouter?.RaiseServerEvent(ev);
        }
        else
        {
            Debug.Log("Client received unknown message type: " + msg.GetType());
        }
    }

    // All inputs (move + abilities) are unified via SendInput(InputMessage)
}
