using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Components;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents;

/// <summary>
/// Tests LinearGradientBackground with vertical gradient.
/// Validates that gradient shaders work correctly with angle = 90°.
/// 
/// Single frame test: Blue (top) → Yellow (bottom) at angle 90°
/// </summary>
public class BackgroundLayerVertGradientTest(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    private int framesRendered = 0;
    private readonly int frameCount = 1;

    // Blue to Yellow vertical gradient
    private static readonly GradientDefinition gradient = GradientDefinition.TwoColor(
        Colors.Blue,   // Position 0.0 (top when angle = 90°)
        Colors.Yellow  // Position 1.0 (bottom when angle = 90°)
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
            Angle = MathF.PI / 2f  // Vertical (90 degrees = π/2 radians)
        });

        var window = windowService.GetWindow();
        int offset = 2;

        // Sample top edge, center, bottom edge (vertical line through middle)
        pixelSampler.SampleCoordinates = [
            new(window.Size.X / 2, offset),                      // Top edge → Blue
            new(window.Size.X / 2, window.Size.Y / 2),          // Center → blend
            new(window.Size.X / 2, window.Size.Y - offset),     // Bottom edge → Yellow
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

        // Expected: Vertical gradient Blue (top) → Yellow (bottom)
        var expected = new[] {
            Colors.Blue,                                  // Top: Blue
            LerpColor(Colors.Blue, Colors.Yellow, 0.5f), // Center: 50% blend
            Colors.Yellow                                 // Bottom: Yellow
        };
        
        for (int i = 0; i < samples[0].Length; i++)
        {
            yield return new()
            {
                TestName = $"Vertical gradient Pixel[{i}] color check",
                ExpectedResult = PixelAssertions.DescribeColor(expected[i]),
                ActualResult = PixelAssertions.DescribeColor(samples[0][i]),
                Passed = PixelAssertions.ColorsMatch(samples[0][i], expected[i], tolerance: 0.05f)
            };
        }
    }
}