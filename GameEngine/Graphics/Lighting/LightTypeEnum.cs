namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Types of light sources available.
/// </summary>
public enum LightTypeEnum
{
    /// <summary>
    /// Directional light (like the sun) - affects all objects equally regardless of position.
    /// </summary>
    Directional,

    /// <summary>
    /// Point light - emits light in all directions from a specific position.
    /// </summary>
    Point,

    /// <summary>
    /// Spot light - emits light in a cone shape from a specific position and direction.
    /// </summary>
    Spot,

    /// <summary>
    /// Ambient light - provides uniform lighting to all objects (no specific position or direction).
    /// </summary>
    Ambient
}