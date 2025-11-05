using System.Collections.Generic;

namespace ServerGame.Content
{
    public class ServerHeroDef
    {
        public string id;
        public string displayName;
        public float baseHp = 500f;
        public float baseMoveSpeed = 3.5f;
        public Dictionary<string, string> bindings = new Dictionary<string, string>(); // key -> abilityId
    }
}

