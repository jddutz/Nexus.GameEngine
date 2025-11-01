using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with Stretch placement.
/// Validates that image stretches non-uniformly to fill viewport (may distort aspect ratio).
/// 
/// Uses image_test.png (256x256): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Entire texture visible (UV 0,0 to 1,1), potentially distorted to fill viewport
/// </summary>
public partial class ImagePlacementStretchTest(
    IPixelSampler pixelSampler
    ) : RenderableTest(pixelSampler)
{
    [Test("Image placement stretch")]
    public readonly static ImagePlacementStretchTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackgroundTemplate()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.Stretch
            }
        ]
    };

    // Helper to convert UV coordinates to expected RGB color from image_test.png
    // UV coordinates are [0,1], image pixels are [0,255]
    private static Vector4D<float> UVToExpectedColor(float u, float v)
    {
        // R channel = X coordinate (0-255) normalized to [0,1]
        // G channel = Y coordinate (0-255) normalized to [0,1]
        var x = u * 255f;
        var y = v * 255f;
        return new Vector4D<float>(x / 255f, y / 255f, 0f, 1f);
    }
}
