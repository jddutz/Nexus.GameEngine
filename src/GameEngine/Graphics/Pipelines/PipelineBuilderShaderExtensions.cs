using Nexus.GameEngine.Resources.Shaders;

namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Extension methods for adding shader configuration to pipeline builders.
/// </summary>
public static class PipelineBuilderShaderExtensions
{
    /// <summary>
    /// Sets the shader resource for this pipeline.
    /// The shader provides the vertex input description and shader modules.
    /// </summary>
    /// <param name="builder">The pipeline builder instance.</param>
    /// <param name="shader">The shader resource to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if shader is null.</exception>
    public static IPipelineBuilder WithShader(this IPipelineBuilder builder, ShaderResource shader)
    {
        if (builder is PipelineBuilder impl)
        {
            impl.Shader = shader ?? throw new ArgumentNullException(nameof(shader));
        }
        return builder;
    }
    
    /// <summary>
    /// Sets the shader definition for this pipeline.
    /// The shader will be loaded from the resource manager when Build() is called.
    /// </summary>
    /// <param name="builder">The pipeline builder instance.</param>
    /// <param name="shaderDefinition">The shader definition to use.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if shader definition is null.</exception>
    public static IPipelineBuilder WithShader(this IPipelineBuilder builder, IShaderDefinition shaderDefinition)
    {
        if (builder is PipelineBuilder impl)
        {
            impl.ShaderDefinition = shaderDefinition ?? throw new ArgumentNullException(nameof(shaderDefinition));
        }
        return builder;
    }
}
