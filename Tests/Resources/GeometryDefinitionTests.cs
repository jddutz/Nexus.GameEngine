using Xunit;
using Nexus.GameEngine.Resources.Geometry;

namespace Tests.Resources;

public class GeometryDefinitionTests
{
    [Fact]
    public void FullScreenQuad_ShouldHaveValidDefinition()
    {
        // Arrange & Act
        var quad = Geometry.FullScreenQuad;

        // Assert
        Assert.Equal("FullScreenQuad", quad.Name);
        Assert.False(quad.Vertices.IsEmpty);
        Assert.True(quad.Indices.HasValue);
        Assert.NotEmpty(quad.Attributes);

        // Should have 4 vertices with 4 components each (x, y, u, v)
        Assert.Equal(16, quad.Vertices.Length); // 4 vertices * 4 components

        // Should have 6 indices (2 triangles)
        Assert.Equal(6, quad.Indices.Value.Length);

        // Should have 2 attributes (position and texcoord)
        Assert.Equal(2, quad.Attributes.Count);
    }

    [Fact]
    public void FullScreenQuad_ShouldHaveCorrectVertices()
    {
        // Arrange & Act
        var quad = Geometry.FullScreenQuad;
        var vertices = quad.Vertices.ToArray();

        // Assert - Check first vertex (bottom-left: -1, -1, 0, 0)
        Assert.Equal(-1.0f, vertices[0]); // x
        Assert.Equal(-1.0f, vertices[1]); // y
        Assert.Equal(0.0f, vertices[2]);  // u
        Assert.Equal(0.0f, vertices[3]);  // v

        // Check last vertex (top-left: -1, 1, 0, 1)
        Assert.Equal(-1.0f, vertices[12]); // x
        Assert.Equal(1.0f, vertices[13]);  // y
        Assert.Equal(0.0f, vertices[14]);  // u
        Assert.Equal(1.0f, vertices[15]);  // v
    }

    [Fact]
    public void FullScreenQuad_ShouldHaveCorrectIndices()
    {
        // Arrange & Act
        var quad = Geometry.FullScreenQuad;
        var indices = quad.Indices!.Value.ToArray();

        // Assert - Two triangles: (0,1,2) and (2,3,0)
        uint[] expectedIndices = { 0, 1, 2, 2, 3, 0 };
        Assert.Equal(expectedIndices, indices);
    }

    [Fact]
    public void FullScreenQuad_ShouldHaveCorrectAttributes()
    {
        // Arrange & Act
        var quad = Geometry.FullScreenQuad;
        var attributes = quad.Attributes.ToList();

        // Assert
        Assert.Equal(2, attributes.Count);

        // Position attribute
        var posAttr = attributes[0];
        Assert.Equal(0u, posAttr.Location);
        Assert.Equal(2, posAttr.ComponentCount);
        Assert.Equal(VertexAttribPointerType.Float, posAttr.Type);

        // Texture coordinate attribute
        var texAttr = attributes[1];
        Assert.Equal(1u, texAttr.Location);
        Assert.Equal(2, texAttr.ComponentCount);
        Assert.Equal(VertexAttribPointerType.Float, texAttr.Type);
        Assert.Equal((nint)(2 * sizeof(float)), texAttr.Offset);
        Assert.Equal((uint)(4 * sizeof(float)), texAttr.Stride);
    }

    [Fact]
    public void VertexAttribute_Position2D_ShouldCreateCorrectAttribute()
    {
        // Arrange & Act
        var attr = VertexAttribute.Position2D(5);

        // Assert
        Assert.Equal(5u, attr.Location);
        Assert.Equal(2, attr.ComponentCount);
        Assert.Equal(VertexAttribPointerType.Float, attr.Type);
        Assert.False(attr.Normalized);
        Assert.Equal(0u, attr.Stride);
        Assert.Equal(0, attr.Offset);
    }

    [Fact]
    public void VertexAttribute_TexCoord2D_ShouldCreateCorrectAttribute()
    {
        // Arrange & Act
        var attr = VertexAttribute.TexCoord2D(1, offset: 8, stride: 16);

        // Assert
        Assert.Equal(1u, attr.Location);
        Assert.Equal(2, attr.ComponentCount);
        Assert.Equal(VertexAttribPointerType.Float, attr.Type);
        Assert.False(attr.Normalized);
        Assert.Equal(16u, attr.Stride);
        Assert.Equal(8, attr.Offset);
    }
}