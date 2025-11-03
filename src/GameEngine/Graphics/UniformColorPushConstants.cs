using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Push constants for uniform color rendering with model transformation.
/// Contains model matrix and color.
/// Size: 80 bytes (64 bytes for mat4 + 16 bytes for vec4).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct UniformColorPushConstants
{
    /// <summary>
    /// The model transformation matrix (local to world space)
    /// </summary>
    public Matrix4X4<float> Model;

    /// <summary>
    /// The uniform color to apply to all vertices
    /// </summary>
    public Vector4D<float> Color;

    /// <summary>
    /// Creates push constants with the specified model matrix and color.
    /// </summary>
    public static UniformColorPushConstants FromModelAndColor(Matrix4X4<float> model, Vector4D<float> color) => new()
    {
        Model = model,
        Color = color
    };
}
