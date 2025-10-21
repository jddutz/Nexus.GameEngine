using Nexus.GameEngine.Resources.Fonts;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Shaders;
using Nexus.GameEngine.Resources.Textures;

namespace Nexus.GameEngine.Resources;

/// <summary>
/// Central resource management system implementation.
/// Coordinates specialized resource managers and handles overall lifecycle.
/// </summary>
public class ResourceManager : IResourceManager
{
    private readonly IGeometryResourceManager _geometry;
    private readonly IShaderResourceManager _shaders;
    private readonly ITextureResourceManager _textures;
    private readonly IFontResourceManager _fonts;
    
    public ResourceManager(
        IGeometryResourceManager geometry,
        IShaderResourceManager shaders,
        ITextureResourceManager textures,
        IFontResourceManager fonts)
    {
        _geometry = geometry;
        _shaders = shaders;
        _textures = textures;
        _fonts = fonts;
    }
    
    /// <inheritdoc />
    public IGeometryResourceManager Geometry => _geometry;
    
    /// <inheritdoc />
    public IShaderResourceManager Shaders => _shaders;
    
    /// <inheritdoc />
    public ITextureResourceManager Textures => _textures;
    
    /// <inheritdoc />
    public IFontResourceManager Fonts => _fonts;
    
    /// <inheritdoc />
    public void Dispose()
    {
        _geometry?.Dispose();
        _shaders?.Dispose();
        _textures?.Dispose();
        // Note: Fonts doesn't implement IDisposable yet
    }
}
