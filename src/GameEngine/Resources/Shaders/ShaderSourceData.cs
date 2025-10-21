namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Raw shader data returned by shader sources.
/// Contains compiled SPIR-V bytecode for vertex and fragment shaders.
/// </summary>
public record ShaderSourceData
{
    /// <summary>
    /// Compiled SPIR-V bytecode for the vertex shader.
    /// </summary>
    public required byte[] VertexSpirV { get; init; }
    
    /// <summary>
    /// Compiled SPIR-V bytecode for the fragment shader.
    /// </summary>
    public required byte[] FragmentSpirV { get; init; }
    
    /// <summary>
    /// Shader name for identification.
    /// </summary>
    public required string ShaderName { get; init; }
}
