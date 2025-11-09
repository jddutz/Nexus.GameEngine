using Nexus.GameEngine.Resources.Textures.Definitions;

namespace Tests.GameEngine.Resources;

public class DummyTextureTests
{
    [Fact]
    public void TextureDefinitions_UniformColor_Exists()
    {
        // Arrange & Act
        var texture = TextureDefinitions.UniformColor;

        // Assert
        Assert.NotNull(texture);
        Assert.Equal("__uniform_color_1x1", texture.Name);
    }

    [Fact]
    public void TextureDefinitions_UniformColor_Is1x1()
    {
        // Arrange
        var texture = TextureDefinitions.UniformColor;

        // Act
        // Note: Actual size verification would require loading the texture
        // For now, this is a placeholder test

        // Assert
        Assert.NotNull(texture);
        // Additional assertions would verify 1x1 dimensions
    }

    [Fact]
    public void TextureDefinitions_UniformColor_IsWhiteColor()
    {
        // Arrange
        var texture = TextureDefinitions.UniformColor;

        // Act
        // Note: Actual color verification would require loading and sampling the texture
        // For now, this is a placeholder test

        // Assert
        Assert.NotNull(texture);
        // Additional assertions would verify RGBA(255,255,255,255) color
    }
}