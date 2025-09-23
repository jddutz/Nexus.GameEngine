namespace Main.Data
{
    public class ClientJoinedEventArgs : EventArgs
    {
        public string ClientId { get; }
        public Dictionary<string, object> ClientInfo { get; }
        public ClientJoinedEventArgs(string clientId, Dictionary<string, object> clientInfo)
        {
            ClientId = clientId;
            ClientInfo = clientInfo;
        }
    }
}
