using Microsoft.Extensions.DependencyInjection;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Shaders;
using Nexus.GameEngine.Resources.Textures;
using Nexus.GameEngine.Resources.Fonts;

namespace Nexus.GameEngine.Resources;

/// <summary>
/// Central resource management system implementation.
/// Coordinates specialized resource managers and handles overall lifecycle.
/// </summary>
public class ResourceManager : IResourceManager
{
    private readonly IServiceProvider _provider;
    
    public ResourceManager(IServiceProvider provider)
    {
        _provider = provider;
    }
    
    /// <inheritdoc />
    public IGeometryResourceManager Geometry => _provider.GetRequiredService<IGeometryResourceManager>();
    
    /// <inheritdoc />
    public IShaderResourceManager Shaders => _provider.GetRequiredService<IShaderResourceManager>();
    
    /// <inheritdoc />
    public ITextureResourceManager Textures => _provider.GetRequiredService<ITextureResourceManager>();
    
    /// <inheritdoc />
    public IFontResourceManager Fonts => _provider.GetRequiredService<IFontResourceManager>();
    
    /// <inheritdoc />
    public void Dispose()
    {
        // Resources are managed by DI container
    }
}
