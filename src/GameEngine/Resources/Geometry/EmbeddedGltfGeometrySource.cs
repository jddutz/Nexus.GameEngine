using System.Reflection;

namespace Nexus.GameEngine.Resources.Geometry;

/// <summary>
/// Loads glTF mesh files from embedded resources.
/// Currently scaffolded - implementation pending.
/// </summary>
public class EmbeddedGltfGeometrySource : IGeometrySource
{
    private readonly string _gltfPath;
    private readonly Assembly _sourceAssembly;
    
    /// <summary>
    /// Creates a source for loading glTF meshes from embedded resources.
    /// </summary>
    /// <param name="gltfPath">Path to the glTF file within embedded resources (.gltf or .glb)</param>
    /// <param name="sourceAssembly">Assembly containing the embedded mesh resource</param>
    /// <exception cref="ArgumentException">Thrown if path doesn't end with .gltf or .glb</exception>
    public EmbeddedGltfGeometrySource(string gltfPath, Assembly sourceAssembly)
    {
        if (!gltfPath.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase) &&
            !gltfPath.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Mesh path must end with .gltf or .glb", nameof(gltfPath));
        
        _gltfPath = gltfPath;
        _sourceAssembly = sourceAssembly;
    }
    
    /// <summary>
    /// Loads the geometry data from embedded resources.
    /// </summary>
    /// <exception cref="NotImplementedException">glTF mesh loading not yet implemented</exception>
    public GeometrySourceData Load()
    {
        throw new NotImplementedException(
            "glTF mesh loading not yet implemented. " +
            "Requires SharpGLTF or similar library.");
        
        // Future implementation:
        // 1. Load .gltf/.glb file from embedded resources
        // 2. Use SharpGLTF to parse
        // 3. Extract mesh primitives
        // 4. Convert to engine vertex format
        // 5. Return GeometrySourceData
    }
}
