using Silk.NET.Maths;

namespace Nexus.GameEngine.Physics.Movement;

/// <summary>
/// Provides data for waypoint reached events.
/// </summary>
public class WaypointReachedEventArgs : EventArgs
{
    public Vector2D<float> Waypoint { get; }
    public int WaypointIndex { get; }
    public bool IsLastWaypoint { get; }
    public bool WillLoop { get; }
    public WaypointReachedEventArgs(Vector2D<float> waypoint, int waypointIndex, bool isLastWaypoint, bool willLoop)
    {
        Waypoint = waypoint;
        WaypointIndex = waypointIndex;
        IsLastWaypoint = isLastWaypoint;
        WillLoop = willLoop;
    }
}
