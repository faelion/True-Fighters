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
    public int serverPort = 7777;

    public int clientPlayerId = 0;

    private INetworkTransport transport;
    private IPEndPoint serverEndpoint;



    private volatile bool hasAssignedId = false;
    private int assignedPlayerId = 0;
    private int joinAttempts = 0;
    private const int MAX_JOIN_ATTEMPTS = 8;
    private float joinRetryDelay = 0.6f;
    private float timeSinceLastJoin = 0f;

    public bool HasAssignedId => hasAssignedId;
    public int AssignedPlayerId => assignedPlayerId;

    private string _currentGameModeId;
    public string CurrentGameModeId 
    { 
        get 
        {
             if (!string.IsNullOrEmpty(_currentGameModeId)) return _currentGameModeId;
             if (!string.IsNullOrEmpty(LastLobbyState.SelectedGameModeId)) return LastLobbyState.SelectedGameModeId;
             // Fallback for direct scene testing or late join without info
             return ClientContent.ContentAssetRegistry.DefaultGameModeId;
        }
        private set => _currentGameModeId = value;
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
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

    private int lastReceivedServerTick = -1;
    private readonly HashSet<int> processedReliableEvents = new HashSet<int>();
    private int lastProcessedEventId = 0;

    public void SendInput(InputMessage input)
    {
        if (!hasAssignedId) return;
        input.playerId = assignedPlayerId;
        input.lastReceivedTick = lastReceivedServerTick;
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

    public event Action<Shared.LobbyStateData> OnLobbyUpdate;
    public Shared.LobbyStateData LastLobbyState { get; private set; }

    public void SendLobbyAction(int actionType, string payload)
    {
        if (!hasAssignedId) return;
        var msg = new LobbyActionMessage { actionType = actionType, payload = payload };
        SendToServer(msg);
    }

    private void OnReceiveMessage(IPEndPoint remote, object msg)
    {
        if (msg is JoinResponseMessage jr)
        {
            assignedPlayerId = jr.assignedPlayerId;
            hasAssignedId = true;
            clientPlayerId = assignedPlayerId;
            Debug.Log($"Client: Received JoinResponse -> assigned id {assignedPlayerId}");
            ClientMessageRouter.RaiseJoinResponse(jr);
        }
        else if (msg is LobbyUpdateMessage lum)
        {
            Debug.Log($"Client: Received LobbyUpdateMessage. Players: {lum.data.Players?.Length}");
            LastLobbyState = lum.data;
            OnLobbyUpdate?.Invoke(lum.data);
        }
        else if (msg is StartGameMessage sgm)
        {
            Debug.Log($"Client: Received StartGameMessage -> Loading scene '{sgm.sceneName}'");
            CurrentGameModeId = sgm.gameModeId;
            UnityEngine.SceneManagement.SceneManager.LoadScene(sgm.sceneName);
        }
        else if (msg is TickPacketMessage tpm)
        {
            if (tpm.serverTick > lastReceivedServerTick)
                lastReceivedServerTick = tpm.serverTick;

            if (tpm.states != null)
                foreach (var state in tpm.states) ClientMessageRouter.RaiseEntityState(state);
            
            if (tpm.events != null)
            {
                foreach (var ev in tpm.events)
                {
                    if (ev.EventId <= lastProcessedEventId) continue;
                    lastProcessedEventId = ev.EventId;
                    
                    ClientMessageRouter.RaiseServerEvent(ev);
                }
            }
        }
        else
        {
            Debug.Log("Client received unknown message type: " + msg.GetType());
        }
    }
}
