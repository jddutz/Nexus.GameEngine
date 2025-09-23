namespace Nexus.GameEngine.Assets.Build.Integration;

/// <summary>
/// Request for cleaning processed assets.
/// </summary>
public class CleanRequest
{
    /// <summary>
    /// Output directory to clean.
    /// </summary>
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Target platform to clean.
    /// </summary>
    public string TargetPlatform { get; set; } = string.Empty;

    /// <summary>
    /// Build configuration to clean.
    /// </summary>
    public string Configuration { get; set; } = string.Empty;
}