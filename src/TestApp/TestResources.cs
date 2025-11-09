using System.Reflection;
using Nexus.GameEngine.Resources.Textures;

namespace TestApp;

/// <summary>
/// Static resource definitions for test components.
/// Reusable resource definitions following DRY principle.
/// </summary>
public static class TestResources
{
    /// <summary>
    /// UV grid texture for visual testing of image placement and transformations.
    /// Contains linear coordinate data (not sRGB color data).
    /// </summary>
    public static readonly TextureDefinition UvGridTexture = new()
    {
        Name = "uvgrid",
        Source = new EmbeddedPngTextureSource(
            "Resources/Textures/uvgrid.png",
            Assembly.GetExecutingAssembly(),
            isSrgb: false)  // Linear data, not sRGB color
    };
    
    /// <summary>
    /// Test image where R channel = X coordinate (0-255) and G channel = Y coordinate (0-255).
    /// Contains linear coordinate data for programmatic testing.
    /// </summary>
    public static readonly TextureDefinition ImageTestTexture = new()
    {
        Name = "image_test",
        Source = new EmbeddedPngTextureSource(
            "Resources/Textures/image_test.png",
            Assembly.GetExecutingAssembly(),
            isSrgb: false)  // Linear data, not sRGB color
    };

    /// <summary>
    /// Solid red texture (256×256, #FF0000) for basic texture testing.
    /// </summary>
    public static readonly TextureDefinition TestTexture = new()
    {
        Name = "test_texture",
        Source = new EmbeddedPngTextureSource(
            "Resources/Textures/test_texture.png",
            Assembly.GetExecutingAssembly(),
            isSrgb: true)  // sRGB color data
    };

    /// <summary>
    /// Texture atlas (512×512) with 4 colored quadrants: red, green, blue, yellow.
    /// Used for testing UV coordinate control and texture atlas rendering.
    /// </summary>
    public static readonly TextureDefinition TestAtlas = new()
    {
        Name = "test_atlas",
        Source = new EmbeddedPngTextureSource(
            "Resources/Textures/test_atlas.png",
            Assembly.GetExecutingAssembly(),
            isSrgb: true)  // sRGB color data
    };

    /// <summary>
    /// Small icon texture (64×64) with white square and black border.
    /// Used for testing small texture rendering.
    /// </summary>
    public static readonly TextureDefinition TestIcon = new()
    {
        Name = "test_icon",
        Source = new EmbeddedPngTextureSource(
            "Resources/Textures/test_icon.png",
            Assembly.GetExecutingAssembly(),
            isSrgb: true)  // sRGB color data
    };

    /// <summary>
    /// Solid white texture (256×256, #FFFFFF) for tint color testing.
    /// When multiplied by a tint color, displays the exact tint color.
    /// </summary>
    public static readonly TextureDefinition WhiteTexture = new()
    {
        Name = "white_texture",
        Source = new EmbeddedPngTextureSource(
            "Resources/Textures/white_texture.png",
            Assembly.GetExecutingAssembly(),
            isSrgb: true)  // sRGB color data
    };
}
