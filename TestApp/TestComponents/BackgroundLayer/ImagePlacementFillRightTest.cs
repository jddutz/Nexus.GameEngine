using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with FillRight placement.
/// Validates that image anchors to right edge when cropping horizontally.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: When wide/short image, shows right and clips left; when narrow/tall, centers vertically
/// </summary>
public partial class ImagePlacementFillRightTest(
    IPixelSampler pixelSampler
    ) : RenderableTest(pixelSampler)
{
    [Test("Image placement middle right")]
    public readonly static ImagePlacementFillRightTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackgroundTemplate()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillRight
            }
        ],
        SampleCoordinates = [
            new(1919, 540),   // Middle-right edge
            new(1919, 270),   // Right, 25% down
            new(1919, 810),   // Right, 75% down
            new(1680, 540),   // Interior right
            new(1440, 540)    // Further interior
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(1, 0.503f, 0, 1),       // Right center: R=255, G=127.5 (1919, 540)
                new(1, 0.361f, 0, 1),       // Right, 25% down (1919, 270)
                new(1, 0.644f, 0, 1),       // Right, 75% down (1919, 810)
                new(0.880f, 0.503f, 0, 1),  // Interior (1680, 540)
                new(0.753f, 0.503f, 0, 1)   // Further interior (1440, 540)
            ]
        }
    };
}
