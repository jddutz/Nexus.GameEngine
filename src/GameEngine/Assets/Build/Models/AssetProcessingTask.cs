namespace Nexus.GameEngine.Assets.Build.Models;

/// <summary>
/// Asset processing task.
/// </summary>
public record AssetProcessingTask
{
    /// <summary>
    /// Asset path to process.
    /// </summary>
    public string AssetPath { get; init; } = string.Empty;

    /// <summary>
    /// Processing priority.
    /// </summary>
    public ProcessingPriorityEnum Priority { get; init; } = ProcessingPriorityEnum.Normal;

    /// <summary>
    /// Target platform.
    /// </summary>
    public TargetPlatformEnum TargetPlatformEnum { get; init; } = TargetPlatformEnum.Universal;

    /// <summary>
    /// Optimization level.
    /// </summary>
    public OptimizationLevelEnum OptimizationLevelEnum { get; init; } = OptimizationLevelEnum.Standard;

    /// <summary>
    /// Compression method.
    /// </summary>
    public CompressionMethodEnum CompressionMethodEnum { get; init; } = CompressionMethodEnum.Automatic;

    /// <summary>
    /// Processing options.
    /// </summary>
    public Dictionary<string, object> Options { get; init; } = [];

    /// <summary>
    /// Estimated processing time.
    /// </summary>
    public TimeSpan EstimatedDuration { get; init; }
}