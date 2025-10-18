using Nexus.GameEngine.Graphics.Pipelines;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Resources.Geometry.Definitions;

namespace Nexus.GameEngine.Resources.Shaders.Definitions;

/// <summary>
/// Shader definition for colored geometry (position + color vertex format).
/// Used by HelloQuad test component.
/// </summary>
public class ColoredGeometryShader : IShaderDefinition
{
    /// <inheritdoc/>
    public string Name => "ColoredGeometryShader";
    
    /// <inheritdoc/>
    public string VertexShaderPath => "Shaders/vert.spv";
    
    /// <inheritdoc/>
    public string FragmentShaderPath => "Shaders/frag.spv";
    
    /// <inheritdoc/>
    public VertexInputDescription InputDescription => ColorQuad.GetVertexInputDescription();
    
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
