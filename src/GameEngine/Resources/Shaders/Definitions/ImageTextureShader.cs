using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;

namespace Nexus.GameEngine.Resources.Shaders.Definitions;

/// <summary>
/// Shader definition for rendering textured backgrounds with image sampling.
/// Supports UV bounds via push constants for Fill placement modes.
/// </summary>
public class ImageTextureShader : IShaderDefinition
{
    /// <inheritdoc />
    public string Name => "ImageTextureShader";
    
    /// <inheritdoc />
    public string VertexShaderPath => "Shaders/image_texture.vert.spv";
    
    /// <inheritdoc />
    public string FragmentShaderPath => "Shaders/image_texture.frag.spv";
    
    /// <inheritdoc />
    public VertexInputDescription InputDescription => TexturedQuad.GetVertexInputDescription();
    
    /// <inheritdoc />
    public PushConstantRange[]? PushConstantRanges =>
    [
        new PushConstantRange
        {
            StageFlags = ShaderStageFlags.VertexBit,
            Offset = 0,
            Size = (uint)Unsafe.SizeOf<Graphics.ImageTexturePushConstants>()  // 16 bytes (2 * vec2)
        }
    ];
    
    /// <inheritdoc />
    public DescriptorSetLayoutBinding[]? DescriptorSetLayoutBindings =>
    [
        new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.FragmentBit,
            PImmutableSamplers = null
        }
    ];
    
    /// <inheritdoc />
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
