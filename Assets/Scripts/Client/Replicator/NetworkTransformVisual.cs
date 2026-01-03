using UnityEngine;
using System.IO;
using ServerGame.Entities;


namespace Client.Replicator
{
    public class NetworkTransformVisual : MonoBehaviour, INetworkComponentVisual
    {
        public int TargetComponentType => (int)ComponentType.Transform;

        private readonly TransformComponent transformComp = new TransformComponent();

        // Interpolation State
        private Vector3 targetPos;
        private Quaternion targetRot;
        private float lerpSpeed = 10f; // Simple smoothing

        // Prediction State
        private Vector3[] pathCorners;
        private int currentCornerIdx;
        private bool isMovingPredicted;
        
        public float PredictionSpeed = 5f; // Updated by NetworkMovementVisual

        private const float RECONCILE_THRESHOLD = 2.0f; // If error > 2 units, snap back
        private bool initialized = false;

        private void Awake()
        {
            targetPos = transform.position;
            targetRot = transform.rotation;
        }

        public void OnNetworkUpdate(BinaryReader reader)
        {
            transformComp.Deserialize(reader);
            
            Vector3 serverPos = new Vector3(transformComp.posX, transform.position.y, transformComp.posY);
            Quaternion serverRot = Quaternion.Euler(0f, transformComp.rotZ, 0f);

            if (!initialized)
            {
                // First update: Snap immediately to avoid (0,0) interpolation
                transform.position = serverPos;
                transform.rotation = serverRot;
                targetPos = serverPos;
                targetRot = serverRot;
                initialized = true;
                return;
            }

            if (GameSettings.UseMovementPrediction)
            {
                // Reconcile
                float dist = Vector3.Distance(transform.position, serverPos);
                // Increase threshold slightly or check for teleport flag if available
                if (dist > RECONCILE_THRESHOLD)
                {
                    // Hard snap if error is too large (Teleport)
                    transform.position = serverPos;
                    isMovingPredicted = false;
                    targetPos = serverPos; // Sync target for interpolation fallback
                }
            }
            else
            {
                // Prepare for interpolation
                float dist = Vector3.Distance(targetPos, serverPos); 
                if (dist > RECONCILE_THRESHOLD) // Detect teleport
                {
                     transform.position = serverPos;
                     targetPos = serverPos;
                     targetRot = serverRot;
                }
                else
                {
                    targetPos = serverPos;
                    targetRot = serverRot;
                }
            }
        }

        public void PredictMovement(Vector3 dest)
        {
            pathCorners = Shared.Utils.PathfindingService.CalculatePath(transform.position, dest);
            if (pathCorners != null && pathCorners.Length > 0)
            {
                currentCornerIdx = 1; // 0 IS start
                isMovingPredicted = true;
            }
        }

        private void Update()
        {
            if (GameSettings.UseMovementPrediction)
            {
                UpdatePrediction();
            }
            else
            {
                UpdateInterpolation();
            }
        }

        private void UpdateInterpolation()
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * lerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lerpSpeed);
        }

        private void UpdatePrediction()
        {
            if (!isMovingPredicted || pathCorners == null) return;

            if (currentCornerIdx >= pathCorners.Length)
            {
                isMovingPredicted = false;
                return;
            }

            Vector3 target = pathCorners[currentCornerIdx];
            target.y = transform.position.y; // Flatten
            Vector3 dir = (target - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, target);
            float step = PredictionSpeed * Time.deltaTime;

            // Use while loop
            while (dist <= step && pathCorners != null && currentCornerIdx < pathCorners.Length)
            {
                transform.position = target;
                step -= dist;
                currentCornerIdx++;
                
                if (currentCornerIdx < pathCorners.Length)
                {
                     target = pathCorners[currentCornerIdx];
                     target.y = transform.position.y;
                     
                     dir = (target - transform.position).normalized;
                     dist = Vector3.Distance(transform.position, target);
                     if (dist > 0.001f) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                }
                else
                {
                    isMovingPredicted = false;
                    break;
                }
            }

            if (dist > step)
            {
                transform.position += dir * step;
                if (dist > 0.001f) transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }
    }
}
