using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with FillLeft placement.
/// Validates that image anchors to left edge when cropping horizontally.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: When wide/short image, shows left and clips right; when narrow/tall, centers vertically
/// </summary>
public partial class ImagePlacementFillLeftTest(
    IPixelSampler pixelSampler
    ) : RenderableTest(pixelSampler)
{
    [Test("Image placement middle left")]
    public readonly static ImagePlacementFillLeftTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackgroundTemplate()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillLeft
            }
        ],
        SampleCoordinates = [
            new(0, 540),      // Middle-left edge
            new(0, 270),      // Left, 25% down
            new(0, 810),      // Left, 75% down
            new(240, 540),    // Interior left
            new(480, 540)     // Further interior
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(0, 0.503f, 0, 1),       // Left center: G=127.5 (0, 540)
                new(0, 0.361f, 0, 1),       // Left, 25% down (0, 270)
                new(0, 0.644f, 0, 1),       // Left, 75% down (0, 810)
                new(0.125f, 0.503f, 0, 1),  // Interior (240, 540)
                new(0.25f, 0.503f, 0, 1)    // Further interior (480, 540)
            ]
        }
    };
}
