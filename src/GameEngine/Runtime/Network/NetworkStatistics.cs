namespace Nexus.GameEngine.Data.Binding
{
    public struct NetworkStatistics(long bytesSent, long bytesReceived, int updatesSent, int updatesReceived,
                        float averageLatency, float currentLatency, float packetLossRate,
                        float bandwidth, float syncFrequency)
    {
        public long BytesSent { get; } = bytesSent;
        public long BytesReceived { get; } = bytesReceived;
        public int UpdatesSent { get; } = updatesSent;
        public int UpdatesReceived { get; } = updatesReceived;
        public float AverageLatency { get; } = averageLatency;
        public float CurrentLatency { get; } = currentLatency;
        public float PacketLossRate { get; } = packetLossRate;
        public float Bandwidth { get; } = bandwidth;
        public float SyncFrequency { get; } = syncFrequency;
    }
}
