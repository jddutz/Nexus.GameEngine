using Silk.NET.Maths;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Uniform Buffer Object structure for biaxial (4-corner) gradient backgrounds.
/// Must match GLSL layout (std140) in biaxial_gradient.frag.
/// 
/// Layout:
///   - vec4 topLeft = 16 bytes
///   - vec4 topRight = 16 bytes
///   - vec4 bottomLeft = 16 bytes
///   - vec4 bottomRight = 16 bytes
/// Total size: 64 bytes
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct CornerColorsUBO
{
    /// <summary>
    /// Color at top-left corner (RGBA)
    /// </summary>
    public Vector4D<float> TopLeft;
    
    /// <summary>
    /// Color at top-right corner (RGBA)
    /// </summary>
    public Vector4D<float> TopRight;
    
    /// <summary>
    /// Color at bottom-left corner (RGBA)
    /// </summary>
    public Vector4D<float> BottomLeft;
    
    /// <summary>
    /// Color at bottom-right corner (RGBA)
    /// </summary>
    public Vector4D<float> BottomRight;
    
    /// <summary>
    /// Gets the size of the UBO structure in bytes (64 bytes = 4 vec4s)
    /// </summary>
    public static uint SizeInBytes => (uint)Unsafe.SizeOf<CornerColorsUBO>();
    
    /// <summary>
    /// Creates a CornerColorsUBO from four corner colors
    /// </summary>
    public static CornerColorsUBO FromCorners(
        Vector4D<float> topLeft,
        Vector4D<float> topRight,
        Vector4D<float> bottomLeft,
        Vector4D<float> bottomRight)
    {
        return new CornerColorsUBO
        {
            TopLeft = topLeft,
            TopRight = topRight,
            BottomLeft = bottomLeft,
            BottomRight = bottomRight
        };
    }
    
    /// <summary>
    /// Gets the UBO data as a byte span for buffer upload
    /// </summary>
    public ReadOnlySpan<byte> AsBytes()
    {
        ReadOnlySpan<CornerColorsUBO> span = new ReadOnlySpan<CornerColorsUBO>(ref Unsafe.AsRef(in this));
        return MemoryMarshal.AsBytes(span);
    }
}
