using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with FillBottomRight placement.
/// Validates that image anchors to bottom-right corner when cropping.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Shows bottom-right portion, crops top and left when both dimensions need cropping
/// </summary>
public partial class ImagePlacementFillBottomRightTest(
    IPixelSampler pixelSampler,
    IRenderer renderer
    ) : RenderableTest(pixelSampler, renderer)
{
    [Test("Image placement bottom right")]
    public readonly static ImagePlacementFillBottomRightTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackgroundTemplate()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillBottomRight
            }
        ],
        SampleCoordinates = [
            new(1919, 1079),  // Bottom-right corner
            new(1680, 1079),  // Bottom, 87.5% across
            new(1919, 810),   // Right edge, 75% down
            new(1680, 810),   // Interior
            new(1440, 540)    // Further interior
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 1, 0, 1),            // Bottom-right: R=255, G=255 (1919, 1079)
                new(0.880f, 1, 0, 1),       // 87.5% across (1680, 1079)
                new(1, 0.863f, 0, 1),       // Right, 75% down (1919, 810)
                new(0.880f, 0.863f, 0, 1),  // Interior (1680, 810)
                new(0.753f, 0.723f, 0, 1)   // Further interior (1440, 540)
            ]
        }
    };
}
