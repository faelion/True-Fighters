using UnityEngine;
using System.IO;
using ServerGame.Entities;

namespace Client.Replicator
{
    public class NetworkMovementVisual : MonoBehaviour, INetworkComponentVisual
    {
        public int TargetComponentType => (int)ComponentType.Movement;

        private NetworkTransformVisual transformVisual;

        private void Awake()
        {
            transformVisual = GetComponent<NetworkTransformVisual>();
        }

        public void OnNetworkUpdate(BinaryReader reader)
        {
            // Deserialize strictly matching MovementComponent structure
            float speed = reader.ReadSingle();
            float vx = reader.ReadSingle();
            float vy = reader.ReadSingle();
            bool hasDest = reader.ReadBoolean();
            if (hasDest)
            {
                reader.ReadSingle(); // destX
                reader.ReadSingle(); // destY
            }

            // Apply Speed to Transform Visual for Prediction
            if (transformVisual != null)
            {
                transformVisual.PredictionSpeed = speed;
            }
        }
    }
}
