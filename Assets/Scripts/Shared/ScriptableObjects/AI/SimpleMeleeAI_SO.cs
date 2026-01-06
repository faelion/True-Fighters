using UnityEngine;
using ServerGame.AI;

namespace Shared.ScriptableObjects.AI
{
    [CreateAssetMenu(fileName = "SimpleMeleeAI", menuName = "TrueFighters/AI/Simple Melee")]
    public class SimpleMeleeAI_SO : AIStrategySO
    {
        public float aggroRange = 8f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1.5f;
        public float attackDamage = 10f;
        public float chaseSpeed = 3.5f;
        public float leashRange = 15f; // Distance from target/spawn to stop chasing
        
        public override AIBehavior CreateBehavior()
        {
            return new ServerGame.AI.Behaviors.SimpleMeleeBehavior(this);
        }
    }
}
