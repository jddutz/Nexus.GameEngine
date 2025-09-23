namespace Nexus.GameEngine.Assets.Build.Models;

/// <summary>
/// Optimization level for asset processing.
/// </summary>
public enum OptimizationLevelEnum
{
    /// <summary>
    /// No optimization applied.
    /// </summary>
    None = 0,

    /// <summary>
    /// Basic optimization.
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Standard optimization.
    /// </summary>
    Standard = 2,

    /// <summary>
    /// Aggressive optimization.
    /// </summary>
    Aggressive = 3,

    /// <summary>
    /// Maximum optimization (slowest build time).
    /// </summary>
    Maximum = 4
}