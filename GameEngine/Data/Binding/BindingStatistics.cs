namespace Main.Data
{
    public struct BindingStatistics
    {
        public int ActiveBindings { get; }
        public long TotalUpdates { get; }
        public double AverageUpdateTime { get; }
        public int ErrorCount { get; }
        public double UpdateRate { get; }
        public BindingStatistics(int activeBindings, long totalUpdates, double averageUpdateTime, int errorCount, double updateRate)
        {
            ActiveBindings = activeBindings;
            TotalUpdates = totalUpdates;
            AverageUpdateTime = averageUpdateTime;
            ErrorCount = errorCount;
            UpdateRate = updateRate;
        }
    }
}
