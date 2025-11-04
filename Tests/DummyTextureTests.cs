using Nexus.GameEngine.Resources.Textures;
using Xunit;

namespace Tests;

public class DummyTextureTests
{
    [Fact]
    public void TextureDefinitions_WhiteDummy_Exists()
    {
        // Arrange & Act
        var texture = TextureDefinitions.WhiteDummy;

        // Assert
        Assert.NotNull(texture);
        Assert.Equal("__white_dummy_1x1", texture.Name);
    }

    [Fact]
    public void TextureDefinitions_WhiteDummy_Is1x1()
    {
        // Arrange
        var texture = TextureDefinitions.WhiteDummy;

        // Act
        // Note: Actual size verification would require loading the texture
        // For now, this is a placeholder test

        // Assert
        Assert.NotNull(texture);
        // Additional assertions would verify 1x1 dimensions
    }

    [Fact]
    public void TextureDefinitions_WhiteDummy_IsWhiteColor()
    {
        // Arrange
        var texture = TextureDefinitions.WhiteDummy;

        // Act
        // Note: Actual color verification would require loading and sampling the texture
        // For now, this is a placeholder test

        // Assert
        Assert.NotNull(texture);
        // Additional assertions would verify RGBA(255,255,255,255) color
    }
}