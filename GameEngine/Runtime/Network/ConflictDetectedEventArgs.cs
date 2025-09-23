namespace Main.Data
{
    public class ConflictDetectedEventArgs : EventArgs
    {
        public INetworkSync NetworkSync { get; }
        public object LocalState { get; }
        public object RemoteState { get; }
        public string PropertyName { get; }
        public object? ResolvedState { get; set; }
        public ConflictDetectedEventArgs(INetworkSync networkSync, object localState, object remoteState, string propertyName)
        {
            NetworkSync = networkSync;
            LocalState = localState;
            RemoteState = remoteState;
            PropertyName = propertyName;
        }
    }
}
