using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Push constants for transformed uniform color rendering.
/// Contains a view/projection matrix and a color.
/// Used for UI components that work in pixel space and need transformation to NDC.
/// Size: 80 bytes (1 × mat4 (64 bytes) + 1 × vec4 (16 bytes)).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct TransformedColorPushConstants
{
    /// <summary>
    /// The combined view/projection matrix to transform vertices from pixel space to NDC.
    /// </summary>
    public Matrix4X4<float> ViewProjectionMatrix;

    /// <summary>
    /// The uniform color to apply to all vertices.
    /// </summary>
    public Vector4D<float> Color;

    /// <summary>
    /// Creates push constants with the specified matrix and color.
    /// </summary>
    public static TransformedColorPushConstants FromMatrixAndColor(
        Matrix4X4<float> viewProjectionMatrix,
        Vector4D<float> color) => new()
    {
        ViewProjectionMatrix = viewProjectionMatrix,
        Color = color
    };
}
