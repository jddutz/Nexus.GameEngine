using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Shaders;

namespace Nexus.GameEngine.Resources;

/// <summary>
/// Central resource management system implementation.
/// Coordinates specialized resource managers and handles overall lifecycle.
/// </summary>
public class ResourceManager : IResourceManager
{
    private readonly IGeometryResourceManager _geometry;
    private readonly IShaderResourceManager _shaders;
    
    public ResourceManager(
        IGeometryResourceManager geometry,
        IShaderResourceManager shaders)
    {
        _geometry = geometry;
        _shaders = shaders;
    }
    
    /// <inheritdoc />
    public IGeometryResourceManager Geometry => _geometry;
    
    /// <inheritdoc />
    public IShaderResourceManager Shaders => _shaders;
    
    /// <inheritdoc />
    public void Dispose()
    {
        _geometry?.Dispose();
        _shaders?.Dispose();
    }
}
