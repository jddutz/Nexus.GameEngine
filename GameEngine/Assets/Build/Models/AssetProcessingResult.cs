namespace Nexus.GameEngine.Assets.Build.Models;

/// <summary>
/// Result of asset processing operations.
/// </summary>
public record AssetProcessingResult
{
    /// <summary>
    /// Whether the processing was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Time taken for processing.
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// List of output files generated.
    /// </summary>
    public List<string> OutputFiles { get; init; } = [];

    /// <summary>
    /// Processing statistics.
    /// </summary>
    public ProcessorStatistics Statistics { get; init; } = new();

    /// <summary>
    /// Additional metadata from processing.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = [];

    /// <summary>
    /// Warnings generated during processing.
    /// </summary>
    public List<string> Warnings { get; init; } = [];

    /// <summary>
    /// The processing context that was used.
    /// </summary>
    public AssetProcessingContext? Context { get; init; }
}