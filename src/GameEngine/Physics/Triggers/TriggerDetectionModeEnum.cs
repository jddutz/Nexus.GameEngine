namespace Nexus.GameEngine.Physics.Triggers;

/// <summary>
/// Specifies different trigger detection modes.
/// </summary>
public enum TriggerDetectionModeEnum
{
    /// <summary>
    /// Detect when any part of an object overlaps the trigger.
    /// </summary>
    Overlap,

    /// <summary>
    /// Detect only when an object's center is within the trigger.
    /// </summary>
    CenterPoint,

    /// <summary>
    /// Detect when an object is fully contained within the trigger.
    /// </summary>
    FullyContained,

    /// <summary>
    /// Detect when an object's bounds intersect the trigger bounds.
    /// </summary>
    BoundsIntersection,

    /// <summary>
    /// Custom detection logic defined by the implementation.
    /// </summary>
    Custom
}