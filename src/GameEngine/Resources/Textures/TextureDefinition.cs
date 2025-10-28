namespace Nexus.GameEngine.Resources.Textures;

/// <summary>
/// Defines a texture resource for GPU rendering.
/// This is a recipe that tells TextureResourceManager how to create the texture resource.
/// </summary>
public record TextureDefinition
{
    /// <summary>
    /// Unique name for this texture resource.
    /// Used for caching and identification.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Source that loads the raw texture pixel data.
    /// </summary>
    public required ITextureSource Source { get; init; }
}
