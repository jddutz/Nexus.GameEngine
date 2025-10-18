using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;

namespace Nexus.GameEngine.Resources.Shaders.Definitions;

/// <summary>
/// Shader definition for colored geometry using push constants.
/// Vertex format: Position (vec2) only
/// Colors: Provided via push constants (4 x vec4 for quad vertices)
/// </summary>
public class ColoredGeometryShader : IShaderDefinition
{
    /// <inheritdoc/>
    public string Name => "ColoredGeometryShader";
    
    /// <inheritdoc/>
    public string VertexShaderPath => "Shaders/shader.vert.spv";
    
    /// <inheritdoc/>
    public string FragmentShaderPath => "Shaders/shader.frag.spv";
    
    /// <inheritdoc/>
    public VertexInputDescription InputDescription => ColorQuad.GetVertexInputDescription();
    
    /// <inheritdoc/>
    public PushConstantRange[]? PushConstantRanges =>
    [
        new PushConstantRange
        {
            StageFlags = ShaderStageFlags.VertexBit,
            Offset = 0,
            Size = (uint)Unsafe.SizeOf<VertexColorsPushConstants>() // 64 bytes (4 x vec4)
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
