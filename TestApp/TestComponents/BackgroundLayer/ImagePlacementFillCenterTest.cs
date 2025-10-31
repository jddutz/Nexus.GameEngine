using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.BackgroundLayers;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests ImageTextureBackground with FillCenter placement (DEFAULT).
/// Validates that image maintains aspect ratio, fills viewport, and centers when cropping.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Aspect ratio maintained, excess cropped equally from both sides, center visible
/// </summary>
public partial class ImagePlacementFillCenterTest(
    IPixelSampler pixelSampler,
    IWindowService windowService
    ) : RenderableTest(pixelSampler)
{
    public new record Template : TestComponent.Template { }

    [Test("Image placement middle center")]
    public readonly static Template BackgroundLayerTest = new()
    {
        Subcomponents = [
            new ImageTextureBackground.Template()
            {
                TextureDefinition = TestResources.ImageTestTexture,
                Placement = BackgroundImagePlacement.FillCenter
            }
        ]
    };

    protected override void OnLoad(Configurable.Template? componentTemplate)
    {
        var window = windowService.GetWindow();
        var imageSize = 256;

        SampleCoordinates = [
            new(window.Size.X / 2, window.Size.Y / 2),              // Center
            new(window.Size.X / 4, window.Size.Y / 2),              // Left-center
            new(3 * window.Size.X / 4, window.Size.Y / 2),          // Right-center
            new(window.Size.X / 2, window.Size.Y / 4),              // Top-center
            new(window.Size.X / 2, 3 * window.Size.Y / 4),          // Bottom-center
        ];

        // Calculate expected UV bounds using BackgroundImagePlacement logic
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillCenter,
            imageSize, imageSize,
            window.Size.X, window.Size.Y);

        // Helper to map screen coordinate to normalized viewport [0,1]
        Vector2D<float> ToViewportNorm(Vector2D<int> pt) => new(
            (float)pt.X / (window.Size.X - 1),
            (float)pt.Y / (window.Size.Y - 1)
        );

        // Helper to map normalized viewport to UV
        Vector2D<float> ToUV(Vector2D<float> norm) => new(
            uvMin.X + (uvMax.X - uvMin.X) * norm.X,
            uvMin.Y + (uvMax.Y - uvMin.Y) * norm.Y
        );

        // Compute expected color for each sample
        var expectedColors = SampleCoordinates
            .Select(pt => {
                var norm = ToViewportNorm(pt);
                var uv = ToUV(norm);
                return new Vector4D<float>(uv.X, uv.Y, 0f, 1f);
            })
            .ToArray();

        ExpectedResults = new Dictionary<int, Vector4D<float>[]>
        {
            { 0, expectedColors }
        };
    }
}
