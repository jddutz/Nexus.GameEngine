namespace Nexus.GameEngine.Graphics.Resources;

/// <summary>
/// Defines shader resource data including vertex and fragment shader source code.
/// </summary>
public record ShaderDefinition : IResourceDefinition
{
    /// <summary>
    /// The name of the shader resource.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Whether this shader should be kept in memory and not purged during resource cleanup.
    /// </summary>
    public bool IsPersistent { get; init; } = true;

    /// <summary>
    /// The vertex shader source code in GLSL.
    /// </summary>
    public string VertexSource { get; init; } = string.Empty;

    /// <summary>
    /// The fragment shader source code in GLSL.
    /// </summary>
    public string FragmentSource { get; init; } = string.Empty;

    /// <summary>
    /// Optional geometry shader source code in GLSL.
    /// </summary>
    public string? GeometrySource { get; init; }

    /// <summary>
    /// Optional compute shader source code in GLSL.
    /// </summary>
    public string? ComputeSource { get; init; }

    /// <summary>
    /// Shader preprocessor defines to apply during compilation.
    /// </summary>
    public Dictionary<string, string> Defines { get; init; } = new();

    /// <summary>
    /// Include paths for shader includes (e.g., common shader functions).
    /// </summary>
    public string[] IncludePaths { get; init; } = [];
}