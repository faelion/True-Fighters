using UnityEngine;
using ServerGame.Entities;
using System.IO;

namespace Client.Replicator
{
    public class NetworkCooldownData : MonoBehaviour, INetworkComponentVisual
    {
        public int TargetComponentType => (int)ComponentType.Cooldown;

        public float cdQ, maxQ;
        public float cdW, maxW;
        public float cdE, maxE;
        public float cdR, maxR;

        public void OnNetworkUpdate(BinaryReader reader)
        {
            cdQ = reader.ReadSingle(); maxQ = reader.ReadSingle();
            cdW = reader.ReadSingle(); maxW = reader.ReadSingle();
            cdE = reader.ReadSingle(); maxE = reader.ReadSingle();
            cdR = reader.ReadSingle(); maxR = reader.ReadSingle();
        }

        void Update()
        {
            // Simulate locally for smooth UI
            if (cdQ > 0) cdQ -= Time.deltaTime;
            if (cdW > 0) cdW -= Time.deltaTime;
            if (cdE > 0) cdE -= Time.deltaTime;
            if (cdR > 0) cdR -= Time.deltaTime;

            if (cdQ < 0) cdQ = 0;
            if (cdW < 0) cdW = 0;
            if (cdE < 0) cdE = 0;
            if (cdR < 0) cdR = 0;
        }
    }
}
