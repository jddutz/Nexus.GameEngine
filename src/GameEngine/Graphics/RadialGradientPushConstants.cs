using Silk.NET.Maths;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Push constants for radial gradient rendering.
/// Updated every frame for animation (center movement, radius changes).
/// Size: 16 bytes (within push constant budget).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RadialGradientPushConstants
{
    /// <summary>
    /// Center point of the radial gradient in normalized device coordinates [-1, 1].
    /// (0, 0) = screen center, (-1, -1) = top-left, (1, 1) = bottom-right.
    /// </summary>
    public Vector2D<float> Center;
    
    /// <summary>
    /// Radius of the gradient in normalized device coordinates.
    /// 1.0 = from center to edge of screen (default).
    /// </summary>
    public float Radius;
    
    /// <summary>
    /// Padding to ensure 16-byte alignment for Vulkan.
    /// </summary>
    private float _padding;
    
    /// <summary>
    /// Creates push constants for a centered radial gradient.
    /// </summary>
    public static RadialGradientPushConstants Centered(float radius = 1.0f) => new() 
    { 
        Center = Vector2D<float>.Zero,
        Radius = radius 
    };
}
