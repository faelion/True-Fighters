namespace ServerGame.Systems
{

    public class SimulationRunner
    {
        private readonly ISystem[] systems;
        private readonly ServerWorld world;

        public SimulationRunner(ServerWorld world)
        {
            this.world = world;
            var abilitySystem = new AbilitySystem();
            world.BindAbilitySystem(abilitySystem);
            systems = new ISystem[]
            {
                new MovementSystem(),
                new HealthSystem(),
                new AIBehaviorSystem(),
                new CollisionSystem(),
                abilitySystem
            };
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < systems.Length; i++)
                systems[i].Tick(world, dt);
            
            world.EntityManager.Tick(world, dt);
        }
    }
}
