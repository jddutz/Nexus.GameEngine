using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;

namespace Nexus.GameEngine.Resources.Shaders.Definitions;

/// <summary>
/// Shader definition for colored geometry with per-vertex colors.
/// Vertex format: Position (vec2) + Color (vec4) = 24 bytes per vertex
/// Colors: Provided as vertex attributes
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
    public PushConstantRange[]? PushConstantRanges => null;  // No push constants needed
    
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
    }
}
