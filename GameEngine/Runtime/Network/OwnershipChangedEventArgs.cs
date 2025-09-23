namespace Main.Data
{
    public class OwnershipChangedEventArgs : EventArgs
    {
        public INetworkSync NetworkSync { get; }
        public string? PreviousOwner { get; }
        public string NewOwner { get; }
        public OwnershipChangeReasonEnum Reason { get; }
        public OwnershipChangedEventArgs(INetworkSync networkSync, string? previousOwner, string newOwner, OwnershipChangeReasonEnum reason)
        {
            NetworkSync = networkSync;
            PreviousOwner = previousOwner;
            NewOwner = newOwner;
            Reason = reason;
        }
    }
}
