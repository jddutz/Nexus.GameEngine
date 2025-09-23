namespace Main.Data
{
    public class NetworkUpdate
    {
        public string NetworkId { get; }
        public string SenderId { get; }
        public int StateVersion { get; }
        public DateTime Timestamp { get; }
        public bool IsDelta { get; }
        public Dictionary<string, object> Properties { get; }
        public Dictionary<string, object> Metadata { get; }
        public NetworkReliabilityEnum Reliability { get; }
        public NetworkPriorityEnum Priority { get; }
        public NetworkUpdate(string networkId, string senderId, int stateVersion, bool isDelta,
                        Dictionary<string, object> properties, NetworkReliabilityEnum reliability, NetworkPriorityEnum priority)
        {
            NetworkId = networkId;
            SenderId = senderId;
            StateVersion = stateVersion;
            Timestamp = DateTime.UtcNow;
            IsDelta = isDelta;
            Properties = properties;
            Metadata = [];
            Reliability = reliability;
            Priority = priority;
        }
        public object? GetProperty(string propertyName) => Properties.TryGetValue(propertyName, out var value) ? value : null;
        public void SetProperty(string propertyName, object value) => Properties[propertyName] = value;
        public bool HasProperty(string propertyName) => Properties.ContainsKey(propertyName);
    }
}
