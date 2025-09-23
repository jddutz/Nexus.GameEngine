using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Triggers;

/// <summary>
/// Represents an object detected by a trigger.
/// </summary>
public struct TriggerContact(object detectedObject, Vector2D<float> position, Vector2D<float> velocity,
                     HashSet<string> tags, int layer, float distanceFromCenter,
                     bool isRigidbody, bool isKinematic, bool isStatic)
{
    /// <summary>
    /// Gets the detected object.
    /// </summary>
    public object DetectedObject { get; } = detectedObject;

    /// <summary>
    /// Gets the position of the detected object.
    /// </summary>
    public Vector2D<float> Position { get; } = position;

    /// <summary>
    /// Gets the velocity of the detected object (if available).
    /// </summary>
    public Vector2D<float> Velocity { get; } = velocity;

    /// <summary>
    /// Gets the tags associated with the detected object.
    /// </summary>
    public HashSet<string> Tags { get; } = tags ?? [];

    /// <summary>
    /// Gets the layer of the detected object.
    /// </summary>
    public int Layer { get; } = layer;

    /// <summary>
    /// Gets the time when the object entered the trigger.
    /// </summary>
    public DateTime EntryTime { get; } = DateTime.UtcNow;

    /// <summary>
    /// Gets how long the object has been in the trigger.
    /// </summary>
    public TimeSpan DwellTime => DateTime.UtcNow - EntryTime;

    /// <summary>
    /// Gets the distance from the trigger center to the object.
    /// </summary>
    public float DistanceFromCenter { get; } = distanceFromCenter;

    /// <summary>
    /// Gets whether this is a rigidbody object.
    /// </summary>
    public bool IsRigidbody { get; } = isRigidbody;

    /// <summary>
    /// Gets whether this is a kinematic object.
    /// </summary>
    public bool IsKinematic { get; } = isKinematic;

    /// <summary>
    /// Gets whether this is a static object.
    /// </summary>
    public bool IsStatic { get; } = isStatic;

    /// <summary>
    /// Checks if the object has a specific tag.
    /// </summary>
    /// <param name="tag">The tag to check for.</param>
    /// <returns>True if the object has the tag, false otherwise.</returns>
    public bool HasTag(string tag)
    {
        return Tags.Contains(tag);
    }
}