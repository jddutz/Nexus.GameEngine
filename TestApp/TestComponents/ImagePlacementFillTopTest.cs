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
/// Tests ImageTextureBackground with FillTop placement.
/// Validates that image anchors to top edge when cropping vertically.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: When narrow/tall image, shows top and clips bottom; when wide/short, centers horizontally
/// </summary>
public class ImagePlacementFillTopTest(IPixelSampler pixelSampler, IWindowService windowService)
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
            Placement = BackgroundImagePlacement.FillTop
        });

        var window = windowService.GetWindow();
        int offset = 10;

        // Sample top edge to verify it's anchored there
        // Top edge should show UV Y=uvMin.Y (smallest Y value visible)
        pixelSampler.SampleCoordinates = [
            new(window.Size.X / 4, offset),                         // Top-left quadrant
            new(window.Size.X / 2, offset),                         // Top-center
            new(3 * window.Size.X / 4, offset),                     // Top-right quadrant
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
            TestName = $"{nameof(ImagePlacementFillTopTest)}: Sampled output should not be null",
            ExpectedResult = "not null",
            ActualResult = samples == null ? "null" : samples.Length.ToString(),
            Passed = samples != null && samples.Length > 0
        };

        if (samples == null || samples.Length == 0) yield break;

        var window = windowService.GetWindow();

        // Calculate expected UV bounds
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillTop,
            ImageSize, ImageSize,
            window.Size.X, window.Size.Y);

        // Top edge samples should show the minimum V value (top of visible area)
        // U values vary with X position
        var expectedColors = new[] {
            new Vector4D<float>(0.25f * (uvMax.X - uvMin.X) + uvMin.X, uvMin.Y, 0f, 1f),  // Left quadrant
            new Vector4D<float>(0.5f * (uvMax.X - uvMin.X) + uvMin.X, uvMin.Y, 0f, 1f),   // Center
            new Vector4D<float>(0.75f * (uvMax.X - uvMin.X) + uvMin.X, uvMin.Y, 0f, 1f),  // Right quadrant
        };
        
        var pixelDescriptions = new[] { "Top-left quadrant", "Top-center", "Top-right quadrant" };
        
        for (int i = 0; i < samples[0].Length; i++)
        {
            var color = samples[0][i];
            var coord = pixelSampler.SampleCoordinates[i];
            
            yield return new()
            {
                TestName = $"{nameof(ImagePlacementFillTopTest)}: FillTop mode {pixelDescriptions[i]}",
                Description = $"Sampled at pixel ({coord.X}, {coord.Y})",
                ExpectedResult = PixelAssertions.DescribeColor(expectedColors[i]),
                ActualResult = color.HasValue ? PixelAssertions.DescribeColor(color.Value) : "null",
                Passed = color.HasValue && PixelAssertions.ColorsMatch(color.Value, expectedColors[i], tolerance: 0.02f)
            };
        }
    }
}
