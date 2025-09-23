namespace Nexus.GameEngine.Assets.Build.Integration;

/// <summary>
/// Result of asset cleaning operation.
/// </summary>
public class CleanResult
{
    /// <summary>
    /// Whether the clean was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The clean request that was processed.
    /// </summary>
    public CleanRequest? Request { get; set; }

    /// <summary>
    /// When the clean started.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When the clean ended.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Number of files that were deleted.
    /// </summary>
    public int DeletedFiles { get; set; }

    /// <summary>
    /// Error message if the clean failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Total clean duration.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;
}