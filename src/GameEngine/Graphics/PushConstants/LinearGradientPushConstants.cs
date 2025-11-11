using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics.PushConstants;

/// <summary>
/// Push constants for linear gradient rendering.
/// Updated every frame for animation (rotation, movement).
/// Size: 16 bytes (within push constant budget).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct LinearGradientPushConstants
{
    /// <summary>
    /// Gradient angle in radians (0 = horizontal left-to-right, π/2 = vertical bottom-to-top).
    /// </summary>
    public float Angle;
    
    /// <summary>
    /// Padding to ensure 16-byte alignment for Vulkan.
    /// </summary>
    private float _padding1;
    private float _padding2;
    private float _padding3;
    
    /// <summary>
    /// Creates push constants for a horizontal gradient (left to right).
    /// </summary>
    public static LinearGradientPushConstants Horizontal() => new() { Angle = 0.0f };
    
    /// <summary>
    /// Creates push constants for a vertical gradient (bottom to top).
    /// </summary>
    public static LinearGradientPushConstants Vertical() => new() { Angle = MathF.PI / 2.0f };
    
    /// <summary>
    /// Creates push constants for a diagonal gradient.
    /// </summary>
    public static LinearGradientPushConstants Diagonal() => new() { Angle = MathF.PI / 4.0f };
}
