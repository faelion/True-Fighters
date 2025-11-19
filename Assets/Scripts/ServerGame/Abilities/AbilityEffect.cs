namespace ServerGame
{
    public class AbilityEffect
    {
        public int id;
        public int ownerPlayerId;
        public string abilityId; // which ability spawned this effect
        public float posX, posY;
        public float dirX, dirY;
        public float speed;
        public int lifeMs;
        public AbilityEffectData data;

        public T GetData<T>() where T : AbilityEffectData
        {
            return data as T;
        }
    }
}
