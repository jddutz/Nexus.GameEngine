using Nexus.GameEngine.Resources.Sources;

namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Source for loading geometry data.
/// Implementations handle different mesh formats and generation mechanisms.
/// </summary>
public interface IGeometrySource : IResourceSource<GeometrySourceData>
{
}
