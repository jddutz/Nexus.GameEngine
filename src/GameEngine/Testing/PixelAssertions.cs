namespace Nexus.GameEngine.Testing;

/// <summary>
/// Helper methods for asserting pixel color values in tests.
/// </summary>
public static class PixelAssertions
{
    /// <summary>
    /// Checks if two colors are approximately equal within a tolerance.
    /// Useful for comparing rendered pixels against expected colors.
    /// </summary>
    /// <param name="actual">The actual sampled color</param>
    /// <param name="expected">The expected color</param>
    /// <param name="tolerance">Maximum difference per channel (0.0 to 1.0). Default is 0.01 (1%)</param>
    /// <returns>True if colors match within tolerance</returns>
    public static bool ColorsMatch(Vector4D<float>? actual, Vector4D<float> expected, float tolerance = 0.01f)
    {
        if (!actual.HasValue)
            return false;

        var a = actual.Value;
        return Math.Abs(a.X - expected.X) <= tolerance &&
               Math.Abs(a.Y - expected.Y) <= tolerance &&
               Math.Abs(a.Z - expected.Z) <= tolerance &&
               Math.Abs(a.W - expected.W) <= tolerance;
    }

    /// <summary>
    /// Gets a human-readable description of a color for test reporting.
    /// </summary>
    public static string DescribeColor(Vector4D<float>? color)
    {
        if (!color.HasValue)
            return "null";

        var c = color.Value;
        return $"RGBA({c.X:F3}, {c.Y:F3}, {c.Z:F3}, {c.W:F3})";
    }
}
