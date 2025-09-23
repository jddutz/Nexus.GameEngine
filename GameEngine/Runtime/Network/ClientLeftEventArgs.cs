namespace Main.Data
{
    public class ClientLeftEventArgs : EventArgs
    {
        public string ClientId { get; }
        public string? Reason { get; }
        public ClientLeftEventArgs(string clientId, string? reason = null)
        {
            ClientId = clientId;
            Reason = reason;
        }
    }
}
