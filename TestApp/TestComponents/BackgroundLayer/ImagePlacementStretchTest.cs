using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
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
    IPixelSampler pixelSampler,
    IRenderer renderer
    ) : RenderableTest(pixelSampler, renderer)
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
        ],
        SampleCoordinates = [
            new(0, 0),        // Top-left UV (0,0)
            new(1919, 0),     // Top-right UV (1,0)
            new(0, 1079),     // Bottom-left UV (0,1)
            new(1919, 1079),  // Bottom-right UV (1,1)
            new(960, 540)     // Center UV (0.5,0.5)
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(0, 0, 0, 1),      // Top-left: R=0, G=0
                new(1, 0, 0, 1),      // Top-right: R=255, G=0
                new(0, 1, 0, 1),      // Bottom-left: R=0, G=255
                new(1, 1, 0, 1),      // Bottom-right: R=255, G=255
                new(0.5f, 0.5f, 0, 1) // Center: R=127.5, G=127.5
            ]
        }
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
