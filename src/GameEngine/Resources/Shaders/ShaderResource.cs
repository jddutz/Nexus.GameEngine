using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Resources.Shaders;

/// <summary>
/// Handle to GPU shader resources (shader modules).
/// Managed by ShaderResourceManager - components should not create or destroy these directly.
/// </summary>
public class ShaderResource
{
    /// <summary>
    /// Vulkan vertex shader module.
    /// </summary>
    public ShaderModule VertexShader { get; }
    
    /// <summary>
    /// Vulkan fragment shader module.
    /// </summary>
    public ShaderModule FragmentShader { get; }
    
    /// <summary>
    /// Shader definition containing input description and validation.
    /// </summary>
    public IShaderDefinition Definition { get; }
    
    /// <summary>
    /// Name of this shader resource.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// Internal constructor - only ShaderResourceManager should create these.
    /// </summary>
    internal ShaderResource(ShaderModule vertexShader, ShaderModule fragmentShader, IShaderDefinition definition)
    {
        VertexShader = vertexShader;
        FragmentShader = fragmentShader;
        Definition = definition;
        Name = definition.Name;
    }
}
