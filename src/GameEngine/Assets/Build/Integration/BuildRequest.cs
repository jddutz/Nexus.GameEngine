namespace Nexus.GameEngine.Assets.Build.Integration;

/// <summary>
/// Request for asset processing during build.
/// </summary>
public class BuildRequest
{
    /// <summary>
    /// List of input directories to scan for assets.
    /// </summary>
    public List<string> InputDirectories { get; set; } = [];

    /// <summary>
    /// Output directory for processed assets.
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Target platform for processing.
    /// </summary>
    public string TargetPlatform { get; set; } = string.Empty;

    /// <summary>
    /// Build configuration (Debug, Release, etc.).
    /// </summary>
    public string Configuration { get; set; } = string.Empty;

    /// <summary>
    /// File patterns to include (default: all files).
    /// </summary>
    public List<string> IncludePatterns { get; set; } = [];

    /// <summary>
    /// File patterns to exclude.
    /// </summary>
    public List<string> ExcludePatterns { get; set; } = [];

    /// <summary>
    /// Whether to force rebuild all assets.
    /// </summary>
    public bool ForceRebuild { get; set; }

    /// <summary>
    /// Maximum number of concurrent processing operations.
    /// </summary>
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Processing options to pass to processors.
    /// </summary>
    public Dictionary<string, object> ProcessingOptions { get; set; } = [];

    /// <summary>
    /// Whether to generate a build manifest file.
    /// </summary>
    public bool GenerateManifest { get; set; } = true;
}