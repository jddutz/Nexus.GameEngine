using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Push constants for the UIElement uber-shader.
/// Contains model matrix, tint color, and UV rectangle for rendering textured UI elements.
/// Total size: 96 bytes (mat4 64 + vec4 16 + vec4 16)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct UIElementPushConstants
{
    /// <summary>
    /// Model transformation matrix (position, scale, rotation).
    /// </summary>
    public Matrix4X4<float> Model;

    /// <summary>
    /// Tint color applied to the texture.
    /// For solid colors: use desired color with WhiteDummy texture.
    /// For textured elements: typically white (no tint) or desired tint color.
    /// </summary>
    public Vector4D<float> TintColor;

    /// <summary>
    /// UV rectangle for texture atlas/sprite sheet support.
    /// (minU, minV, maxU, maxV) - default (0, 0, 1, 1) for full texture.
    /// Allows multiple sprites to share a single geometry buffer.
    /// </summary>
    public Vector4D<float> UvRect;

    /// <summary>
    /// Creates push constants from a model matrix and tint color.
    /// Uses default UV rect (0, 0, 1, 1) for full texture.
    /// </summary>
    /// <param name="model">Model transformation matrix</param>
    /// <param name="tintColor">Tint color to apply</param>
    /// <returns>UIElementPushConstants instance</returns>
    public static UIElementPushConstants FromModelAndColor(Matrix4X4<float> model, Vector4D<float> tintColor)
    {
        return new UIElementPushConstants
        {
            Model = model,
            TintColor = tintColor,
            UvRect = new Vector4D<float>(0f, 0f, 1f, 1f)
        };
    }

    /// <summary>
    /// Creates push constants with custom UV rectangle for sprite atlas support.
    /// </summary>
    /// <param name="model">Model transformation matrix</param>
    /// <param name="tintColor">Tint color to apply</param>
    /// <param name="uvMin">Minimum UV coordinates (top-left)</param>
    /// <param name="uvMax">Maximum UV coordinates (bottom-right)</param>
    /// <returns>UIElementPushConstants instance</returns>
    public static UIElementPushConstants FromModelColorAndUV(
        Matrix4X4<float> model, 
        Vector4D<float> tintColor, 
        Vector2D<float> uvMin, 
        Vector2D<float> uvMax)
    {
        return new UIElementPushConstants
        {
            Model = model,
            TintColor = tintColor,
            UvRect = new Vector4D<float>(uvMin.X, uvMin.Y, uvMax.X, uvMax.Y)
        };
    }
}