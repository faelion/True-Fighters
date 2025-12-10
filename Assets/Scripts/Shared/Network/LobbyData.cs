using System;
using System.IO;

namespace Shared
{
    [Serializable]
    public struct LobbyPlayerInfo
    {
        public int ConnectionId;
        public string PlayerName;
        public string SelectedHeroId;
        public bool IsReady;
        public int TeamId; // 0 = None/Spectator (or default team logic)

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ConnectionId);
            writer.Write(PlayerName ?? "Unknown");
            writer.Write(SelectedHeroId ?? "");
            writer.Write(IsReady);
            writer.Write(TeamId);
        }

        public void Deserialize(BinaryReader reader)
        {
            ConnectionId = reader.ReadInt32();
            PlayerName = reader.ReadString();
            SelectedHeroId = reader.ReadString();
            IsReady = reader.ReadBoolean();
            TeamId = reader.ReadInt32();
        }
    }

    [Serializable]
    public struct LobbyStateData
    {
        public bool IsGameStarted;
        public string SelectedGameModeId;
        public LobbyPlayerInfo[] Players;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(IsGameStarted);
            writer.Write(SelectedGameModeId ?? "");
            int count = Players != null ? Players.Length : 0;
            writer.Write(count);
            for (int i = 0; i < count; i++)
            {
                Players[i].Serialize(writer);
            }
        }

        public void Deserialize(BinaryReader reader)
        {
            IsGameStarted = reader.ReadBoolean();
            SelectedGameModeId = reader.ReadString();
            int count = reader.ReadInt32();
            Players = new LobbyPlayerInfo[count];
            for (int i = 0; i < count; i++)
            {
                Players[i] = new LobbyPlayerInfo();
                Players[i].Deserialize(reader);
            }
        }
    }

    public enum ServerGameState
    {
        Lobby,
        Game
    }
}
