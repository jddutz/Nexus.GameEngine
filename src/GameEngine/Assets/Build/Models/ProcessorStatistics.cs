namespace Nexus.GameEngine.Assets.Build.Models;

/// <summary>
/// Statistics for asset processing operations.
/// </summary>
public record ProcessorStatistics
{
    /// <summary>
    /// Time taken for processing.
    /// </summary>
    public TimeSpan ProcessingTime { get; init; }

    /// <summary>
    /// Size of input file in bytes.
    /// </summary>
    public long InputSizeBytes { get; init; }

    /// <summary>
    /// Size of output file(s) in bytes.
    /// </summary>
    public long OutputSizeBytes { get; init; }

    /// <summary>
    /// Compression ratio achieved (if applicable).
    /// </summary>
    public double CompressionRatio { get; init; } = 1.0;

    /// <summary>
    /// Number of warnings generated.
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Number of errors encountered.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Number of registered processors.
    /// </summary>
    public int ProcessorCount { get; init; }

    /// <summary>
    /// List of supported file extensions.
    /// </summary>
    public List<string> SupportedExtensions { get; init; } = [];

    /// <summary>
    /// List of supported platforms.
    /// </summary>
    public List<string> SupportedPlatforms { get; init; } = [];

    /// <summary>
    /// Processors grouped by priority.
    /// </summary>
    public Dictionary<int, List<string>> ProcessorsByPriority { get; init; } = [];

    /// <summary>
    /// Additional performance metrics.
    /// </summary>
    public Dictionary<string, object> Metrics { get; init; } = [];
}