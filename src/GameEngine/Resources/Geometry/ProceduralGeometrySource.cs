namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Creates geometry from a procedural generator function.
/// Useful for runtime-generated meshes and parametric shapes.
/// </summary>
public class ProceduralGeometrySource : IGeometrySource
{
    private readonly Func<GeometrySourceData> _generator;
    
    /// <summary>
    /// Creates a geometry source from a generator function.
    /// </summary>
    /// <param name="generator">Function that generates geometry data</param>
    public ProceduralGeometrySource(Func<GeometrySourceData> generator)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
    }
    
    /// <summary>
    /// Loads the geometry data by invoking the generator function.
    /// </summary>
    public GeometrySourceData Load()
    {
        return _generator();
    }
}
