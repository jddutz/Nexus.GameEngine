namespace Nexus.GameEngine.Physics.Triggers;

/// <summary>
/// Represents different trigger shape types.
/// </summary>
public enum TriggerShapeEnum
{
    /// <summary>
    /// No trigger shape.
    /// </summary>
    None,

    /// <summary>
    /// Rectangular trigger area.
    /// </summary>
    Rectangle,

    /// <summary>
    /// Circular trigger area.
    /// </summary>
    Circle,

    /// <summary>
    /// Elliptical trigger area.
    /// </summary>
    Ellipse,

    /// <summary>
    /// Polygonal trigger area with custom vertices.
    /// </summary>
    Polygon,

    /// <summary>
    /// Capsule-shaped trigger area.
    /// </summary>
    Capsule,

    /// <summary>
    /// Spherical trigger area (3D).
    /// </summary>
    Sphere,

    /// <summary>
    /// Box-shaped trigger area (3D).
    /// </summary>
    Box,

    /// <summary>
    /// Custom trigger shape defined by the implementation.
    /// </summary>
    Custom
}