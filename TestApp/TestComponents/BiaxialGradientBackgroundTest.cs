using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI.Components;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents;

/// <summary>
/// Tests BiaxialGradientBackground with 4-corner color interpolation.
/// Validates bilinear interpolation across the screen.
/// 
/// Single frame test: 
/// - Top-left: Red
/// - Top-right: Green
/// - Bottom-left: Blue
/// - Bottom-right: Yellow
/// </summary>
public partial class BiaxialGradientBackgroundTest(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    private int framesRendered = 0;
    private readonly int frameCount = 1;

    // Helper to linearly interpolate between two colors
    private static Vector4D<float> LerpColor(Vector4D<float> a, Vector4D<float> b, float t)
    {
        return new Vector4D<float>(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t,
            a.W + (b.W - a.W) * t
        );
    }

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        // Create biaxial gradient with 4 corner colors
        CreateChild(new BiaxialGradientBackground.Template()
        {
            TopLeft = Colors.Red,
            TopRight = Colors.Green,
            BottomLeft = Colors.Blue,
            BottomRight = Colors.Yellow
        });

        var window = windowService.GetWindow();
        int offset = 2;

        // Sample the 4 corners and center point
        pixelSampler.SampleCoordinates = [
            new(offset, offset),                                  // Top-left → Red
            new(window.Size.X - offset, offset),                  // Top-right → Green
            new(offset, window.Size.Y - offset),                  // Bottom-left → Blue
            new(window.Size.X - offset, window.Size.Y - offset),  // Bottom-right → Yellow
            new(window.Size.X / 2, window.Size.Y / 2),           // Center → blend of all 4
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
            TestName = $"Sampled output should not be null",
            ExpectedResult = "not null",
            ActualResult = samples == null ? "null" : samples.Length.ToString(),
            Passed = samples != null && samples.Length > 0
        };

        if (samples == null || samples.Length == 0) yield break;

        // Expected colors:
        // Corners should be exact, center should be average of all 4
        var topEdge = LerpColor(Colors.Red, Colors.Green, 0.5f);      // Top center
        var bottomEdge = LerpColor(Colors.Blue, Colors.Yellow, 0.5f); // Bottom center
        var center = LerpColor(topEdge, bottomEdge, 0.5f);             // True center
        
        var expected = new[] {
            Colors.Red,     // Top-left corner
            Colors.Green,   // Top-right corner
            Colors.Blue,    // Bottom-left corner
            Colors.Yellow,  // Bottom-right corner
            center          // Center point (blend of all 4)
        };
        
        var descriptions = new[] {
            "Top-left (Red)",
            "Top-right (Green)",
            "Bottom-left (Blue)",
            "Bottom-right (Yellow)",
            "Center (blended)"
        };
        
        for (int i = 0; i < samples[0].Length; i++)
        {
            yield return new()
            {
                TestName = $"Biaxial gradient Pixel[{i}] {descriptions[i]} color check",
                ExpectedResult = PixelAssertions.DescribeColor(expected[i]),
                ActualResult = PixelAssertions.DescribeColor(samples[0][i]),
                Passed = PixelAssertions.ColorsMatch(samples[0][i], expected[i], tolerance: 0.05f)
            };
        }
    }
}
