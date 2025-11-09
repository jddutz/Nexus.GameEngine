using Nexus.GameEngine.Resources.Fonts;

namespace Tests;

/// <summary>
/// Unit tests for FontResourceManager logic that doesn't require Vulkan context.
/// Integration tests for full font resource creation are in TestApp.
/// </summary>
public class FontResourceManagerTests
{
    [Fact]
    public void FontDefinitions_AreDifferent()
    {
        // Arrange
        var fontDef1 = FontDefinitions.RobotoNormal;
        var fontDef2 = FontDefinitions.RobotoTitle;
        
        // Assert
        Assert.NotSame(fontDef1, fontDef2);
        Assert.NotEqual(fontDef1.Name, fontDef2.Name);
    }

    // NOTE: Font resource creation tests removed.
    // FontResourceManager requires real Vulkan context (cannot be mocked).
    // Test font atlas generation, GPU upload, and resource caching via
    // integration tests in TestApp where full Vulkan context is available.
}
