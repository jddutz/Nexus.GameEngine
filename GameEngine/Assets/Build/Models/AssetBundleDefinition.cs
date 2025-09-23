namespace Nexus.GameEngine.Assets.Build.Models;

/// <summary>
/// Asset bundle definition.
/// </summary>
public record AssetBundleDefinition
{
    /// <summary>
    /// Bundle name.
    /// </summary>
    public string BundleName { get; init; } = string.Empty;

    /// <summary>
    /// Assets to include in the bundle.
    /// </summary>
    public List<string> AssetPaths { get; init; } = [];

    /// <summary>
    /// Target platform for the bundle.
    /// </summary>
    public TargetPlatformEnum TargetPlatformEnum { get; init; } = TargetPlatformEnum.Universal;

    /// <summary>
    /// Compression method for the bundle.
    /// </summary>
    public CompressionMethodEnum CompressionMethodEnum { get; init; } = CompressionMethodEnum.Automatic;

    /// <summary>
    /// Bundle priority for loading.
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Bundle dependencies.
    /// </summary>
    public List<string> Dependencies { get; init; } = [];
}