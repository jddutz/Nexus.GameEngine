using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Push constants for image texture rendering with UV bounds.
/// Used to control which portion of the texture is visible (for Fill modes).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ImageTexturePushConstants
{
    /// <summary>
    /// Minimum UV coordinates (top-left corner of visible region).
    /// </summary>
    public Vector2D<float> UvMin;
    
    /// <summary>
    /// Maximum UV coordinates (bottom-right corner of visible region).
    /// </summary>
    public Vector2D<float> UvMax;
    
    /// <summary>
    /// Tint color to multiply the texture by (RGBA).
    /// Use (1, 1, 1, 1) for no tint (white = original texture colors).
    /// </summary>
    public Vector4D<float> TintColor;
    
    /// <summary>
    /// Creates push constants from calculated UV bounds.
    /// </summary>
    /// <param name="uvMin">Minimum UV coordinates</param>
    /// <param name="uvMax">Maximum UV coordinates</param>
    /// <returns>Push constants structure</returns>
    public static ImageTexturePushConstants FromUVBounds(Vector2D<float> uvMin, Vector2D<float> uvMax)
    {
        return new ImageTexturePushConstants
        {
            UvMin = uvMin,
            UvMax = uvMax,
            TintColor = new Vector4D<float>(1, 1, 1, 1)  // Default to white (no tint)
        };
    }
}
