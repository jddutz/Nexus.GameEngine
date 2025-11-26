using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Graphics.PushConstants;

/// <summary>
/// Push constants for the UIElement uber-shader.
/// Contains model matrix, element size, tint color, and UV rectangle for rendering textured UI elements.
/// Total size: 112 bytes (mat4 64 + vec2 size 8 + vec4 tintColor 16 + vec4 uvRect 16 + vec2 padding 8)
/// Layout MUST match the shader's push_constant layout exactly!
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct UIElementPushConstants
{
    /// <summary>
    /// Model transformation matrix (position, rotation, scale).
    /// Does NOT include element Size - that's handled separately.
    /// </summary>
    public Matrix4X4<float> Model;

    /// <summary>
    /// Tint color applied to the texture.
    /// For solid colors: use desired color with UniformColor texture (1x1 white pixel).
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
    /// Element size in pixels (width, height).
    /// Used to scale the normalized 2x2 quad geometry to pixel dimensions.
    /// Separate from Model matrix to avoid double-scaling in hierarchical transforms.
    /// </summary>
    public Vector2D<float> Size;

    /// <summary>
    /// <summary>
    /// Pivot point used to translate quad coordinates to pixel coordinates.
    /// (0,0) = Top-Left, (0.5,0.5) = Center, (1,1) = Bottom-Right.
    /// Stored as floats in push constants.
    /// </summary>
    public Vector2D<float> Pivot;

    /// <summary>
    /// Creates push constants with custom UV rectangle for sprite atlas support.
    /// </summary>
    /// <param name="model">Model transformation matrix</param>
    /// <param name="tintColor">Tint color to apply</param>
    /// <param name="uvMin">Minimum UV coordinates (top-left)</param>
    /// <param name="uvMax">Maximum UV coordinates (bottom-right)</param>
    /// <param name="size">Element size in pixels (width, height)</param>
    /// <param name="pivot">Pivot defining the element relative to its Position</param>
    /// <returns>UIElementPushConstants instance</returns>
    public UIElementPushConstants(
        Matrix4X4<float> model,
        Vector4D<float> tintColor,
        Vector2D<float> uvMin,
        Vector2D<float> uvMax,
        Vector2D<int> size,
        Vector2D<float> pivot)
    {
        Model = model;
        TintColor = tintColor;
        UvRect = new Vector4D<float>(uvMin.X, uvMin.Y, uvMax.X, uvMax.Y);
        Size = new Vector2D<float>(size.X, size.Y);
        Pivot = pivot;
    }

    /// <summary>
    /// Simple constructor when using the full texture (uvRect = 0,0,1,1) and default pivot (0,0).
    /// </summary>
    public UIElementPushConstants(
        Matrix4X4<float> model,
        Vector4D<float> tintColor,
        Vector2D<int> size)
    {
        Model = model;
        TintColor = tintColor;
        UvRect = new Vector4D<float>(0f, 0f, 1f, 1f);
        Size = new Vector2D<float>(size.X, size.Y);
        Pivot = new Vector2D<float>(0f, 0f);
    }

    /// <summary>
    /// Constructor allowing explicit pivot with full-texture UVs.
    /// </summary>
    public UIElementPushConstants(
        Matrix4X4<float> model,
        Vector4D<float> tintColor,
        Vector2D<int> size,
        Vector2D<float> pivot)
    {
        Model = model;
        TintColor = tintColor;
        UvRect = new Vector4D<float>(0f, 0f, 1f, 1f);
        Size = new Vector2D<float>(size.X, size.Y);
        Pivot = pivot;
    }

    /// <summary>
    /// Constructor allowing explicit UV rectangle but default pivot (0,0).
    /// </summary>
    public UIElementPushConstants(
        Matrix4X4<float> model,
        Vector4D<float> tintColor,
        Vector2D<float> uvMin,
        Vector2D<float> uvMax,
        Vector2D<int> size)
    {
        Model = model;
        TintColor = tintColor;
        UvRect = new Vector4D<float>(uvMin.X, uvMin.Y, uvMax.X, uvMax.Y);
        Size = new Vector2D<float>(size.X, size.Y);
        Pivot = new Vector2D<float>(0f, 0f);
    }
}