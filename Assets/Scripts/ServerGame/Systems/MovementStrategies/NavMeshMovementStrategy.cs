using UnityEngine;
using ServerGame.Entities;
using Shared.ScriptableObjects;
using Shared.Utils;

namespace ServerGame.Systems.MovementStrategies
{
    [CreateAssetMenu(fileName = "NavMeshMovementStrategy", menuName = "TrueFighters/Movement/NavMesh Strategy")]
    public class NavMeshMovementStrategy : MovementStrategySO
    {
        public bool debugDraw = false;

        public override void UpdateMovement(ServerGame.ServerWorld world, GameEntity entity, float dt)
        {
            if (!entity.TryGetComponent(out MovementComponent move)) return;
            if (!entity.TryGetComponent(out TransformComponent trans)) return;

            if (!move.hasDestination)
            {
                move.velX = 0;
                move.velY = 0;
                move.pathCorners = null; // Clear path
                return;
            }

            // 1. Calculate Path if missing or dirty
            bool needPath = move.pathCorners == null || move.pathCorners.Length == 0;
            if (!needPath)
            {
                var end = move.pathCorners[move.pathCorners.Length - 1];
                if (Vector3.SqrMagnitude(end - new Vector3(move.destX, 0, move.destY)) > 0.1f)
                {
                    needPath = true;
                }
            }

            Vector3 currentPos = new Vector3(trans.posX, 0, trans.posY);

            if (needPath)
            {
                Vector3 target = new Vector3(move.destX, 0, move.destY);
                move.pathCorners = PathfindingService.CalculatePath(currentPos, target);
                move.currentCornerIdx = 1; // 0 is start
                move.pathDirty = false;
            }

            // 2. Follow Path
            if (move.pathCorners != null && move.currentCornerIdx < move.pathCorners.Length)
            {
                Vector3 nextTarget = move.pathCorners[move.currentCornerIdx];
                
                nextTarget.y = 0;
                Vector3 dir = (nextTarget - currentPos).normalized;
                float dist = Vector3.Distance(currentPos, nextTarget);
                float step = move.moveSpeed * dt;

                // Use while loop to handle passing multiple corners in one frame if speed is high
                while (dist <= step && move.pathCorners != null && move.currentCornerIdx < move.pathCorners.Length)
                {
                    trans.posX = nextTarget.x;
                    trans.posY = nextTarget.z;
                    
                    step -= dist; 
                    move.currentCornerIdx++;
                    
                    if (move.currentCornerIdx < move.pathCorners.Length)
                    {
                        nextTarget = move.pathCorners[move.currentCornerIdx];
                        nextTarget.y = 0;
                        
                        currentPos = new Vector3(trans.posX, 0, trans.posY);
                        dist = Vector3.Distance(currentPos, nextTarget);
                        dir = (nextTarget - currentPos).normalized;
                        
                        if (dist > 0.001f)
                        {
                            trans.rotZ = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                            move.velX = dir.x * move.moveSpeed;
                            move.velY = dir.z * move.moveSpeed;
                        }
                    }
                    else
                    {
                         move.hasDestination = false;
                         move.velX = 0; move.velY = 0;
                         break;
                    }
                }

                if (move.hasDestination && dist > step)
                {
                    trans.posX += dir.x * step;
                    trans.posY += dir.z * step;
                    
                    if (dist > 0.001f)
                    {
                        trans.rotZ = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                        move.velX = dir.x * move.moveSpeed;
                        move.velY = dir.z * move.moveSpeed;
                    }
                }
            }
            else
            {
                // Arrived at final destination
                move.hasDestination = false;
                move.velX = 0;
                move.velY = 0;
            }
        }
    }
}
