using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Resources.Geometry.Definitions;

/// <summary>
/// Colored quad geometry definition.
/// Vertex format: Position(Vec2) only - colors provided via push constants
/// Four vertices arranged as a triangle strip forming a full-screen quad.
/// Note: In Vulkan, Y=-1 is top, Y=1 is bottom (inverted from OpenGL)
/// </summary>
public record ColorQuad : IGeometryDefinition
{
    /// <inheritdoc />
    public string Name => "ColorQuad";

    // Four vertices forming a full-screen quad using triangle strip
    // Triangle strip order: TL -> BL -> TR -> BR
    private readonly Vector2D<float>[] _vertices =
    [
        new(-1f, -1f),  // Top-left
        new(-1f,  1f),  // Bottom-left
        new( 1f, -1f),  // Top-right
        new( 1f,  1f),  // Bottom-right
    ];

    /// <inheritdoc />
    public uint VertexCount => (uint)_vertices.Length;

    /// <inheritdoc />
    public uint Stride => (uint)Unsafe.SizeOf<Vector2D<float>>();

    /// <inheritdoc />
    public ReadOnlySpan<byte> GetVertexData()
    {
        return MemoryMarshal.AsBytes(_vertices.AsSpan());
    }

    /// <summary>
    /// Gets the vertex input description for this geometry.
    /// TODO: This should be moved to shader definition once shader resource system is fully implemented.
    /// </summary>
    public static Graphics.Pipelines.VertexInputDescription GetVertexInputDescription()
    {
        var binding = new VertexInputBindingDescription
        {
            Binding = 0,
            Stride = (uint)Unsafe.SizeOf<Vector2D<float>>(),
            InputRate = VertexInputRate.Vertex
        };

        var attributes = new VertexInputAttributeDescription[]
        {
            // Position (vec2) at location 0 - only vertex attribute now
            new()
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32Sfloat,
                Offset = 0
            }
        };

        return new Graphics.Pipelines.VertexInputDescription
        {
            Bindings = [binding],
            Attributes = attributes
        };
    }
};
