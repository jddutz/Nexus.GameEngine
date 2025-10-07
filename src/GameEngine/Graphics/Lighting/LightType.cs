namespace Nexus.GameEngine.Graphics.Lighting;

/// <summary>
/// Defines the type of light source
/// </summary>
public enum LightType
{
    /// <summary>
    /// Directional light (sun-like, infinite distance)
    /// </summary>
    Directional = 0,

    /// <summary>
    /// Point light (omnidirectional from a point)
    /// </summary>
    Point = 1,

    /// <summary>
    /// Spot light (cone-shaped directional)
    /// </summary>
    Spot = 2
}