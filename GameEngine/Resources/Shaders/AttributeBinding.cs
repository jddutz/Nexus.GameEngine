namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Defines an attribute binding for a shader
/// </summary>
public record AttributeBinding
{
    /// <summary>
    /// Name of the attribute in the shader
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Location/index for this attribute
    /// </summary>
    public required uint Location { get; init; }
}