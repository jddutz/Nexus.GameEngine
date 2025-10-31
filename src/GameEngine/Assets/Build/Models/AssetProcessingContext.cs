namespace Nexus.GameEngine.Assets.Build.Models;

/// <summary>
/// Context for asset processing operations.
/// </summary>
public record AssetProcessingContext
{
    /// <summary>
    /// The input asset file path.
    /// </summary>
    public string InputPath { get; init; } = string.Empty;

    /// <summary>
    /// The output asset file path.
    /// </summary>
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>
    /// The output directory for processed assets.
    /// </summary>
    public string OutputDirectory { get; init; } = string.Empty;

    /// <summary>
    /// Target platform for processing.
    /// </summary>
    public TargetPlatformEnum TargetPlatform { get; init; } = TargetPlatformEnum.Universal;

    /// <summary>
    /// Processing priority.
    /// </summary>
    public ProcessingPriorityEnum Priority { get; init; } = ProcessingPriorityEnum.Normal;

    /// <summary>
    /// Optimization level to apply.
    /// </summary>
    public OptimizationLevelEnum OptimizationLevel { get; init; } = OptimizationLevelEnum.Standard;

    /// <summary>
    /// Compression method to use.
    /// </summary>
    public CompressionMethodEnum CompressionMethod { get; init; } = CompressionMethodEnum.Automatic;

    /// <summary>
    /// Build configuration (Debug, Release, etc.).
    /// </summary>
    public string Configuration { get; init; } = "Debug";

    /// <summary>
    /// Processing options.
    /// </summary>
    public Dictionary<string, object> Options { get; init; } = [];

    /// <summary>
    /// Additional metadata for processing.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = [];

    /// <summary>
    /// Build configuration options.
    /// </summary>
    public Dictionary<string, string> BuildOptions { get; init; } = [];

    /// <summary>
    /// Logger for processing operations.
    /// </summary>
    public ILogger? Logger { get; init; }

    /// <summary>
    /// Whether to force processing even if not needed.
    /// </summary>
    public bool ForceProcessing { get; init; }

    /// <summary>
    /// Cancellation token for the processing operation.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;
}