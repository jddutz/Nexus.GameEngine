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
/// Tests ImageTextureBackground with Stretch placement.
/// Validates that image stretches non-uniformly to fill viewport (may distort aspect ratio).
/// 
/// Uses image_test.png (256x256): R channel = X coordinate (0-255), G channel = Y coordinate (0-255)
/// Expected: Entire texture visible (UV 0,0 to 1,1), potentially distorted to fill viewport
/// </summary>
public class ImagePlacementStretchTest(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    private int framesRendered = 0;
    private readonly int frameCount = 1;

    private const int ImageSize = 256;

    // Helper to convert UV coordinates to expected RGB color from image_test.png
    // UV coordinates are [0,1], image pixels are [0,255]
    private static Vector4D<float> UVToExpectedColor(float u, float v)
    {
        // R channel = X coordinate (0-255) normalized to [0,1]
        // G channel = Y coordinate (0-255) normalized to [0,1]
        var x = u * 255f;
        var y = v * 255f;
        return new Vector4D<float>(x / 255f, y / 255f, 0f, 1f);
    }

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        CreateChild(new ImageTextureBackground.Template()
        {
            TextureDefinition = TestResources.ImageTestTexture,
            Placement = BackgroundImagePlacement.Stretch
        });

        var window = windowService.GetWindow();
        int offset = 5;

        // Sample corners and center to verify entire texture is visible (stretched to fit)
        // Stretch mode: UV [0,0] to [1,1] maps to entire viewport
        pixelSampler.SampleCoordinates = [
            new(offset, offset),                                    // Top-left corner -> UV(0,0) -> RGB(0,0,0)
            new(window.Size.X - offset, offset),                    // Top-right corner -> UV(1,0) -> RGB(255,0,0)
            new(offset, window.Size.Y - offset),                    // Bottom-left corner -> UV(0,1) -> RGB(0,255,0)
            new(window.Size.X - offset, window.Size.Y - offset),    // Bottom-right corner -> UV(1,1) -> RGB(255,255,0)
            new(window.Size.X / 2, window.Size.Y / 2),              // Center -> UV(0.5,0.5) -> RGB(127,127,0)
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
            TestName = $"{nameof(ImagePlacementStretchTest)}: Sampled output should not be null",
            ExpectedResult = "not null",
            ActualResult = samples == null ? "null" : samples.Length.ToString(),
            Passed = samples != null && samples.Length > 0
        };

        if (samples == null || samples.Length == 0) yield break;

        // Stretch mode shows entire texture, UV coordinates [0,0] to [1,1]
        var expectedColors = new[] {
            UVToExpectedColor(0f, 0f),      // Top-left
            UVToExpectedColor(1f, 0f),      // Top-right
            UVToExpectedColor(0f, 1f),      // Bottom-left
            UVToExpectedColor(1f, 1f),      // Bottom-right
            UVToExpectedColor(0.5f, 0.5f),  // Center
        };
        
        var pixelDescriptions = new[] { "Top-left", "Top-right", "Bottom-left", "Bottom-right", "Center" };
        
        for (int i = 0; i < samples[0].Length; i++)
        {
            var color = samples[0][i];
            var coord = pixelSampler.SampleCoordinates[i];
            
            yield return new()
            {
                TestName = $"{nameof(ImagePlacementStretchTest)}: Stretch mode {pixelDescriptions[i]} color",
                Description = $"Sampled at pixel ({coord.X}, {coord.Y})",
                ExpectedResult = PixelAssertions.DescribeColor(expectedColors[i]),
                ActualResult = color.HasValue ? PixelAssertions.DescribeColor(color.Value) : "null",
                Passed = color.HasValue && PixelAssertions.ColorsMatch(color.Value, expectedColors[i], tolerance: 0.02f)
            };
        }
    }
}
