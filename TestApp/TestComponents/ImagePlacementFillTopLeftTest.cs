using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Components;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents;

/// <summary>
/// Tests ImageTextureBackground with FillTopLeft placement.
/// Validates that image anchors to top-left corner when cropping.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Shows top-left portion, crops bottom and right when both dimensions need cropping
/// </summary>
public partial class ImagePlacementFillTopLeftTest(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    private int framesRendered = 0;
    private readonly int frameCount = 1;
    private const int ImageSize = 256;

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        CreateChild(new ImageTextureBackground.Template()
        {
            TextureDefinition = TestResources.ImageTestTexture,
            Placement = BackgroundImagePlacement.FillTopLeft
        });

        var window = windowService.GetWindow();
        int offset = 10;

        // Sample top-left corner area
        pixelSampler.SampleCoordinates = [
            new(offset, offset),                                    // Top-left corner
            new(window.Size.X / 4, offset),                         // Top edge, left quadrant
            new(offset, window.Size.Y / 4),                         // Left edge, top quadrant
        ];
        
        pixelSampler.Enabled = true;
    }

    protected override void OnActivate()
    {
        pixelSampler.Activate();
    }

    protected override void OnUpdate(double deltaTime)
    {
        // Render one frame then deactivate
        if (framesRendered > frameCount)
        {
            pixelSampler.Deactivate();
            Deactivate();
        }
        framesRendered++;
    }

    public IEnumerable<TestResult> GetTestResults()
    {
        var samples = pixelSampler.GetResults();

        yield return new TestResult
        {
            TestName = $"{nameof(ImagePlacementFillTopLeftTest)}: Sampled output should not be null",
            ExpectedResult = "not null",
            ActualResult = samples == null ? "null" : samples.Length.ToString(),
            Passed = samples != null && samples.Length > 0
        };

        if (samples == null || samples.Length == 0) yield break;

        var window = windowService.GetWindow();

        // Calculate expected UV bounds
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillTopLeft,
            ImageSize, ImageSize,
            window.Size.X, window.Size.Y);

        // Expected colors at sampled positions
        var expectedColors = new[] {
            new Vector4D<float>(uvMin.X, uvMin.Y, 0f, 1f),                              // Top-left corner
            new Vector4D<float>(0.25f * (uvMax.X - uvMin.X) + uvMin.X, uvMin.Y, 0f, 1f),  // Top edge
            new Vector4D<float>(uvMin.X, 0.25f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),  // Left edge
        };
        
        var pixelDescriptions = new[] { "Top-left corner", "Top edge, left quadrant", "Left edge, top quadrant" };
        
        for (int i = 0; i < samples[0].Length; i++)
        {
            var color = samples[0][i];
            var coord = pixelSampler.SampleCoordinates[i];
            
            yield return new()
            {
                TestName = $"{nameof(ImagePlacementFillTopLeftTest)}: FillTopLeft mode {pixelDescriptions[i]}",
                Description = $"Sampled at pixel ({coord.X}, {coord.Y})",
                ExpectedResult = PixelAssertions.DescribeColor(expectedColors[i]),
                ActualResult = color.HasValue ? PixelAssertions.DescribeColor(color.Value) : "null",
                Passed = color.HasValue && PixelAssertions.ColorsMatch(color.Value, expectedColors[i], tolerance: 0.02f)
            };
        }
    }
}
