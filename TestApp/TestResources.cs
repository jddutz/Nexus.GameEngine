using System.Reflection;
using Nexus.GameEngine.Resources.Textures.Definitions;

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
    public static readonly SimpleTextureDefinition UvGridTexture = new(
        FilePath: "Resources/Textures/uvgrid.png",
        IsSrgb: false,  // Linear data, not sRGB color
        SourceAssembly: Assembly.GetExecutingAssembly()  // Load from TestApp assembly
    );
    
    /// <summary>
    /// Test image where R channel = X coordinate (0-255) and G channel = Y coordinate (0-255).
    /// Contains linear coordinate data for programmatic testing.
    /// </summary>
    public static readonly SimpleTextureDefinition ImageTestTexture = new(
        FilePath: "Resources/Textures/image_test.png",
        IsSrgb: false,  // Linear data, not sRGB color
        SourceAssembly: Assembly.GetExecutingAssembly()  // Load from TestApp assembly
    );
}
