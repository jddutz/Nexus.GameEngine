namespace Nexus.GameEngine.Assets.Build.Integration;

/// <summary>
/// Result of asset processing build operation.
/// </summary>
public class BuildResult
{
    /// <summary>
    /// Whether the build was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The build request that was processed.
    /// </summary>
    public BuildRequest? Request { get; set; }

    /// <summary>
    /// When the build started.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// When the build ended.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Number of assets that were processed.
    /// </summary>
    public int ProcessedAssets { get; set; }

    /// <summary>
    /// Number of successfully processed assets.
    /// </summary>
    public int SuccessfulAssets { get; set; }

    /// <summary>
    /// Number of failed assets.
    /// </summary>
    public int FailedAssets { get; set; }

    /// <summary>
    /// List of output files created.
    /// </summary>
    public List<string> OutputFiles { get; set; } = [];

    /// <summary>
    /// List of errors that occurred.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// List of warnings that occurred.
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Path to the generated build manifest file.
    /// </summary>
    public string? ManifestFile { get; set; }

    /// <summary>
    /// Total build duration.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;
}