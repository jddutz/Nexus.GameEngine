namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Pure data definition for shader resources
/// </summary>
public record ShaderDefinition : IResourceDefinition
{
    /// <summary>
    /// Unique name for this shader resource
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Vertex shader source code (GLSL)
    /// </summary>
    public required string VertexSource { get; init; }

    /// <summary>
    /// Fragment shader source code (GLSL)
    /// </summary>
    public required string FragmentSource { get; init; }

    /// <summary>
    /// Optional geometry shader source code (GLSL)
    /// </summary>
    public string? GeometrySource { get; init; }

    /// <summary>
    /// Uniform definitions expected by this shader
    /// </summary>
    public IReadOnlyList<UniformDefinition> Uniforms { get; init; } = Array.Empty<UniformDefinition>();

    /// <summary>
    /// Vertex attribute locations expected by this shader
    /// </summary>
    public IReadOnlyList<AttributeBinding> AttributeBindings { get; init; } = Array.Empty<AttributeBinding>();
}