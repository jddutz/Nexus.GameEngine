using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Resources.Geometry.Definitions;

/// <summary>
/// Colored quad geometry definition.
/// Vertex format: Position(Vec2) + Color(Vec4)
/// Four vertices arranged as a triangle strip forming a full-screen quad.
/// Note: In Vulkan, Y=-1 is top, Y=1 is bottom (inverted from OpenGL)
/// Screen positions:
/// - Vertex 0 (Top-left): Red
/// - Vertex 1 (Bottom-left): Green
/// - Vertex 2 (Top-right): Black
/// - Vertex 3 (Bottom-right): Blue
/// </summary>
public record ColorQuad : IGeometryDefinition
{
    /// <inheritdoc />
    public string Name => "ColorQuad";

    // Four vertices forming a full-screen quad using triangle strip
    // Triangle strip order: TL -> BL -> TR -> BR
    private readonly Vertex<Vector2D<float>, Vector4D<float>>[] _vertices =
    [
        new() { Position = new(-1f, -1f), Attribute1 = Colors.Red },   // Top-left
        new() { Position = new(-1f,  1f), Attribute1 = Colors.Green }, // Bottom-left
        new() { Position = new( 1f, -1f), Attribute1 = Colors.Black }, // Top-right
        new() { Position = new( 1f,  1f), Attribute1 = Colors.Blue },  // Bottom-right
    ];

    /// <inheritdoc />
    public uint VertexCount => (uint)_vertices.Length;

    /// <inheritdoc />
    public uint Stride => (uint)Unsafe.SizeOf<Vertex<Vector2D<float>, Vector4D<float>>>();

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
            Stride = (uint)Unsafe.SizeOf<Vertex<Vector2D<float>, Vector4D<float>>>(),
            InputRate = VertexInputRate.Vertex
        };

        var attributes = new VertexInputAttributeDescription[]
        {
            // Position (vec2) at location 0
            new()
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32Sfloat,
                Offset = 0
            },
            // Color (vec3) at location 1
            new()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Unsafe.SizeOf<Vector2D<float>>()
            }
        };

        return new Graphics.Pipelines.VertexInputDescription
        {
            Bindings = [binding],
            Attributes = attributes
        };
    }
};
