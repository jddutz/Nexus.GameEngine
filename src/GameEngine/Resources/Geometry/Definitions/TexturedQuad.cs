using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Resources.Geometry.Definitions;

/// <summary>
/// Textured quad geometry definition with position and UV texture coordinates.
/// Vertex format: Position(Vec2) + TexCoord(Vec2) = 16 bytes per vertex
/// Four vertices arranged as a triangle strip forming a full-screen quad.
/// 
/// COORDINATE SYSTEM AND VERTEX ORDER STANDARD:
/// - Vulkan NDC: X=[-1,1] left-to-right, Y=[-1,1] top-to-bottom (Y=-1 is top)
/// - UV space: U=[0,1] left-to-right, V=[0,1] top-to-bottom
/// - Triangle strip topology renders two triangles: (0,1,2) and (1,2,3)
/// - Face culling is DISABLED for BackgroundLayer (CullMode.None)
/// 
/// VERTEX INDEX TO SCREEN AND UV MAPPING:
///   Index 0 → Top-Left      (NDC: -1, -1)  (UV: 0, 0)
///   Index 1 → Bottom-Left   (NDC: -1, +1)  (UV: 0, 1)
///   Index 2 → Top-Right     (NDC: +1, -1)  (UV: 1, 0)
///   Index 3 → Bottom-Right  (NDC: +1, +1)  (UV: 1, 1)
/// </summary>
public record TexturedQuad : IGeometryDefinition
{
    /// <inheritdoc />
    public string Name => "TexturedQuad";

    // Four vertices forming a full-screen quad using triangle strip
    // Triangle strip order: TL -> BL -> TR -> BR (indices 0, 1, 2, 3)
    private readonly Vertex<Vector2D<float>, Vector2D<float>>[] _vertices =
    [
        new() { Position = new(-1f, -1f), Attribute1 = new(0f, 0f) }, // Top-left
        new() { Position = new(-1f,  1f), Attribute1 = new(0f, 1f) }, // Bottom-left
        new() { Position = new( 1f, -1f), Attribute1 = new(1f, 0f) }, // Top-right
        new() { Position = new( 1f,  1f), Attribute1 = new(1f, 1f) }  // Bottom-right
    ];

    /// <inheritdoc />
    public uint VertexCount => (uint)_vertices.Length;

    /// <inheritdoc />
    public uint Stride => (uint)Unsafe.SizeOf<Vertex<Vector2D<float>, Vector2D<float>>>();  // 16 bytes

    /// <inheritdoc />
    public ReadOnlySpan<byte> GetVertexData()
    {
        var bytes = MemoryMarshal.AsBytes(_vertices.AsSpan());
        
        // Debug: Log first vertex as hex to verify memory layout
        System.Diagnostics.Debug.WriteLine($"TexturedQuad first 16 bytes (vertex 0): {BitConverter.ToString(bytes[..16].ToArray())}");
        System.Diagnostics.Debug.WriteLine($"  Expected: Position(-1,-1)=0x00-00-80-BF-00-00-80-BF, UV(0,0)=0x00-00-00-00-00-00-00-00");
        
        return bytes;
    }

    /// <summary>
    /// Gets the vertex input description for this geometry.
    /// </summary>
    public static Graphics.Pipelines.VertexInputDescription GetVertexInputDescription()
    {
        var binding = new VertexInputBindingDescription
        {
            Binding = 0,
            Stride = (uint)Unsafe.SizeOf<Vertex<Vector2D<float>, Vector2D<float>>>(),  // 16 bytes
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
            // TexCoord (vec2) at location 1
            new()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Unsafe.SizeOf<Vector2D<float>>()  // 8 bytes
            }
        };

        return new Graphics.Pipelines.VertexInputDescription
        {
            Bindings = [binding],
            Attributes = attributes
        };
    }
}
