using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Provides data for boundary reached events.
/// </summary>
public class BoundaryReachedEventArgs(Rectangle<float> boundary, BoundaryEdgeEnum edge, Vector2D<float> contactPoint) : EventArgs
{
    public Rectangle<float> Boundary { get; } = boundary;
    public BoundaryEdgeEnum Edge { get; } = edge;
    public Vector2D<float> ContactPoint { get; } = contactPoint;
}
