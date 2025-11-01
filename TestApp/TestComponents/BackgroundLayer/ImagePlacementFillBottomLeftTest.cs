using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with FillBottomLeft placement.
/// Validates that image anchors to bottom-left corner when cropping.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Shows bottom-left portion, crops top and right when both dimensions need cropping
/// </summary>
public partial class ImagePlacementFillBottomLeftTest(
    IPixelSampler pixelSampler
    ) : RenderableTest(pixelSampler)
{
    [Test("Image placement bottom left")]
    public readonly static ImagePlacementFillBottomLeftTestTemplate BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackgroundTemplate()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillBottomLeft
            }
        ],
        SampleCoordinates = [
            new(0, 1079),     // Bottom-left corner
            new(240, 1079),   // Bottom, 25% across
            new(0, 810),      // Left edge, 75% down
            new(240, 810),    // Interior
            new(480, 540)     // Further interior
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                new(0, 1, 0, 1),            // Bottom-left: G=255 (0, 1079)
                new(0.125f, 1, 0, 1),       // 12.5% across (240, 1079)
                new(0, 0.863f, 0, 1),       // Left, 75% down (0, 810)
                new(0.125f, 0.863f, 0, 1),  // Interior (240, 810)
                new(0.25f, 0.723f, 0, 1)    // Further interior (480, 540)
            ]
        }
    };
}
