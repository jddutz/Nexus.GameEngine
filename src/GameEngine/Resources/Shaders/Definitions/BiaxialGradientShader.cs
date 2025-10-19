using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Resources.Shaders.Definitions;

/// <summary>
/// Shader definition for biaxial (4-corner) gradient backgrounds.
/// Vertex format: Position (vec2) only
/// Colors: Uniform Buffer Object (UBO) at binding 0 with 4 corner colors
/// Bilinear interpolation is performed in the fragment shader.
/// </summary>
public class BiaxialGradientShader : IShaderDefinition
{
    /// <inheritdoc/>
    public string Name => "BiaxialGradientShader";
    
    /// <inheritdoc/>
    public string VertexShaderPath => "Shaders/biaxial_gradient.vert.spv";
    
    /// <inheritdoc/>
    public string FragmentShaderPath => "Shaders/biaxial_gradient.frag.spv";
    
    /// <inheritdoc/>
    public VertexInputDescription InputDescription => UniformColorQuad.GetVertexInputDescription();
    
    /// <inheritdoc/>
    public PushConstantRange[]? PushConstantRanges => null;  // No push constants needed
    
    /// <inheritdoc/>
    public DescriptorSetLayoutBinding[]? DescriptorSetLayoutBindings =>
    [
        new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.FragmentBit,
            PImmutableSamplers = null
        }
    ];
    
    /// <inheritdoc/>
    public void ValidateGeometry(GeometryResource geometry)
    {
        var expectedStride = InputDescription.Bindings[0].Stride;
        if (geometry.Stride != expectedStride)
        {
            throw new InvalidOperationException(
                $"Geometry '{geometry.Name}' stride ({geometry.Stride} bytes) " +
                $"doesn't match shader '{Name}' expected stride ({expectedStride} bytes)");
        }
    }
}
