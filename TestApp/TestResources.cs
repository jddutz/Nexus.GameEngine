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
}
