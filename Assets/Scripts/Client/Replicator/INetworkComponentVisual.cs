using System.IO;

namespace Client.Replicator
{
    public interface INetworkComponentVisual
    {
        // The ComponentType this visual is interested in (e.g. ServerGame.Entities.ComponentType.Transform)
        int TargetComponentType { get; }

        // Called when data for this component type is received
        // reader is positioned at the start of the component data
        void OnNetworkUpdate(BinaryReader reader);
    }
}
