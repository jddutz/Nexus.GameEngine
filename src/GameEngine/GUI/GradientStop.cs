using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI;

/// <summary>
/// Defines a color stop in a gradient at a specific position.
/// Position is normalized (0.0 to 1.0) along the gradient direction.
/// </summary>
/// <param name="Position">Normalized position (0.0 = start, 1.0 = end). Values outside [0,1] are allowed.</param>
/// <param name="Color">RGBA color at this position.</param>
public readonly record struct GradientStop(float Position, Vector4D<float> Color)
{
    /// <summary>
    /// Validates that the gradient stop has valid values.
    /// </summary>
    public void Validate()
    {
        if (float.IsNaN(Position) || float.IsInfinity(Position))
            throw new ArgumentException($"Invalid gradient stop position: {Position}");
            
        if (float.IsNaN(Color.X) || float.IsNaN(Color.Y) || float.IsNaN(Color.Z) || float.IsNaN(Color.W))
            throw new ArgumentException($"Invalid gradient stop color (contains NaN)");
    }
}
