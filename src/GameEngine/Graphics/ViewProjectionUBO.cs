using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Uniform Buffer Object for view/projection matrix.
/// Bound once per viewport to avoid pushing the 64-byte matrix with every draw command.
/// Size: 64 bytes (1 × mat4).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ViewProjectionUBO
{
    /// <summary>
    /// The combined view/projection matrix to transform vertices from pixel space to NDC.
    /// </summary>
    public Matrix4X4<float> ViewProjectionMatrix;

    /// <summary>
    /// Creates a UBO with the specified view-projection matrix.
    /// </summary>
    public static ViewProjectionUBO FromMatrix(Matrix4X4<float> viewProjectionMatrix) => new()
    {
        ViewProjectionMatrix = viewProjectionMatrix
    };
}
