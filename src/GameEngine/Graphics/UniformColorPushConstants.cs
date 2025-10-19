using Silk.NET.Maths;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Push constants for uniform color rendering.
/// Contains a single color applied to all vertices.
/// Size: 16 bytes (1 × vec4 × 4 bytes per float).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct UniformColorPushConstants
{
    /// <summary>
    /// The uniform color to apply to all vertices
    /// </summary>
    public Vector4D<float> Color;

    /// <summary>
    /// Creates push constants with the specified color.
    /// </summary>
    public static UniformColorPushConstants FromColor(Vector4D<float> color) => new()
    {
        Color = color
    };
}
