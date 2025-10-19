using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Components;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents;

/// <summary>
/// Tests BackgroundLayer with PerVertexColor mode.
/// 
/// VERTEX ORDER SPECIFICATION (per ColorQuad standard):
///   Index 0 → Top-Left     (Screen: offset, offset)
///   Index 1 → Bottom-Left  (Screen: offset, height-offset)
///   Index 2 → Top-Right    (Screen: width-offset, offset)
///   Index 3 → Bottom-Right (Screen: width-offset, height-offset)
/// 
/// Initial colors: TL=Blue, BL=Green, TR=Red, BR=Yellow
/// Animates to: TL=Yellow, BL=Red, TR=Green, BR=Blue (clockwise rotation)
/// </summary>
public class BackgroundLayerPerVertexColorTest(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    private int framesRendered = 0;
    private System.Diagnostics.Stopwatch? stopwatch;
    private BackgroundLayer? bg;
    private int finalFrame = -1;

    // Initial vertex colors: TL=Blue, BL=Green, TR=Red, BR=Yellow
    private static readonly Vector4D<float>[] initialColors = [
        Colors.Blue,    // Index 0 - Top-Left
        Colors.Green,   // Index 1 - Bottom-Left
        Colors.Red,     // Index 2 - Top-Right
        Colors.Yellow   // Index 3 - Bottom-Right
    ];

    // Final vertex colors (clockwise rotation): TL=Yellow, BL=Red, TR=Green, BR=Blue
    private static readonly Vector4D<float>[] finalColors = [
        Colors.Yellow,  // Index 0 - Top-Left (was BR)
        Colors.Red,     // Index 1 - Bottom-Left (was TR)
        Colors.Green,   // Index 2 - Top-Right (was BL)
        Colors.Blue     // Index 3 - Bottom-Right (was TL)
    ];

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        bg = CreateChild(new BackgroundLayer.Template()
        {
            Mode = BackgroundLayerModeEnum.PerVertexColor,
            VertexColors = initialColors
        }) as BackgroundLayer ?? throw new InvalidOperationException("BackgroundLayer component is null");

        bg.AnimationEnded += (sender, e) =>
        {
            // Capture one more frame after animation completes to render the final values
            finalFrame = framesRendered + 1;
        };

        var window = windowService.GetWindow();

        int offset = 2;

        pixelSampler.SampleCoordinates = [
            new(offset, offset),                                  // Top-left corner
            new(offset, window.Size.Y - offset),                  // Bottom-left corner
            new(window.Size.X - offset, offset),                  // Top-right corner
            new(window.Size.X - offset, window.Size.Y - offset),  // Bottom-right corner
        ];
        
        pixelSampler.Enabled = true;
    }

    protected override void OnActivate()
    {
        stopwatch = System.Diagnostics.Stopwatch.StartNew();
        pixelSampler.Activate();  // Start capturing frames
    }

    protected override void OnUpdate(double deltaTime)
    {
        if (framesRendered == 0 && bg != null)
        {
            bg.VertexColors = finalColors;
        }

        // Check if we've reached the final frame (animation complete + 1 frame rendered)
        if (finalFrame >= 0 && framesRendered >= finalFrame)
        {
            pixelSampler.Deactivate();
            Deactivate();
        }

        framesRendered++;
    }
    
    protected void OnDeactivated()
    {
        stopwatch?.Stop();
    }

    public IEnumerable<TestResult> GetTestResults()
    {
        var expectedTestDuration = 0.4d;
        var timeElapsedSeconds = stopwatch?.Elapsed.TotalSeconds ?? 0d;
        var fps = framesRendered / timeElapsedSeconds;

        yield return new TestResult
        {
            TestName = $"Test duration should be longer than the animation",
            ExpectedResult = $"~{expectedTestDuration:F1}s",
            ActualResult = $"{timeElapsedSeconds:F3}s ({framesRendered} frames, {fps:F0} fps)",
            Passed = timeElapsedSeconds >= expectedTestDuration
        };
        
        var samples = pixelSampler.GetResults();
        var framesSampled = samples?.Length ?? 0;

        yield return new TestResult
        {
            TestName = $"Sampled output should not be null",
            ExpectedResult = "not null",
            ActualResult = samples == null ? "null" : samples.Length.ToString(),
            Passed = samples != null && samples.Length > 0
        };

        if (samples == null || samples.Length == 0) yield break;

        // Check initial frame colors (frame 0)
        for(int i=0; i<samples[0].Length; i++)
        {
            yield return new()
            {
                TestName = $"Frame[0] Pixel[{i}] color check",
                ExpectedResult = PixelAssertions.DescribeColor(initialColors[i]),
                ActualResult = PixelAssertions.DescribeColor(samples[0][i]),
                Passed = PixelAssertions.ColorsMatch(samples[0][i], initialColors[i])
            };
        }

        // Check final frame colors (frame when animation completed)
        if (finalFrame >= 0 && finalFrame < samples.Length)
        {
            for (int i = 0; i < samples[finalFrame].Length; i++)
            {
                yield return new()
                {
                    TestName = $"Frame[{finalFrame}] Pixel[{i}] color check",
                    ExpectedResult = PixelAssertions.DescribeColor(finalColors[i]),
                    ActualResult = PixelAssertions.DescribeColor(samples[finalFrame][i]),
                    Passed = PixelAssertions.ColorsMatch(samples[finalFrame][i], finalColors[i])
                };
            }
        }
    }
}