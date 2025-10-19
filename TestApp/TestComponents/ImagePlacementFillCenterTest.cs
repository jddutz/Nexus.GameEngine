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
/// Tests ImageTextureBackground with FillCenter placement (DEFAULT).
/// Validates that image maintains aspect ratio, fills viewport, and centers when cropping.
/// 
/// Uses image_test.png (256x256 square): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Aspect ratio maintained, excess cropped equally from both sides, center visible
/// </summary>
public class ImagePlacementFillCenterTest(IPixelSampler pixelSampler, IWindowService windowService)
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
            Placement = BackgroundImagePlacement.FillCenter
        });

        var window = windowService.GetWindow();

        // Sample center (should always be image center at UV 0.5,0.5 = RGB(127,127,0))
        // Sample near edges to verify centering behavior
        pixelSampler.SampleCoordinates = [
            new(window.Size.X / 2, window.Size.Y / 2),              // Center -> always UV(0.5,0.5)
            new(window.Size.X / 4, window.Size.Y / 2),              // Left-center quadrant
            new(3 * window.Size.X / 4, window.Size.Y / 2),          // Right-center quadrant
            new(window.Size.X / 2, window.Size.Y / 4),              // Top-center quadrant
            new(window.Size.X / 2, 3 * window.Size.Y / 4),          // Bottom-center quadrant
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
            TestName = $"{nameof(ImagePlacementFillCenterTest)}: Sampled output should not be null",
            ExpectedResult = "not null",
            ActualResult = samples == null ? "null" : samples.Length.ToString(),
            Passed = samples != null && samples.Length > 0
        };

        if (samples == null || samples.Length == 0) yield break;

        var window = windowService.GetWindow();

        // Calculate expected UV bounds using BackgroundImagePlacement logic
        var (uvMin, uvMax) = BackgroundImagePlacement.CalculateUVBounds(
            BackgroundImagePlacement.FillCenter,
            ImageSize, ImageSize,
            window.Size.X, window.Size.Y);

        // Log UV bounds for debugging
        var uvBoundsInfo = $"UV Bounds: min=({uvMin.X:F4}, {uvMin.Y:F4}), max=({uvMax.X:F4}, {uvMax.Y:F4})";
        
        // Center of viewport should show center of image (UV 0.5, 0.5)
        var centerU = (uvMin.X + uvMax.X) / 2f;
        var centerV = (uvMin.Y + uvMax.Y) / 2f;
        var expectedCenterColor = new Vector4D<float>(centerU, centerV, 0f, 1f);
        
        var centerColor = samples[0][0];
        var centerCoord = pixelSampler.SampleCoordinates[0];

        yield return new()
        {
            TestName = $"{nameof(ImagePlacementFillCenterTest)}: FillCenter mode center pixel",
            Description = $"Sampled at pixel ({centerCoord.X}, {centerCoord.Y}), {uvBoundsInfo}",
            ExpectedResult = PixelAssertions.DescribeColor(expectedCenterColor),
            ActualResult = centerColor.HasValue ? PixelAssertions.DescribeColor(centerColor.Value) : "null",
            Passed = centerColor.HasValue && PixelAssertions.ColorsMatch(centerColor.Value, expectedCenterColor, tolerance: 0.02f)
        };

        // Verify other samples have valid colors (center portion of image visible)
        var pixelDescriptions = new[] { "Center", "Left-center", "Right-center", "Top-center", "Bottom-center" };
        
        for (int i = 1; i < samples[0].Length; i++)
        {
            var color = samples[0][i];
            var coord = pixelSampler.SampleCoordinates[i];
            bool hasColor = color.HasValue && (color.Value.X > 0.01f || color.Value.Y > 0.01f);
            
            yield return new()
            {
                TestName = $"{nameof(ImagePlacementFillCenterTest)}: FillCenter mode {pixelDescriptions[i]} has color",
                Description = $"Sampled at pixel ({coord.X}, {coord.Y})",
                ExpectedResult = "has color (center portion visible)",
                ActualResult = color.HasValue ? PixelAssertions.DescribeColor(color.Value) : "null",
                Passed = hasColor
            };
        }
    }
}
