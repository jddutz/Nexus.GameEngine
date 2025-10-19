using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;

namespace Nexus.GameEngine.Resources.Shaders.Definitions;

/// <summary>
/// Shader definition for radial gradient backgrounds.
/// Vertex format: Position (vec2) only
/// Gradient definition: Uniform Buffer Object (UBO) at binding 0
/// Animation: Push constants (center, radius)
/// </summary>
public class RadialGradientShader : IShaderDefinition
{
    /// <inheritdoc/>
    public string Name => "RadialGradientShader";
    
    /// <inheritdoc/>
    public string VertexShaderPath => "Shaders/radial_gradient.vert.spv";
    
    /// <inheritdoc/>
    public string FragmentShaderPath => "Shaders/radial_gradient.frag.spv";
    
    /// <inheritdoc/>
    public VertexInputDescription InputDescription => UniformColorQuad.GetVertexInputDescription();
    
    /// <inheritdoc/>
    public PushConstantRange[]? PushConstantRanges =>
    [
        new PushConstantRange
        {
            StageFlags = ShaderStageFlags.FragmentBit,
            Offset = 0,
            Size = (uint)Unsafe.SizeOf<RadialGradientPushConstants>() // 16 bytes
        }
    ];
    
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
