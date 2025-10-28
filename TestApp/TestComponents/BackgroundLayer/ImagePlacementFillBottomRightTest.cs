using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Components;
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
    IWindowService windowService
    ) : RenderableTest(pixelSampler)
{
    public new record Template : TestComponent.Template { }

    [Test("Image placement bottom right")]
    public readonly static Template BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackground.Template()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillBottomRight
            }
        ]
    };

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        var window = windowService.GetWindow();
        var offset = 2;
        var imageSize = 256;
        
        SampleCoordinates = [
            new(window.Size.X - offset, window.Size.Y - offset),    // Bottom-right corner
            new(3 * window.Size.X / 4, window.Size.Y - offset),     // Bottom edge, right quadrant
            new(window.Size.X - offset, 3 * window.Size.Y / 4),     // Right edge, bottom quadrant
        ];

        // Calculate expected UV bounds
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillBottomRight,
            imageSize, imageSize,
            window.Size.X, window.Size.Y);

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            {
                0,
                new[]
                {
                    new Vector4D<float>(uvMax.X, uvMax.Y, 0f, 1f),                              // Bottom-right corner
                    new Vector4D<float>(0.75f * (uvMax.X - uvMin.X) + uvMin.X, uvMax.Y, 0f, 1f),  // Bottom edge
                    new Vector4D<float>(uvMax.X, 0.75f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),  // Right edge
                }
            }
        };
    }
}
