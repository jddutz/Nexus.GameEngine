using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Push constants for radial gradient rendering.
/// Updated every frame for animation (center movement, radius changes, scaling).
/// Size: 32 bytes (within push constant budget).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RadialGradientPushConstants
{
    /// <summary>
    /// Center point of the radial gradient in normalized [0,1] coordinates.
    /// (0.5, 0.5) = screen center, (0, 0) = top-left, (1, 1) = bottom-right.
    /// Values outside [0,1] place center offscreen for partial gradient effects.
    /// </summary>
    public Vector2D<float> Center;
    
    /// <summary>
    /// Radius of the gradient in normalized [0,1] coordinates.
    /// 0.5 = from center to edge (when centered).
    /// </summary>
    public float Radius;
    
    private float _padding1;
    
    /// <summary>
    /// Scale factors for elliptical gradients.
    /// (1, 1) = circular (default)
    /// (aspectRatio, 1) = circular on non-square viewports
    /// (2, 1) = ellipse stretched horizontally
    /// </summary>
    public Vector2D<float> Scale;
    
    private Vector2D<float> _padding2;
    
    /// <summary>
    /// Creates push constants for a centered circular radial gradient with aspect correction.
    /// </summary>
    public static RadialGradientPushConstants Centered(float radius, float aspectRatio) => new() 
    { 
        Center = new Vector2D<float>(0.5f, 0.5f),
        Radius = radius,
        Scale = new Vector2D<float>(aspectRatio, 1.0f)
    };
}
