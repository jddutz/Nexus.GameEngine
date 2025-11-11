using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics.PushConstants;

/// <summary>
/// Push constants for per-vertex colors.
/// Contains 4 colors (one for each vertex of a quad).
/// Size: 64 bytes (4 × vec4 × 4 bytes per float).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VertexColorsPushConstants
{
    /// <summary>
    /// Color for vertex 0 (Top-left)
    /// </summary>
    public Vector4D<float> Color0;
    
    /// <summary>
    /// Color for vertex 1 (Bottom-left)
    /// </summary>
    public Vector4D<float> Color1;
    
    /// <summary>
    /// Color for vertex 2 (Top-right)
    /// </summary>
    public Vector4D<float> Color2;
    
    /// <summary>
    /// Color for vertex 3 (Bottom-right)
    /// </summary>
    public Vector4D<float> Color3;

    /// <summary>
    /// Creates push constants with the same color for all vertices.
    /// </summary>
    public static VertexColorsPushConstants Solid(Vector4D<float> color) => new()
    {
        Color0 = color,
        Color1 = color,
        Color2 = color,
        Color3 = color
    };

    /// <summary>
    /// Creates push constants with different colors for each vertex.
    /// </summary>
    public static VertexColorsPushConstants FromColors(
        Vector4D<float> topLeft,
        Vector4D<float> bottomLeft,
        Vector4D<float> topRight,
        Vector4D<float> bottomRight) => new()
    {
        Color0 = topLeft,
        Color1 = bottomLeft,
        Color2 = topRight,
        Color3 = bottomRight
    };
}
