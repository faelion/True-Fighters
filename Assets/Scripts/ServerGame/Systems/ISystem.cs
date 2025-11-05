namespace ServerGame.Systems
{
    public interface ISystem
    {
        void Tick(ServerWorld world, float dt);
    }
}

