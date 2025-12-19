using System.IO;

namespace ServerGame.Entities
{
    public class TeamComponent : IGameComponent
    {
        public ComponentType Type => ComponentType.Team;

        public int teamId = 0;
        public bool friendlyFire = false;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(teamId);
            writer.Write(friendlyFire);
        }

        public void Deserialize(BinaryReader reader)
        {
            teamId = reader.ReadInt32();
            friendlyFire = reader.ReadBoolean();
        }

        public bool IsEnemyTo(TeamComponent other)
        {
            if (other == null) return false;

            if (teamId <= 0 || other.teamId <= 0) return true; 
            
            if (teamId == other.teamId) return friendlyFire;
            return true;
        }
    }
}
