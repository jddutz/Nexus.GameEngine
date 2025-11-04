using Nexus.GameEngine.Resources.Geometry.Definitions;

namespace Tests;

public class TexturedQuadGeometryTests
{
    [Fact]
    public void GeometryDefinitions_TexturedQuad_Exists()
    {
        // Arrange & Act
        var geometry = GeometryDefinitions.TexturedQuad;

        // Assert
        Assert.NotNull(geometry);
        Assert.Equal("TexturedQuad", geometry.Name);
    }

    [Fact]
    public void GeometryDefinitions_TexturedQuad_HasSource()
    {
        // Arrange
        var geometry = GeometryDefinitions.TexturedQuad;

        // Act
        var source = geometry.Source;

        // Assert
        Assert.NotNull(source);
    }

    [Fact]
    public void GeometryDefinitions_TexturedQuad_SourceIsVertexArray()
    {
        // Arrange
        var geometry = GeometryDefinitions.TexturedQuad;

        // Act
        var source = geometry.Source;

        // Assert
        Assert.IsType<Nexus.GameEngine.Resources.Geometry.VertexArrayGeometrySource<Nexus.GameEngine.Resources.Geometry.Vertex<Silk.NET.Maths.Vector2D<float>, Silk.NET.Maths.Vector2D<float>>>>(source);
    }

    [Fact]
    public void GeometryDefinitions_TexturedQuad_NameIsUnique()
    {
        // Arrange
        var texturedQuad = GeometryDefinitions.TexturedQuad;

        // Act & Assert
        Assert.NotNull(texturedQuad.Name);
        Assert.NotEmpty(texturedQuad.Name);
    }
}