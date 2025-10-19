using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;

namespace Nexus.GameEngine.Resources.Shaders.Definitions;

/// <summary>
/// Shader definition for uniform color rendering using push constants.
/// Vertex format: Position (vec2) = 8 bytes per vertex
/// Color: Provided via push constants (single color for all vertices)
/// </summary>
public class UniformColorQuadShader : IShaderDefinition
{
    /// <inheritdoc/>
    public string Name => "UniformColorQuadShader";
    
    /// <inheritdoc/>
    public string VertexShaderPath => "Shaders/uniform_color.vert.spv";
    
    /// <inheritdoc/>
    public string FragmentShaderPath => "Shaders/uniform_color.frag.spv";
    
    /// <inheritdoc/>
    public VertexInputDescription InputDescription => UniformColorQuad.GetVertexInputDescription();
    
    /// <inheritdoc/>
    public PushConstantRange[]? PushConstantRanges =>
    [
        new PushConstantRange
        {
            StageFlags = ShaderStageFlags.VertexBit,
            Offset = 0,
            Size = (uint)Unsafe.SizeOf<UniformColorPushConstants>() // 16 bytes
        }
    ];
    
    /// <inheritdoc/>
    public DescriptorSetLayoutBinding[]? DescriptorSetLayoutBindings => null;  // No descriptor sets needed
    
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

        if (geometry.VertexCount != 4)
        {
            throw new InvalidOperationException(
                $"Geometry '{geometry.Name}' must have exactly 4 vertices (quad), " +
                $"but has {geometry.VertexCount}");
        }
    }
}
