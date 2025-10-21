namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Manages shader resource lifecycle: creation, caching, reference counting, and disposal.
/// </summary>
public interface IShaderResourceManager : IDisposable
{
    /// <summary>
    /// Gets or creates a shader resource from a definition.
    /// Resources are cached - multiple calls with the same definition return the same resource.
    /// Increments reference count.
    /// </summary>
    /// <param name="definition">Shader definition containing paths and input description</param>
    /// <returns>Shader resource handle</returns>
    ShaderResource GetOrCreate(ShaderDefinition definition);
    
    /// <summary>
    /// Releases a shader resource, decrementing its reference count.
    /// If reference count reaches zero and not flagged as persistent, the resource is destroyed.
    /// </summary>
    /// <param name="definition">Shader definition to release</param>
    void Release(ShaderDefinition definition);
}
