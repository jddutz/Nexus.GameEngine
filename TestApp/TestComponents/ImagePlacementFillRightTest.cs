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
/// Tests ImageTextureBackground with FillRight placement.
/// Validates that image anchors to right edge when cropping horizontally.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: When wide/short image, shows right and clips left; when narrow/tall, centers vertically
/// </summary>
public partial class ImagePlacementFillRightTest(IPixelSampler pixelSampler, IWindowService windowService)
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
            Placement = BackgroundImagePlacement.FillRight
        });

        var window = windowService.GetWindow();
        int offset = 10;

        // Sample right edge to verify it's anchored there
        // Right edge should show UV X=uvMax.X (largest X value visible)
        pixelSampler.SampleCoordinates = [
            new(window.Size.X - offset, window.Size.Y / 4),         // Right edge, top quadrant
            new(window.Size.X - offset, window.Size.Y / 2),         // Right edge, center
            new(window.Size.X - offset, 3 * window.Size.Y / 4),     // Right edge, bottom quadrant
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
            TestName = $"{nameof(ImagePlacementFillRightTest)}: Sampled output should not be null",
            ExpectedResult = "not null",
            ActualResult = samples == null ? "null" : samples.Length.ToString(),
            Passed = samples != null && samples.Length > 0
        };

        if (samples == null || samples.Length == 0) yield break;

        var window = windowService.GetWindow();

        // Calculate expected UV bounds
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillRight,
            ImageSize, ImageSize,
            window.Size.X, window.Size.Y);

        // Right edge samples should show the maximum U value (right of visible area)
        // V values vary with Y position
        var expectedColors = new[] {
            new Vector4D<float>(uvMax.X, 0.25f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),  // Top quadrant
            new Vector4D<float>(uvMax.X, 0.5f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),   // Center
            new Vector4D<float>(uvMax.X, 0.75f * (uvMax.Y - uvMin.Y) + uvMin.Y, 0f, 1f),  // Bottom quadrant
        };
        
        var pixelDescriptions = new[] { "Right edge top quadrant", "Right edge center", "Right edge bottom quadrant" };
        
        for (int i = 0; i < samples[0].Length; i++)
        {
            var color = samples[0][i];
            var coord = pixelSampler.SampleCoordinates[i];
            
            yield return new()
            {
                TestName = $"{nameof(ImagePlacementFillRightTest)}: FillRight mode {pixelDescriptions[i]}",
                Description = $"Sampled at pixel ({coord.X}, {coord.Y})",
                ExpectedResult = PixelAssertions.DescribeColor(expectedColors[i]),
                ActualResult = color.HasValue ? PixelAssertions.DescribeColor(color.Value) : "null",
                Passed = color.HasValue && PixelAssertions.ColorsMatch(color.Value, expectedColors[i], tolerance: 0.02f)
            };
        }
    }
}
