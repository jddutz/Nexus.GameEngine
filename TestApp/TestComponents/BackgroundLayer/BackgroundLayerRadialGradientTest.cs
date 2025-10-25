using Nexus.GameEngine.Components;
using Nexus.GameEngine.Data;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Components;
using Nexus.GameEngine.Input;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;
using System.Numerics;

namespace TestApp.TestComponents.BackgroundLayer;

/// <summary>
/// Tests RadialGradientBackground component.
/// Validates that radial gradient shaders work correctly.
/// 
/// Single frame test: White (center) → Black (edges) from center point (0.5, 0.5)
/// </summary>
public partial class BackgroundLayerRadialGradientTest(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    private int framesRendered = 0;
    private readonly int frameCount = 1;

    // White to Black radial gradient
    private static readonly GradientDefinition gradient = GradientDefinition.TwoColor(
        Colors.White,  // Position 0.0 (center)
        Colors.Black   // Position 1.0 (edges)
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

        CreateChild(new RadialGradientBackground.Template()
        {
            Gradient = gradient,
            Center = new Vector2D<float>(0.5f, 0.5f),  // Center of screen in normalized [0,1] coordinates
            Radius = 0.5f  // Radius that reaches edges
        });

        var window = windowService.GetWindow();
        int offset = 2;

        // Sample center, quarter-way, and edge along horizontal line
        // With aspect-corrected circular gradient on 1920x1080 (aspect ~1.78):
        // - Center (0.5, 0.5) → distance 0.0 → White (t=0.0)
        // - Quarter (0.625, 0.5) → scaled distance ~0.22 → light blend (t=~0.44)
        // - Edge (1.0, 0.5) → scaled distance ~0.89 → very dark (t=~1.78, clamped to 1.0)
        pixelSampler.SampleCoordinates = [
            new(window.Size.X / 2, window.Size.Y / 2),           // Center → White
            new(window.Size.X * 5 / 8, window.Size.Y / 2),       // Quarter-way → light gray
            new(window.Size.X - offset, window.Size.Y / 2),      // Right edge → Black
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

        // Expected: Radial gradient White (center) → Black (edges)
        // With 1920x1080 aspect ratio (~1.78):
        // - Center (0.5, 0.5): distance = 0, t = 0.0 → White
        // - Quarter (0.625, 0.5): distance = 0.125 * 1.78 = 0.22, t = 0.22/0.5 = 0.44 → lighter gray
        // - Right edge (1.0, 0.5): distance = 0.5 * 1.78 = 0.89, t = 0.89/0.5 = 1.78 (clamped to 1.0) → Black
        var expected = new[] {
            Colors.White,                                  // Center: t = 0.0
            LerpColor(Colors.White, Colors.Black, 0.44f),  // Quarter: t ≈ 0.44
            Colors.Black                                   // Edge: t = 1.0 (clamped)
        };
        
        for (int i = 0; i < samples[0].Length; i++)
        {
            yield return new()
            {
                TestName = $"Radial gradient Pixel[{i}] color check",
                ExpectedResult = PixelAssertions.DescribeColor(expected[i]),
                ActualResult = PixelAssertions.DescribeColor(samples[0][i]),
                Passed = PixelAssertions.ColorsMatch(samples[0][i], expected[i], tolerance: 0.05f)
            };
        }
    }
}