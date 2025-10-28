using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Components;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with FillTopLeft placement.
/// Validates that image anchors to top-left corner when cropping.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Shows top-left portion, crops bottom and right when both dimensions need cropping
/// </summary>
public partial class ImagePlacementFillTopLeftTest(
    IPixelSampler pixelSampler,
    IWindowService windowService
    ) : RenderableTest(pixelSampler)
{
    public new record Template : TestComponent.Template { }

    [Test("Image placement top left")]
    public readonly static Template BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackground.Template()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillTopLeft
            }
        ]
    };

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        var window = windowService.GetWindow();
        var offset = 2;
        var imageSize = 256;
        
        SampleCoordinates = [
            new(offset, offset),                                    // Top-left corner
            new(window.Size.X / 4, offset),                         // Top edge, left quadrant
            new(offset, window.Size.Y / 4),                         // Left edge, top quadrant
        ];

        // Calculate expected UV bounds
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillTopLeft,
            imageSize, imageSize,
            window.Size.X, window.Size.Y);

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            {
                0,
                new[]
                {
                    new Vector4D<float>(uvMin.X, uvMin.Y, 0f, 1f),                              // Top-left corner
                    new Vector4D<float>(0.25f * (uvMax.X - uvMin.X) + uvMin.X, uvMin.Y, 0f, 1f),  // Top edge
                    new Vector4D<float>(uvMin.X, 0.25f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),  // Left edge
                }
            }
        };
    }
}
