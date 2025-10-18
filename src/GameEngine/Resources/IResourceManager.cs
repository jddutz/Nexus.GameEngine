using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Shaders;

namespace Nexus.GameEngine.Resources;

/// <summary>
/// Central resource management system with specialized sub-managers.
/// Provides access to geometry, shader, texture, and other game resources.
/// </summary>
public interface IResourceManager : IDisposable
{
    /// <summary>
    /// Geometry resource manager for vertex buffers and mesh data.
    /// </summary>
    IGeometryResourceManager Geometry { get; }
    
    /// <summary>
    /// Shader resource manager for compiled shader programs.
    /// </summary>
    IShaderResourceManager Shaders { get; }
    
    // Future: ITextureResourceManager Textures { get; }
    // Future: IAudioResourceManager Audio { get; }
}
