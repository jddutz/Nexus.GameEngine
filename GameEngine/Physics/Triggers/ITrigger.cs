using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Triggers;

/// <summary>
/// Represents a component that detects when other objects enter or exit its area without physical collision response.
/// Triggers are ideal for detection zones, pickups, checkpoints, and area-based events.
/// </summary>
public interface ITrigger
{
    /// <summary>
    /// Gets or sets whether trigger detection is enabled.
    /// </summary>
    bool IsTriggerEnabled { get; set; }

    /// <summary>
    /// Gets or sets the trigger shape type.
    /// </summary>
    TriggerShapeEnum TriggerShapeEnum { get; set; }

    /// <summary>
    /// Gets or sets the trigger area bounds.
    /// </summary>
    Rectangle<float> TriggerBounds { get; set; }

    /// <summary>
    /// Gets or sets the trigger radius (for circle/sphere shapes).
    /// </summary>
    float TriggerRadius { get; set; }

    /// <summary>
    /// Gets or sets the trigger layer this component belongs to.
    /// </summary>
    int TriggerLayer { get; set; }

    /// <summary>
    /// Gets or sets which layers this trigger can detect.
    /// </summary>
    int TriggerMask { get; set; }

    /// <summary>
    /// Gets or sets the trigger detection mode.
    /// </summary>
    TriggerDetectionModeEnum DetectionMode { get; set; }

    /// <summary>
    /// Gets or sets whether the trigger should detect rigidbodies.
    /// </summary>
    bool DetectRigidbodies { get; set; }

    /// <summary>
    /// Gets or sets whether the trigger should detect kinematic objects.
    /// </summary>
    bool DetectKinematic { get; set; }

    /// <summary>
    /// Gets or sets whether the trigger should detect static colliders.
    /// </summary>
    bool DetectStatic { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of objects this trigger can detect simultaneously.
    /// </summary>
    int MaxDetectedObjects { get; set; }

    /// <summary>
    /// Gets or sets tags that this trigger should specifically look for.
    /// </summary>
    HashSet<string> TargetTags { get; set; }

    /// <summary>
    /// Gets or sets tags that this trigger should ignore.
    /// </summary>
    HashSet<string> IgnoredTags { get; set; }

    /// <summary>
    /// Gets the current objects within the trigger area.
    /// </summary>
    IReadOnlyList<TriggerContact> ObjectsInTrigger { get; }

    /// <summary>
    /// Gets whether there are any objects currently in the trigger.
    /// </summary>
    bool HasObjectsInTrigger { get; }

    /// <summary>
    /// Gets the number of objects currently in the trigger.
    /// </summary>
    int ObjectCount { get; }

    /// <summary>
    /// Gets or sets the cooldown time between trigger events for the same object.
    /// </summary>
    float TriggerCooldown { get; set; }

    /// <summary>
    /// Gets or sets whether the trigger should remain active after being triggered.
    /// </summary>
    bool IsReusable { get; set; }

    /// <summary>
    /// Gets whether the trigger has been activated (for one-time triggers).
    /// </summary>
    bool HasBeenTriggered { get; }

    /// <summary>
    /// Occurs when an object enters the trigger area.
    /// </summary>
    event EventHandler<TriggerEventArgs> TriggerEnter;

    /// <summary>
    /// Occurs while an object remains in the trigger area.
    /// </summary>
    event EventHandler<TriggerEventArgs> TriggerStay;

    /// <summary>
    /// Occurs when an object exits the trigger area.
    /// </summary>
    event EventHandler<TriggerEventArgs> TriggerExit;

    /// <summary>
    /// Occurs when the trigger is activated (customizable activation condition).
    /// </summary>
    event EventHandler<TriggerActivatedEventArgs> TriggerActivated;

    /// <summary>
    /// Occurs when the trigger is deactivated.
    /// </summary>
    event EventHandler<TriggerActivatedEventArgs> TriggerDeactivated;

    /// <summary>
    /// Checks if a specific object is currently in the trigger.
    /// </summary>
    /// <param name="obj">The object to check for.</param>
    /// <returns>True if the object is in the trigger, false otherwise.</returns>
    bool ContainsObject(object obj);

    /// <summary>
    /// Checks if an object with a specific tag is in the trigger.
    /// </summary>
    /// <param name="tag">The tag to search for.</param>
    /// <returns>True if an object with the tag is found, false otherwise.</returns>
    bool ContainsObjectWithTag(string tag);

    /// <summary>
    /// Gets all objects in the trigger with a specific tag.
    /// </summary>
    /// <param name="tag">The tag to filter by.</param>
    /// <returns>Collection of trigger contacts with the specified tag.</returns>
    IEnumerable<TriggerContact> GetObjectsWithTag(string tag);

    /// <summary>
    /// Checks if a point is within the trigger area.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns>True if the point is in the trigger area, false otherwise.</returns>
    bool ContainsPoint(Vector2D<float> point);

    /// <summary>
    /// Gets the closest object to a specific position within the trigger.
    /// </summary>
    /// <param name="position">The position to find the closest object to.</param>
    /// <returns>The closest trigger contact, or null if none found.</returns>
    TriggerContact? GetClosestObject(Vector2D<float> position);

    /// <summary>
    /// Forces a manual check for objects in the trigger area.
    /// </summary>
    void ForceCheck();

    /// <summary>
    /// Resets the trigger to its initial state (for reusable triggers).
    /// </summary>
    void Reset();

    /// <summary>
    /// Enables or disables detection for a specific layer.
    /// </summary>
    /// <param name="layer">The layer to modify.</param>
    /// <param name="enabled">Whether detection should be enabled.</param>
    void SetLayerDetection(int layer, bool enabled);

    /// <summary>
    /// Gets whether detection is enabled for a specific layer.
    /// </summary>
    /// <param name="layer">The layer to check.</param>
    /// <returns>True if detection is enabled, false otherwise.</returns>
    bool GetLayerDetection(int layer);

    /// <summary>
    /// Adds a tag to the target tags list.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    void AddTargetTag(string tag);

    /// <summary>
    /// Removes a tag from the target tags list.
    /// </summary>
    /// <param name="tag">The tag to remove.</param>
    void RemoveTargetTag(string tag);

    /// <summary>
    /// Adds a tag to the ignored tags list.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    void AddIgnoredTag(string tag);

    /// <summary>
    /// Removes a tag from the ignored tags list.
    /// </summary>
    /// <param name="tag">The tag to remove.</param>
    void RemoveIgnoredTag(string tag);

    /// <summary>
    /// Updates the trigger bounds based on current transform.
    /// </summary>
    void UpdateTriggerBounds();
}
