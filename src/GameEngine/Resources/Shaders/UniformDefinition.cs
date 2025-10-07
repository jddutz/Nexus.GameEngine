namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Defines a uniform variable expected by a shader
/// </summary>
public record UniformDefinition
{
    /// <summary>
    /// Name of the uniform in the shader
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Type of the uniform
    /// </summary>
    public required UniformType Type { get; init; }

    /// <summary>
    /// Whether this uniform is required (validation will fail if missing)
    /// </summary>
    public bool Required { get; init; } = true;

    /// <summary>
    /// Default value for optional uniforms
    /// </summary>
    public object? DefaultValue { get; init; }
}