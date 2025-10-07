namespace Main.Data
{
    public class NetworkUpdate(string networkId, string senderId, int stateVersion, bool isDelta,
                    Dictionary<string, object> properties, NetworkReliabilityEnum reliability, NetworkPriorityEnum priority)
    {
        public string NetworkId { get; } = networkId;
        public string SenderId { get; } = senderId;
        public int StateVersion { get; } = stateVersion;
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public bool IsDelta { get; } = isDelta;
        public Dictionary<string, object> Properties { get; } = properties;
        public Dictionary<string, object> Metadata { get; } = [];
        public NetworkReliabilityEnum Reliability { get; } = reliability;
        public NetworkPriorityEnum Priority { get; } = priority;

        public object? GetProperty(string propertyName) => Properties.TryGetValue(propertyName, out var value) ? value : null;
        public void SetProperty(string propertyName, object value) => Properties[propertyName] = value;
        public bool HasProperty(string propertyName) => Properties.ContainsKey(propertyName);
    }
}
