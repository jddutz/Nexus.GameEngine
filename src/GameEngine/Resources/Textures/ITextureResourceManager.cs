namespace Nexus.GameEngine.Resources.Textures;

/// <summary>
/// Manages texture resource lifecycle with caching and reference counting.
/// </summary>
public interface ITextureResourceManager : IDisposable
{
    /// <summary>
    /// Gets or creates a texture resource from a definition.
    /// Resources are cached - multiple calls with the same definition return the same resource.
    /// Increments reference count.
    /// </summary>
    /// <param name="definition">Texture definition specifying the texture to load</param>
    /// <returns>Texture resource handle</returns>
    TextureResource GetOrCreate(ITextureDefinition definition);
    
    /// <summary>
    /// Releases a texture resource, decrementing reference count.
    /// If count reaches zero, the resource is destroyed.
    /// </summary>
    /// <param name="definition">Texture definition to release</param>
    void Release(ITextureDefinition definition);
}
