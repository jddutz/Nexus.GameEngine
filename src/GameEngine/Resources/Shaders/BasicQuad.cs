namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Predefined shader definitions
/// </summary>
public static partial class ShaderDefinitions
{
    /// <summary>
    /// Basic quad shader for rendering a simple colored quad
    /// </summary>
    public static readonly ShaderDefinition BasicQuad = new()
    {
        Name = "BasicQuad",
        VertexShader = ShaderSource.FromEmbeddedResource("basic-quad-vert.glsl"),
        FragmentShader = ShaderSource.FromEmbeddedResource("basic-quad-frag.glsl")
    };
}