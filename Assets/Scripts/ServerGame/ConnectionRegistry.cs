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
        private int nextPlayerId = 1;

        public IReadOnlyDictionary<int, IPEndPoint> PlayerEndpoints => playerIdToEndpoint;

        public bool TryGetPlayerId(IPEndPoint endpoint, out int playerId) => endpointToPlayerId.TryGetValue(endpoint, out playerId);

        public string GetHeroId(int playerId)
        {
            return playerIdToHero.TryGetValue(playerId, out var heroId) ? heroId : ClientContent.ContentAssetRegistry.DefaultHeroId;
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

            world.EnsurePlayer(assigned, jr.playerName, heroId);
            return assigned;
        }
    }
}
