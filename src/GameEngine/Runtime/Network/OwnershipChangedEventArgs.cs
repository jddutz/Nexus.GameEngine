namespace Nexus.GameEngine.Data.Binding
{
    public class OwnershipChangedEventArgs(INetworkSync networkSync, string? previousOwner, string newOwner, OwnershipChangeReasonEnum reason) : EventArgs
    {
        public INetworkSync NetworkSync { get; } = networkSync;
        public string? PreviousOwner { get; } = previousOwner;
        public string NewOwner { get; } = newOwner;
        public OwnershipChangeReasonEnum Reason { get; } = reason;
    }
}
