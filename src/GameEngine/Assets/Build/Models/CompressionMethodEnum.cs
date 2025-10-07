namespace Nexus.GameEngine.Assets.Build.Models;

/// <summary>
/// Compression method for asset processing.
/// </summary>
public enum CompressionMethodEnum
{
    /// <summary>
    /// No compression applied.
    /// </summary>
    None = 0,

    /// <summary>
    /// Automatic compression based on asset type.
    /// </summary>
    Automatic = 1,

    /// <summary>
    /// Fast compression with lower ratio.
    /// </summary>
    Fast = 2,

    /// <summary>
    /// Standard compression with balanced speed/ratio.
    /// </summary>
    Standard = 3,

    /// <summary>
    /// Maximum compression with slower speed.
    /// </summary>
    Maximum = 4,

    /// <summary>
    /// Lossless compression.
    /// </summary>
    Lossless = 5,

    /// <summary>
    /// Lossy compression (for applicable asset types).
    /// </summary>
    Lossy = 6
}