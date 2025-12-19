using System.Collections.Generic;
using System.Net;

namespace ServerGame
{
    // Keeps track of endpoint <-> playerId and hero selection.
    public class ConnectionRegistry
    {
        private readonly Dictionary<IPEndPoint, int> endpointToPlayerId = new Dictionary<IPEndPoint, int>();
        private readonly Dictionary<int, IPEndPoint> playerIdToEndpoint = new Dictionary<int, IPEndPoint>();
        private readonly Dictionary<int, string> playerIdToHero = new Dictionary<int, string>();
        private readonly Dictionary<int, string> playerIdToName = new Dictionary<int, string>();
        private readonly Dictionary<int, bool> playerIdToReady = new Dictionary<int, bool>();
        private readonly Dictionary<int, int> playerIdToTeam = new Dictionary<int, int>();
        private int nextPlayerId = 1;

        public IReadOnlyDictionary<int, IPEndPoint> PlayerEndpoints => playerIdToEndpoint;

        public bool TryGetPlayerId(IPEndPoint endpoint, out int playerId) => endpointToPlayerId.TryGetValue(endpoint, out playerId);

        public string GetHeroId(int playerId)
        {
            return playerIdToHero.TryGetValue(playerId, out var heroId) ? heroId : ClientContent.ContentAssetRegistry.DefaultHeroId;
        }

        public string GetPlayerName(int playerId)
        {
            return playerIdToName.TryGetValue(playerId, out var name) ? name : "Unknown";
        }

        public int GetTeam(int playerId)
        {
            return playerIdToTeam.TryGetValue(playerId, out var team) ? team : 0;
        }

        public int EnsurePlayer(IPEndPoint endpoint, JoinRequestMessage jr, ServerWorld world)
        {
            if (endpointToPlayerId.TryGetValue(endpoint, out int existing))
                return existing;

            int assigned = nextPlayerId++;
            string heroId = !string.IsNullOrEmpty(jr.heroId) ? jr.heroId : ClientContent.ContentAssetRegistry.DefaultHeroId;

            endpointToPlayerId[endpoint] = assigned;
            playerIdToEndpoint[assigned] = endpoint;
            playerIdToHero[assigned] = heroId;
            playerIdToName[assigned] = !string.IsNullOrEmpty(jr.playerName) ? jr.playerName : $"Player {assigned}";
            playerIdToReady[assigned] = false;
            playerIdToTeam[assigned] = 0; // Default to Team 0 (FFA / No Team)

            if (world != null)
                world.EnsurePlayer(assigned, jr.playerName, heroId);
            return assigned;
        }

        public void UpdateHero(int playerId, string heroId)
        {
            if (playerIdToHero.ContainsKey(playerId)) playerIdToHero[playerId] = heroId;
        }

        public void SetReady(int playerId, bool ready)
        {
            if (playerIdToReady.ContainsKey(playerId)) playerIdToReady[playerId] = ready;
        }

        public void SetTeam(int playerId, int teamId)
        {
            if (playerIdToTeam.ContainsKey(playerId)) playerIdToTeam[playerId] = teamId;
        }

        public Shared.LobbyPlayerInfo[] GetLobbyInfo()
        {
            var list = new List<Shared.LobbyPlayerInfo>();
            foreach(var kv in playerIdToEndpoint)
            {
                int pid = kv.Key;
                list.Add(new Shared.LobbyPlayerInfo
                {
                    ConnectionId = pid,
                    PlayerName = playerIdToName.ContainsKey(pid) ? playerIdToName[pid] : "Unknown",
                    SelectedHeroId = playerIdToHero.ContainsKey(pid) ? playerIdToHero[pid] : "",
                    IsReady = playerIdToReady.ContainsKey(pid) ? playerIdToReady[pid] : false,
                    TeamId = playerIdToTeam.ContainsKey(pid) ? playerIdToTeam[pid] : 0
                });
            }
            return list.ToArray();
        }
    }
}
