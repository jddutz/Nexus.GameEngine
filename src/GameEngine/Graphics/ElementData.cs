namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Defines rendering requirements for a single renderable element without direct OpenGL access.
/// Used by the renderer to determine what GL state changes are needed for batched rendering.
/// Components return collections of ElementData via IRenderable.GetElements().
/// </summary>
public class ElementData
{
    public required uint Vbo { get; init; }
    public required uint Ebo { get; init; }
    public required uint Vao { get; init; }
    public required uint Shader { get; init; }

    /// <summary>
    /// Priority rendering order. Used for layering render results on top of one another.
    /// </summary>
    public uint? Priority { get; set; }

    /// <summary>
    /// Required shader program ID.
    /// </summary>
    public uint? ShaderProgram { get; set; }

    /// <summary>
    /// Currently bound textures by texture unit slot.
    /// Index corresponds to GL_TEXTURE0 + index.
    /// </summary>
    public uint?[] BoundTextures { get; private set; } = new uint?[16]; // GL_MAX_TEXTURE_UNITS

    /// <summary>
    /// Currently bound vertex array object ID. Null if none bound.
    /// </summary>
    public uint? VertexArray { get; set; }

    /// <summary>
    /// Currently bound framebuffer ID. Null if default framebuffer.
    /// </summary>
    public uint? Framebuffer { get; set; }

    /// <summary>
    /// Currently active texture unit (0-15).
    /// </summary>
    public int ActiveTextureUnit { get; set; } = 0;

    /// <summary>
    /// Viewport that generated this render state. Used for viewport-aware batching and debugging.
    /// Optional - can be null for legacy components that don't use viewport context.
    /// </summary>
    public IViewport? SourceViewport { get; set; }

    /// <summary>
    /// Uniform values to set when this render state is applied.
    /// Key is the uniform name, value is the uniform data.
    /// </summary>
    public Dictionary<string, object> Uniforms { get; init; } = [];

    /// <summary>
    /// Checks if a texture is already bound to the specified slot.
    /// </summary>
    /// <param name="textureId">Texture ID to check</param>
    /// <param name="slot">Texture unit slot (0-15)</param>
    /// <returns>True if the texture is already bound to this slot</returns>
    public bool IsTextureBound(uint textureId, int slot = 0)
    {
        return slot >= 0 && slot < BoundTextures.Length && BoundTextures[slot] == textureId;
    }

    /// <summary>
    /// Updates the bound texture for the specified slot.
    /// </summary>
    /// <param name="textureId">Texture ID that was bound</param>
    /// <param name="slot">Texture unit slot (0-15)</param>
    public void SetBoundTexture(uint textureId, int slot = 0)
    {
        if (slot >= 0 && slot < BoundTextures.Length)
        {
            BoundTextures[slot] = textureId;
        }
    }

    /// <summary>
    /// Clears the bound texture for the specified slot.
    /// </summary>
    /// <param name="slot">Texture unit slot to clear</param>
    public void ClearBoundTexture(int slot = 0)
    {
        if (slot >= 0 && slot < BoundTextures.Length)
        {
            BoundTextures[slot] = null;
        }
    }

    /// <summary>
    /// Resets all tracked state to initial values.
    /// Call this when GL context is lost or at the start of rendering.
    /// </summary>
    public void Reset()
    {
        ShaderProgram = null;
        VertexArray = null;
        Framebuffer = null;
        ActiveTextureUnit = 0;

        for (int i = 0; i < BoundTextures.Length; i++)
        {
            BoundTextures[i] = null;
        }
    }
}
