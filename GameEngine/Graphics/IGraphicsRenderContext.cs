using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Rendering context interface that provides drawing capabilities for components.
/// This abstracts the underlying graphics API (OpenGL, DirectX, Vulkan, etc.).
/// </summary>
public interface IGraphicsRenderContext
{
    /// <summary>
    /// Draw a texture/sprite at the specified position.
    /// </summary>
    /// <param name="texturePath">Path to the texture to draw</param>
    /// <param name="position">Position to draw at</param>
    /// <param name="size">Size to draw the texture</param>
    /// <param name="tint">Vector4D<float> tint to apply</param>
    /// <param name="rotation">Rotation in radians</param>
    /// <param name="origin">Origin point for rotation (0,0 to 1,1)</param>
    void DrawTexture(string texturePath, Vector2D<float> position, Vector2D<float> size, Vector4D<float> tint, float rotation = 0f, Vector2D<float> origin = default);

    /// <summary>
    /// Draw a portion of a texture (sprite sheet).
    /// </summary>
    /// <param name="texturePath">Path to the texture to draw</param>
    /// <param name="sourceRect">Source rectangle within the texture</param>
    /// <param name="position">Position to draw at</param>
    /// <param name="size">Size to draw the texture</param>
    /// <param name="tint">Vector4D<float> tint to apply</param>
    void DrawTexture(string texturePath, Rectangle<float> sourceRect, Vector2D<float> position, Vector2D<float> size, Vector4D<float> tint);

    /// <summary>
    /// Draw a filled rectangle.
    /// </summary>
    /// <param name="position">Position of the rectangle</param>
    /// <param name="size">Size of the rectangle</param>
    /// <param name="color">Fill color</param>
    void DrawRectangle(Vector2D<float> position, Vector2D<float> size, Vector4D<float> color);

    /// <summary>
    /// Draw text using the specified font.
    /// </summary>
    /// <param name="text">Text to draw</param>
    /// <param name="position">Position to draw at</param>
    /// <param name="color">Text color</param>
    /// <param name="fontName">Name of the font to use</param>
    /// <param name="fontSize">Size of the font</param>
    void DrawText(string text, Vector2D<float> position, Vector4D<float> color, string fontName = "DefaultFont", float fontSize = 12f);

    /// <summary>
    /// Draw a 3D model with the specified world transformation.
    /// </summary>
    /// <param name="modelPath">Path to the 3D model</param>
    /// <param name="worldMatrix">World transformation matrix</param>
    /// <param name="texturePaths">Array of texture paths for materials</param>
    void DrawModel(string modelPath, Matrix4X4<float> worldMatrix, string[]? texturePaths = null);

    /// <summary>
    /// Push a transformation matrix onto the transform stack.
    /// All subsequent drawing operations will be transformed by this matrix.
    /// </summary>
    /// <param name="position">Translation</param>
    /// <param name="rotation">Rotation in radians</param>
    /// <param name="scale">Scale factors</param>
    void PushTransform(Vector2D<float> position, float rotation = 0f, Vector2D<float> scale = default);

    /// <summary>
    /// Pop the most recent transformation from the transform stack.
    /// </summary>
    void PopTransform();

    /// <summary>
    /// Set the current viewport/clipping rectangle.
    /// </summary>
    /// <param name="viewport">The viewport rectangle</param>
    void SetViewport(Rectangle<float> viewport);

    /// <summary>
    /// Reset the viewport to the full render target.
    /// </summary>
    void ResetViewport();
}