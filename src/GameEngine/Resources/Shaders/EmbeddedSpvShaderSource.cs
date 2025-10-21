using System.Reflection;

namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Loads pre-compiled SPIR-V shaders from embedded resources.
/// </summary>
public class EmbeddedSpvShaderSource : IShaderSource
{
    private readonly string _vertexShaderPath;
    private readonly string _fragmentShaderPath;
    private readonly Assembly _sourceAssembly;
    
    /// <summary>
    /// Creates a source for loading compiled SPIR-V shaders from embedded resources.
    /// </summary>
    /// <param name="vertexShaderPath">Path to compiled vertex shader SPIR-V file (e.g., "Shaders/vert.spv")</param>
    /// <param name="fragmentShaderPath">Path to compiled fragment shader SPIR-V file (e.g., "Shaders/frag.spv")</param>
    /// <param name="sourceAssembly">Assembly containing the embedded shader resources</param>
    public EmbeddedSpvShaderSource(string vertexShaderPath, string fragmentShaderPath, Assembly sourceAssembly)
    {
        _vertexShaderPath = vertexShaderPath ?? throw new ArgumentNullException(nameof(vertexShaderPath));
        _fragmentShaderPath = fragmentShaderPath ?? throw new ArgumentNullException(nameof(fragmentShaderPath));
        _sourceAssembly = sourceAssembly ?? throw new ArgumentNullException(nameof(sourceAssembly));
    }
    
    /// <summary>
    /// Loads the compiled SPIR-V shader code from embedded resources.
    /// </summary>
    public ShaderSourceData Load()
    {
        var vertexCode = LoadShaderFromEmbeddedResource(_vertexShaderPath, _sourceAssembly);
        var fragmentCode = LoadShaderFromEmbeddedResource(_fragmentShaderPath, _sourceAssembly);
        
        return new ShaderSourceData
        {
            VertexSpirV = vertexCode,
            FragmentSpirV = fragmentCode,
            ShaderName = Path.GetFileNameWithoutExtension(_vertexShaderPath)
        };
    }
    
    private byte[] LoadShaderFromEmbeddedResource(string path, Assembly assembly)
    {
        // Convert path to embedded resource name
        // Example: "Shaders/vert.spv" -> "AssemblyName.Shaders.vert.spv"
        var assemblyName = assembly.GetName().Name;
        var resourceName = $"{assemblyName}.{path.Replace('/', '.')}";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        
        if (stream == null)
        {
            throw new FileNotFoundException(
                $"Shader resource not found: {resourceName} in assembly {assemblyName} (from path: {path})");
        }
        
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
