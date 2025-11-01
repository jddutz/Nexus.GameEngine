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
    /// Defaults to "DefaultTexture".
    /// </summary>
    public string Name { get; init; } = "DefaultTexture";
    
    /// <summary>
    /// Source that loads the raw texture pixel data.
    /// Defaults to a 1x1 white pixel.
    /// </summary>
    public ITextureSource Source { get; init; } = new ArgbArrayTextureSource(1, 1, [new Vector4D<float>(1, 1, 1, 1)]);
}
