namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Provides data for waypoint reached events.
/// </summary>
public class WaypointReachedEventArgs(Vector2D<float> waypoint, int waypointIndex, bool isLastWaypoint, bool willLoop) : EventArgs
{
    public Vector2D<float> Waypoint { get; } = waypoint;
    public int WaypointIndex { get; } = waypointIndex;
    public bool IsLastWaypoint { get; } = isLastWaypoint;
    public bool WillLoop { get; } = willLoop;
}
