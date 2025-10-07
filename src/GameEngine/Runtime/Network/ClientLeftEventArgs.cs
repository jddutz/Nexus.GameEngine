namespace Main.Data
{
    public class ClientLeftEventArgs(string clientId, string? reason = null) : EventArgs
    {
        public string ClientId { get; } = clientId;
        public string? Reason { get; } = reason;
    }
}
