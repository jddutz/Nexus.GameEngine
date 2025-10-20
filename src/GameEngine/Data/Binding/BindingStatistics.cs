namespace Nexus.GameEngine.Data.Binding
{
    public struct BindingStatistics(int activeBindings, long totalUpdates, double averageUpdateTime, int errorCount, double updateRate)
    {
        public int ActiveBindings { get; } = activeBindings;
        public long TotalUpdates { get; } = totalUpdates;
        public double AverageUpdateTime { get; } = averageUpdateTime;
        public int ErrorCount { get; } = errorCount;
        public double UpdateRate { get; } = updateRate;
    }
}
