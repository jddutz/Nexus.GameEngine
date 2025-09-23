namespace Main.Data
{
    public struct NetworkStatistics
    {
        public long BytesSent { get; }
        public long BytesReceived { get; }
        public int UpdatesSent { get; }
        public int UpdatesReceived { get; }
        public float AverageLatency { get; }
        public float CurrentLatency { get; }
        public float PacketLossRate { get; }
        public float Bandwidth { get; }
        public float SyncFrequency { get; }
        public NetworkStatistics(long bytesSent, long bytesReceived, int updatesSent, int updatesReceived,
                            float averageLatency, float currentLatency, float packetLossRate,
                            float bandwidth, float syncFrequency)
        {
            BytesSent = bytesSent;
            BytesReceived = bytesReceived;
            UpdatesSent = updatesSent;
            UpdatesReceived = updatesReceived;
            AverageLatency = averageLatency;
            CurrentLatency = currentLatency;
            PacketLossRate = packetLossRate;
            Bandwidth = bandwidth;
            SyncFrequency = syncFrequency;
        }
    }
}
