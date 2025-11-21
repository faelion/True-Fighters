namespace ServerGame.Entities
{
    public class GameEntity
    {
        public int Id;
        public string Name;
        public EntityType Type;
        public int OwnerPlayerId = -1;
        public string ArchetypeId;

        public readonly TransformComponent Transform = new TransformComponent();
        public readonly MovementComponent Movement = new MovementComponent();
        public readonly HealthComponent Health = new HealthComponent();
        public readonly TeamComponent Team = new TeamComponent();
        public readonly CombatComponent Combat = new CombatComponent();
        public NpcComponent Npc;
    }
}
