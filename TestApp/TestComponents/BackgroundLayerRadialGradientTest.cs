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

namespace TestApp.TestComponents;

/// <summary>
/// Tests BackgroundLayer with RadialGradient mode.
/// Validates that radial gradient shaders work correctly.
/// 
/// Single frame test: White (center) → Black (edges) from center point (0.5, 0.5)
/// </summary>
public class BackgroundLayerRadialGradientTest(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    private bool rendered = false;

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

        CreateChild(new BackgroundLayer.Template()
        {
            Mode = BackgroundLayerModeEnum.RadialGradient,
            RadialGradientDefinition = gradient,
            RadialGradientCenter = new Vector2D<float>(0.5f, 0.5f),  // Center of screen
            RadialGradientRadius = 0.5f  // Radius that reaches edges
        });

        var window = windowService.GetWindow();
        int offset = 2;

        // Sample center, midpoint, and edge
        // For a radial gradient centered at (0.5, 0.5) with radius 0.5:
        // - Center (0.5, 0.5) → distance 0.0 → White
        // - Midpoint (0.75, 0.5) → distance ~0.25 → blend
        // - Edge (1.0, 0.5) → distance ~0.5 → Black
        pixelSampler.SampleCoordinates = [
            new(window.Size.X / 2, window.Size.Y / 2),           // Center → White
            new(window.Size.X * 3 / 4, window.Size.Y / 2),       // Midpoint → blend
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
        if (rendered)
        {
            pixelSampler.Deactivate();
            Deactivate();
        }
        rendered = true;
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
        // Distance calculations: center is at 0.5, samples at 0.5, 0.75, 1.0
        // Normalized distances: 0.0, 0.5, 1.0
        var expected = new[] {
            Colors.White,                                  // Center: distance 0.0
            LerpColor(Colors.White, Colors.Black, 0.5f),  // Midpoint: distance 0.5
            Colors.Black                                   // Edge: distance 1.0
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