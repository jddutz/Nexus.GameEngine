using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Provides data for boundary reached events.
/// </summary>
public class BoundaryReachedEventArgs : EventArgs
{
    public Rectangle<float> Boundary { get; }
    public BoundaryEdgeEnum Edge { get; }
    public Vector2D<float> ContactPoint { get; }
    public BoundaryReachedEventArgs(Rectangle<float> boundary, BoundaryEdgeEnum edge, Vector2D<float> contactPoint)
    {
        Boundary = boundary;
        Edge = edge;
        ContactPoint = contactPoint;
    }
}
