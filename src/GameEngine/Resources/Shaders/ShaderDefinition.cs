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
    /// Vertex shader source - can be from embedded resource, file, or inline string
    /// </summary>
    public required ShaderSource VertexShader { get; init; }

    /// <summary>
    /// Fragment shader source - can be from embedded resource, file, or inline string
    /// </summary>
    public required ShaderSource FragmentShader { get; init; }

    /// <summary>
    /// Uniform definitions expected by this shader
    /// </summary>
    public IReadOnlyList<UniformDefinition> Uniforms { get; init; } = [];

    /// <summary>
    /// Vertex attribute locations expected by this shader
    /// </summary>
    public IReadOnlyList<AttributeBinding> AttributeBindings { get; init; } = [];
}