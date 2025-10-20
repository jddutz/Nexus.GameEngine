namespace Nexus.GameEngine.Data.Binding
{
    public class NetworkSyncEventArgs(INetworkSync networkSync, NetworkUpdate update, SyncDirectionEnum direction) : EventArgs
    {
        public INetworkSync NetworkSync { get; } = networkSync;
        public NetworkUpdate Update { get; } = update;
        public SyncDirectionEnum Direction { get; } = direction;
        public bool Cancel { get; set; }
    }
}
