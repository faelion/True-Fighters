namespace ServerGame.Entities
{
    public class TeamComponent
    {
        public int teamId = 0;
        public bool friendlyFire = false;

        public bool IsEnemyTo(TeamComponent other)
        {
            if (other == null) return false;
            if (teamId == -1 || other.teamId == -1) return true; // neutrals hostile to all
            if (teamId == other.teamId) return friendlyFire;
            return true;
        }
    }
}
