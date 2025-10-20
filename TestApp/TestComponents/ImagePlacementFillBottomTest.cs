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
/// Tests ImageTextureBackground with FillBottom placement.
/// Validates that image anchors to bottom edge when cropping vertically.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: When narrow/tall image, shows bottom and clips top; when wide/short, centers horizontally
/// </summary>
public partial class ImagePlacementFillBottomTest(IPixelSampler pixelSampler, IWindowService windowService)
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
            Placement = BackgroundImagePlacement.FillBottom
        });

        var window = windowService.GetWindow();
        int offset = 10;

        // Sample bottom edge to verify it's anchored there
        // Bottom edge should show UV Y=uvMax.Y (largest Y value visible)
        pixelSampler.SampleCoordinates = [
            new(window.Size.X / 4, window.Size.Y - offset),         // Bottom-left quadrant
            new(window.Size.X / 2, window.Size.Y - offset),         // Bottom-center
            new(3 * window.Size.X / 4, window.Size.Y - offset),     // Bottom-right quadrant
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
            TestName = $"{nameof(ImagePlacementFillBottomTest)}: Sampled output should not be null",
            ExpectedResult = "not null",
            ActualResult = samples == null ? "null" : samples.Length.ToString(),
            Passed = samples != null && samples.Length > 0
        };

        if (samples == null || samples.Length == 0) yield break;

        var window = windowService.GetWindow();

        // Calculate expected UV bounds
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillBottom,
            ImageSize, ImageSize,
            window.Size.X, window.Size.Y);

        // Bottom edge samples should show the maximum V value (bottom of visible area)
        // U values vary with X position
        var expectedColors = new[] {
            new Vector4D<float>(0.25f * (uvMax.X - uvMin.X) + uvMin.X, uvMax.Y, 0f, 1f),  // Left quadrant
            new Vector4D<float>(0.5f * (uvMax.X - uvMin.X) + uvMin.X, uvMax.Y, 0f, 1f),   // Center
            new Vector4D<float>(0.75f * (uvMax.X - uvMin.X) + uvMin.X, uvMax.Y, 0f, 1f),  // Right quadrant
        };
        
        var pixelDescriptions = new[] { "Bottom-left quadrant", "Bottom-center", "Bottom-right quadrant" };
        
        for (int i = 0; i < samples[0].Length; i++)
        {
            var color = samples[0][i];
            var coord = pixelSampler.SampleCoordinates[i];
            
            yield return new()
            {
                TestName = $"{nameof(ImagePlacementFillBottomTest)}: FillBottom mode {pixelDescriptions[i]}",
                Description = $"Sampled at pixel ({coord.X}, {coord.Y})",
                ExpectedResult = PixelAssertions.DescribeColor(expectedColors[i]),
                ActualResult = color.HasValue ? PixelAssertions.DescribeColor(color.Value) : "null",
                Passed = color.HasValue && PixelAssertions.ColorsMatch(color.Value, expectedColors[i], tolerance: 0.02f)
            };
        }
    }
}
