using Silk.NET.Maths;

namespace Nexus.GameEngine.GUI;

/// <summary>
/// Absolute or relative positioning
/// </summary>
public record Position(float X, float Y, PositionType Type = PositionType.Relative)
{
    public static Position Zero => new(0, 0);
    public Vector2D<float> ToVector2() => new(X, Y);
}