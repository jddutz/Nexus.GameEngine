namespace Main.Data
{
    public class NetworkConnectionEventArgs(INetworkSync networkSync, bool isConnected, string? reason = null) : EventArgs
    {
        public INetworkSync NetworkSync { get; } = networkSync;
        public bool IsConnected { get; } = isConnected;
        public string? Reason { get; } = reason;
    }
}
