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
/// Tests LinearGradientBackground with horizontal gradient.
/// Validates that gradient shaders and UBO/descriptor system work correctly.
/// 
/// Single frame test: Red (left) → Green (right) at angle 0°
/// </summary>
public class BackgroundLayerHorizGradientTest(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    private int framesRendered = 0;
        private readonly int frameCount = 1;

    // Red to Green horizontal gradient
    private static readonly GradientDefinition gradient = GradientDefinition.TwoColor(
        Colors.Red,    // Position 0.0 (left)
        Colors.Green   // Position 1.0 (right)
    );

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

        CreateChild(new LinearGradientBackground.Template()
        {
            Gradient = gradient,
            Angle = 0f  // Horizontal (0 radians)
        });

        var window = windowService.GetWindow();
        int offset = 2;

        // Sample left edge, center, right edge (horizontal line through middle)
        pixelSampler.SampleCoordinates = [
            new(offset, window.Size.Y / 2),                      // Left edge → Red
            new(window.Size.X / 2, window.Size.Y / 2),          // Center → blend
            new(window.Size.X - offset, window.Size.Y / 2),     // Right edge → Green
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

        // Expected: Horizontal gradient Red (left) → Green (right)
        var expected = new[] {
            Colors.Red,                                 // Left: Red
            LerpColor(Colors.Red, Colors.Green, 0.5f), // Center: 50% blend
            Colors.Green                                // Right: Green
        };
        
        for (int i = 0; i < samples[0].Length; i++)
        {
            yield return new()
            {
                TestName = $"Horizontal gradient Pixel[{i}] color check",
                ExpectedResult = PixelAssertions.DescribeColor(expected[i]),
                ActualResult = PixelAssertions.DescribeColor(samples[0][i]),
                Passed = PixelAssertions.ColorsMatch(samples[0][i], expected[i], tolerance: 0.05f)
            };
        }
    }
}