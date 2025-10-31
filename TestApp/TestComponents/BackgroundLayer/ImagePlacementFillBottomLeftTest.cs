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
    IPixelSampler pixelSampler,
    IWindowService windowService
    ) : RenderableTest(pixelSampler)
{
    public new record Template : TestComponent.Template { }

    [Test("Image placement bottom left")]
    public readonly static Template BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackground.Template()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillBottomLeft
            }
        ]
    };

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        var window = windowService.GetWindow();
        var offset = 2;
        var imageSize = 256;
        
        SampleCoordinates = [
            new(offset, window.Size.Y - offset),                    // Bottom-left corner
            new(window.Size.X / 4, window.Size.Y - offset),         // Bottom edge, left quadrant
            new(offset, 3 * window.Size.Y / 4),                     // Left edge, bottom quadrant
        ];

        // Calculate expected UV bounds
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillBottomLeft,
            imageSize, imageSize,
            window.Size.X, window.Size.Y);

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            {
                0,
                new[]
                {
                    new Vector4D<float>(uvMin.X, uvMax.Y, 0f, 1f),                                // Bottom-left corner
                    new Vector4D<float>(0.25f * (uvMax.X - uvMin.X) + uvMin.X, uvMax.Y, 0f, 1f),  // Bottom edge
                    new Vector4D<float>(uvMin.X, 0.75f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),  // Left edge
                }
            }
        };
    }
}
