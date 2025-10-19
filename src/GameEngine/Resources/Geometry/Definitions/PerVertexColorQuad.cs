using Silk.NET.Maths;
using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Nexus.GameEngine.Resources.Geometry.Definitions;

/// <summary>
/// Position-only quad geometry definition for per-vertex color rendering via UBO.
/// Vertex format: Position(Vec2) = 8 bytes per vertex
/// Colors are provided via Uniform Buffer Object (not vertex data).
/// Four vertices arranged as a triangle strip forming a full-screen quad.
/// 
/// COORDINATE SYSTEM AND VERTEX ORDER STANDARD:
/// - Vulkan NDC: X=[-1,1] left-to-right, Y=[-1,1] top-to-bottom (Y=-1 is top)
/// - Screen space: X=[0,width] left-to-right, Y=[0,height] top-to-bottom
/// - Triangle strip topology renders two triangles: (0,1,2) and (1,2,3)
/// - Winding order (when viewed from camera): Triangle 1 is CCW, Triangle 2 is CW
/// - Face culling is DISABLED for BackgroundLayer (CullMode.None)
/// 
/// VERTEX INDEX TO SCREEN POSITION MAPPING:
///   Index 0 → Top-Left      (NDC: -1, -1)  (Screen: 0, 0)
///   Index 1 → Bottom-Left   (NDC: -1, +1)  (Screen: 0, height)
///   Index 2 → Top-Right     (NDC: +1, -1)  (Screen: width, 0)
///   Index 3 → Bottom-Right  (NDC: +1, +1)  (Screen: width, height)
/// 
/// This matches the OpenGL/Vulkan standard quad vertex ordering.
/// UBO contains 4 colors indexed by gl_VertexIndex.
/// </summary>
public record PerVertexColorQuad : IGeometryDefinition
{
    /// <inheritdoc />
    public string Name => "PerVertexColorQuad";

    // Four vertices forming a full-screen quad using triangle strip
    // Triangle strip order: TL -> BL -> TR -> BR (indices 0, 1, 2, 3)
    private readonly Vector2D<float>[] _positions =
    [
        new(-1f, -1f),  // Index 0: Top-left
        new(-1f,  1f),  // Index 1: Bottom-left
        new( 1f, -1f),  // Index 2: Top-right
        new( 1f,  1f),  // Index 3: Bottom-right
    ];

    /// <inheritdoc />
    public uint VertexCount => (uint)_positions.Length;

    /// <inheritdoc />
    public uint Stride => (uint)Unsafe.SizeOf<Vector2D<float>>();  // 8 bytes

    /// <inheritdoc />
    public ReadOnlySpan<byte> GetVertexData()
    {
        // Return position data only (colors come from UBO)
        return MemoryMarshal.AsBytes(_positions.AsSpan());
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
            Stride = (uint)Unsafe.SizeOf<Vector2D<float>>(),  // 8 bytes
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
            }
        };

        return new Graphics.Pipelines.VertexInputDescription
        {
            Bindings = [binding],
            Attributes = attributes
        };
    }
}
