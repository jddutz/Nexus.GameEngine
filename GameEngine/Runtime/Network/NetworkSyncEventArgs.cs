namespace Main.Data
{
    public class NetworkSyncEventArgs : EventArgs
    {
        public INetworkSync NetworkSync { get; }
        public NetworkUpdate Update { get; }
        public SyncDirectionEnum Direction { get; }
        public bool Cancel { get; set; }
        public NetworkSyncEventArgs(INetworkSync networkSync, NetworkUpdate update, SyncDirectionEnum direction)
        {
            NetworkSync = networkSync;
            Update = update;
            Direction = direction;
        }
    }
}
