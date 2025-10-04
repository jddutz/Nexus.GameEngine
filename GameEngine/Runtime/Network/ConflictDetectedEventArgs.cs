namespace Main.Data
{
    public class ConflictDetectedEventArgs(INetworkSync networkSync, object localState, object remoteState, string propertyName) : EventArgs
    {
        public INetworkSync NetworkSync { get; } = networkSync;
        public object LocalState { get; } = localState;
        public object RemoteState { get; } = remoteState;
        public string PropertyName { get; } = propertyName;
        public object? ResolvedState { get; set; }
    }
}
