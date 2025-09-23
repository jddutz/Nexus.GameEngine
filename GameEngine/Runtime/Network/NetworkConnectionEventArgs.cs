namespace Main.Data
{
    public class NetworkConnectionEventArgs : EventArgs
    {
        public INetworkSync NetworkSync { get; }
        public bool IsConnected { get; }
        public string? Reason { get; }
        public NetworkConnectionEventArgs(INetworkSync networkSync, bool isConnected, string? reason = null)
        {
            NetworkSync = networkSync;
            IsConnected = isConnected;
            Reason = reason;
        }
    }
}
