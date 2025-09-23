namespace Main.Data
{
    public class NetworkSyncErrorEventArgs : EventArgs
    {
        public INetworkSync NetworkSync { get; }
        public Exception Exception { get; }
        public NetworkUpdate? Update { get; }
        public bool Ignore { get; set; }
        public NetworkSyncErrorEventArgs(INetworkSync networkSync, Exception exception, NetworkUpdate? update = null)
        {
            NetworkSync = networkSync;
            Exception = exception;
            Update = update;
        }
    }
}
