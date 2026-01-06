using UnityEngine;
using ServerGame.Entities;
using Shared.ScriptableObjects.AI;
using Shared.Utils;
using System.Collections.Generic;

namespace ServerGame.AI.Behaviors
{
    public class SimpleMeleeBehavior : AIBehavior
    {
        private readonly SimpleMeleeAI_SO config;
        
        private float cooldownTimer = 0f;
        private float pathUpdateTimer = 0f;
        private const float PATH_UPDATE_INTERVAL = 0.5f;

        private int targetId = -1;

        public SimpleMeleeBehavior(SimpleMeleeAI_SO config)
        {
            this.config = config;
        }

        public override void Tick(ServerWorld world, GameEntity entity, float dt)
        {
            if (cooldownTimer > 0) cooldownTimer -= dt;
            if (pathUpdateTimer > 0) pathUpdateTimer -= dt;

            // 1. Validate Target
            GameEntity target = null;
            if (targetId != -1)
            {
                if (world.EntityRepo.TryGetEntity(targetId, out var t))
                {
                    if (t.TryGetComponent(out HealthComponent h) && h.IsAlive)
                    {
                         target = t;
                    }
                }
            }

            // 2. Scan if no target (or target lost/dead)
            if (target == null)
            {
                targetId = -1;
                target = FindTarget(world, entity);
                if (target != null) targetId = target.Id;
            }

            // 3. Act
            if (target != null)
            {
                PerformCombatLogic(world, entity, target, dt);
            }
            else
            {
                // Idle / Return home logic could go here
                StopMoving(entity);
            }
        }

        private GameEntity FindTarget(ServerWorld world, GameEntity me)
        {
            if (!me.TryGetComponent(out TransformComponent myTrans)) return null;

            float closestDistSq = config.aggroRange * config.aggroRange;
            GameEntity bestTarget = null;
            
            // Simple scan of Heroes (can extend to other teams later)
            foreach (var hero in world.HeroEntities)
            {
                if (!hero.TryGetComponent(out HealthComponent h) || !h.IsAlive) continue;
                if (!hero.TryGetComponent(out TransformComponent t)) continue;
                
                // Optional: Check Team 
                if (me.TryGetComponent(out TeamComponent myTeam) && hero.TryGetComponent(out TeamComponent heroTeam))
                {
                    if (myTeam.teamId == heroTeam.teamId) continue;
                }

                float distSq = GetDistSq(myTrans, t);
                if (distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    bestTarget = hero;
                }
            }
            return bestTarget;
        }

        private void PerformCombatLogic(ServerWorld world, GameEntity me, GameEntity target, float dt)
        {
             if (!me.TryGetComponent(out TransformComponent myTrans)) return;
             if (!target.TryGetComponent(out TransformComponent targetTrans)) return;
             
             float dist = Vector3.Distance(new Vector3(myTrans.posX, 0, myTrans.posY), new Vector3(targetTrans.posX, 0, targetTrans.posY));

             if (dist <= config.attackRange)
             {
                 // Attack!
                 StopMoving(me);
                 if (cooldownTimer <= 0)
                 {
                     Attack(world, me, target);
                     cooldownTimer = config.attackCooldown;
                 }
                 
                 // Look at target
                 LookAt(me, targetTrans);
             }
             else if (dist > config.leashRange)
             {
                 // Target too far, give up
                 targetId = -1;
                 StopMoving(me);
                 UnityEngine.Debug.Log($"[SimpleMeleeBehavior] Target too far ({dist} > {config.leashRange}). Leashing.");
             }
             else
             {
                 // Chase
                 if (pathUpdateTimer <= 0)
                 {
                     MoveTo(me, targetTrans.posX, targetTrans.posY);
                     pathUpdateTimer = PATH_UPDATE_INTERVAL;
                 }
             }
        }

        private void Attack(ServerWorld world, GameEntity me, GameEntity target)
        {
            // Simple instant damage
            if (target.TryGetComponent(out HealthComponent health))
            {
                health.ApplyDamage(config.attackDamage);
                
                // Visual Feedback: Send AbilityCastedEvent (using a generic "Attack" ID or similar)
                // For now, we use a placeholder ID "melee_attack" which client can map to a sound/effect
                world.EnqueueEvent(new AbilityCastedEvent
                {
                    SourceId = "melee_attack", // Client needs to handle this or ignore it
                    CasterId = me.Id,
                    TargetX = target.GetComponent<TransformComponent>().posX,
                    TargetY = target.GetComponent<TransformComponent>().posY
                });
                
                UnityEngine.Debug.Log($"[SimpleMeleeBehavior] {me.Id} attacked {target.Id} for {config.attackDamage} dmg!");
            }
        }

        private void MoveTo(GameEntity me, float x, float y)
        {
            if (me.TryGetComponent(out MovementComponent move))
            {
                move.destX = x;
                move.destY = y;
                move.hasDestination = true;
                move.moveSpeed = config.chaseSpeed; // Ensure speed is set from AI config
            }
        }

        private void StopMoving(GameEntity me)
        {
             if (me.TryGetComponent(out MovementComponent move))
            {
                move.hasDestination = false;
                move.velX = 0; move.velY = 0;
            }
        }
        
        private void LookAt(GameEntity me, TransformComponent targetTrans)
        {
            if (!me.TryGetComponent(out TransformComponent myTrans)) return;
            float dx = targetTrans.posX - myTrans.posX;
            float dy = targetTrans.posY - myTrans.posY;
            if (Mathf.Abs(dx) > 0.01f || Mathf.Abs(dy) > 0.01f)
            {
                myTrans.rotZ = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
            }
        }

        private float GetDistSq(TransformComponent a, TransformComponent b)
        {
            float dx = a.posX - b.posX;
            float dy = a.posY - b.posY;
            return dx * dx + dy * dy;
        }
    }
}
