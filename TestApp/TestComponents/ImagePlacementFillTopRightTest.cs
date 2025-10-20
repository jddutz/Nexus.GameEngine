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
/// Tests ImageTextureBackground with FillTopRight placement.
/// Validates that image anchors to top-right corner when cropping.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Shows top-right portion, crops bottom and left when both dimensions need cropping
/// </summary>
public partial class ImagePlacementFillTopRightTest(IPixelSampler pixelSampler, IWindowService windowService)
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
            Placement = BackgroundImagePlacement.FillTopRight
        });

        var window = windowService.GetWindow();
        int offset = 10;

        // Sample top-right corner area
        pixelSampler.SampleCoordinates = [
            new(window.Size.X - offset, offset),                    // Top-right corner
            new(3 * window.Size.X / 4, offset),                     // Top edge, right quadrant
            new(window.Size.X - offset, window.Size.Y / 4),         // Right edge, top quadrant
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
            TestName = $"{nameof(ImagePlacementFillTopRightTest)}: Sampled output should not be null",
            ExpectedResult = "not null",
            ActualResult = samples == null ? "null" : samples.Length.ToString(),
            Passed = samples != null && samples.Length > 0
        };

        if (samples == null || samples.Length == 0) yield break;

        var window = windowService.GetWindow();

        // Calculate expected UV bounds
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillTopRight,
            ImageSize, ImageSize,
            window.Size.X, window.Size.Y);

        // Expected colors at sampled positions
        var expectedColors = new[] {
            new Vector4D<float>(uvMax.X, uvMin.Y, 0f, 1f),                              // Top-right corner
            new Vector4D<float>(0.75f * (uvMax.X - uvMin.X) + uvMin.X, uvMin.Y, 0f, 1f),  // Top edge
            new Vector4D<float>(uvMax.X, 0.25f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),  // Right edge
        };
        
        var pixelDescriptions = new[] { "Top-right corner", "Top edge, right quadrant", "Right edge, top quadrant" };
        
        for (int i = 0; i < samples[0].Length; i++)
        {
            var color = samples[0][i];
            var coord = pixelSampler.SampleCoordinates[i];
            
            yield return new()
            {
                TestName = $"{nameof(ImagePlacementFillTopRightTest)}: FillTopRight mode {pixelDescriptions[i]}",
                Description = $"Sampled at pixel ({coord.X}, {coord.Y})",
                ExpectedResult = PixelAssertions.DescribeColor(expectedColors[i]),
                ActualResult = color.HasValue ? PixelAssertions.DescribeColor(color.Value) : "null",
                Passed = color.HasValue && PixelAssertions.ColorsMatch(color.Value, expectedColors[i], tolerance: 0.02f)
            };
        }
    }
}
