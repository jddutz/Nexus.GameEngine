namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Comparer for sorting viewports by render priority (lower values render first).
/// </summary>
internal class ViewportPriorityComparer : IComparer<Viewport>
{
    public int Compare(Viewport? x, Viewport? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        
        // Primary sort by RenderPriority
        var priorityComparison = x.RenderPriority.CompareTo(y.RenderPriority);
        if (priorityComparison != 0) return priorityComparison;
        
        // Secondary sort by GetHashCode for stable ordering
        return x.GetHashCode().CompareTo(y.GetHashCode());
    }
}