namespace ServerGame.Systems
{
    // Orchestrates ticking the simulation systems over a ServerWorld.
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
                new NpcSystem(),
                abilitySystem,
                new AbilityEffectSystem()
            };
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < systems.Length; i++)
                systems[i].Tick(world, dt);
        }
    }
}
