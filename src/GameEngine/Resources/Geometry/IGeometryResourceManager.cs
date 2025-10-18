namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Manages geometry resource lifecycle: creation, caching, reference counting, and disposal.
/// </summary>
public interface IGeometryResourceManager : IDisposable
{
    /// <summary>
    /// Gets or creates a geometry resource from a definition.
    /// Resources are cached - multiple calls with the same definition return the same resource.
    /// Increments reference count.
    /// </summary>
    /// <param name="definition">Geometry definition containing vertex data</param>
    /// <returns>Geometry resource handle</returns>
    GeometryResource GetOrCreate(IGeometryDefinition definition);
    
    /// <summary>
    /// Releases a geometry resource, decrementing its reference count.
    /// If reference count reaches zero and not flagged as persistent, the resource is destroyed.
    /// </summary>
    /// <param name="definition">Geometry definition to release</param>
    void Release(IGeometryDefinition definition);
}
