namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Compiles GLSL shaders to SPIR-V at runtime.
/// Currently scaffolded - implementation pending.
/// </summary>
public class GlslCompilerShaderSource : IShaderSource
{
    private readonly string _vertexGlslPath;
    private readonly string _fragmentGlslPath;
    
    /// <summary>
    /// Creates a source for runtime GLSL compilation.
    /// </summary>
    /// <param name="vertexGlslPath">Path to vertex shader GLSL source</param>
    /// <param name="fragmentGlslPath">Path to fragment shader GLSL source</param>
    public GlslCompilerShaderSource(string vertexGlslPath, string fragmentGlslPath)
    {
        _vertexGlslPath = vertexGlslPath ?? throw new ArgumentNullException(nameof(vertexGlslPath));
        _fragmentGlslPath = fragmentGlslPath ?? throw new ArgumentNullException(nameof(fragmentGlslPath));
    }
    
    /// <summary>
    /// Compiles GLSL to SPIR-V at runtime.
    /// </summary>
    /// <exception cref="NotImplementedException">Runtime GLSL compilation not yet implemented</exception>
    public ShaderSourceData Load()
    {
        // Options for runtime compilation:
        // 1. Shell out to glslc.exe (VulkanSDK required)
        // 2. Use shaderc library (C# bindings)
        // 3. Use SPIRV-Cross or similar
        
        throw new NotImplementedException(
            "Runtime GLSL compilation not yet implemented. " +
            "Pre-compile shaders to .spv using compile.bat");
    }
}
