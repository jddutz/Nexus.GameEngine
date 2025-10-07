namespace Nexus.GameEngine.Assets.Build.Models;

/// <summary>
/// Processing priority for asset operations.
/// </summary>
public enum ProcessingPriorityEnum
{
    /// <summary>
    /// Low priority processing.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority processing.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority processing.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority processing.
    /// </summary>
    Critical = 3
}