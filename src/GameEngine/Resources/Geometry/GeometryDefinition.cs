namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Defines a geometry resource for creation and caching.
/// Used as a key in GeometryResourceManager's cache.
/// </summary>
public record GeometryDefinition
{
    /// <summary>
    /// Unique name identifying this geometry definition.
    /// Used for cache key generation and logging.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// The source that knows how to generate or load this geometry's vertex data.
    /// </summary>
    public required IGeometrySource Source { get; init; }
}
