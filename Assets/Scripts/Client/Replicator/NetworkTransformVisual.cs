using UnityEngine;
using System.IO;
using ServerGame.Entities;

namespace Client.Replicator
{
    public class NetworkTransformVisual : MonoBehaviour, INetworkComponentVisual
    {
        public int TargetComponentType => (int)ComponentType.Transform;

        private readonly TransformComponent transformComp = new TransformComponent();

        public void OnNetworkUpdate(BinaryReader reader)
        {
            transformComp.Deserialize(reader);
            
            // Apply logic (Interpolation could be added here)
            transform.position = new Vector3(transformComp.posX, transform.position.y, transformComp.posY);
            transform.rotation = Quaternion.Euler(0f, transformComp.rotZ, 0f);
        }
    }
}
