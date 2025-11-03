namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Comparer for sorting items by render priority. Lower priority values come first.
/// If priorities are equal, maintains insertion order by comparing hash codes.
/// </summary>
public class RenderPriorityComparer<T> : IComparer<T> where T : IRenderPriority
{
    public int Compare(T? x, T? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        // First compare by RenderPriority
        int priorityComparison = x.RenderPriority.CompareTo(y.RenderPriority);
        if (priorityComparison != 0)
            return priorityComparison;

        // If priorities are equal, use GetHashCode for stable ordering
        // This ensures items with same priority maintain insertion order
        return x.GetHashCode().CompareTo(y.GetHashCode());
    }
}
