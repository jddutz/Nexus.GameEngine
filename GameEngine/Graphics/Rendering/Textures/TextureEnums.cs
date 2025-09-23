namespace Nexus.GameEngine.Graphics.Rendering.Textures;

/// <summary>
/// Texture filtering options
/// </summary>
public enum TextureFilter
{
    Nearest,
    Linear,
    NearestMipmapNearest,
    LinearMipmapNearest,
    NearestMipmapLinear,
    LinearMipmapLinear
}

/// <summary>
/// Texture wrapping modes
/// </summary>
public enum TextureWrap
{
    Repeat,
    ClampToEdge,
    ClampToBorder,
    MirroredRepeat
}

/// <summary>
/// Texture usage hints for optimization
/// </summary>
public enum TextureUsage
{
    Static,     // Texture data rarely changes
    Dynamic,    // Texture data changes occasionally  
    Stream      // Texture data changes frequently
}