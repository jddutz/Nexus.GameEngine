namespace Main.Data
{
    public enum ConflictResolutionStrategyEnum
    {
        LocalWins,
        RemoteWins,
        OwnerWins,
        TimestampWins,
        Merge,
        Custom
    }
}
