using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Components;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;

namespace TestApp.TestComponents;

/// <summary>
/// </summary>
public class BackgroundLayerUniformColorTest(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    private int framesRendered = 0;
    private System.Diagnostics.Stopwatch? stopwatch;
    private BackgroundLayer? bg;
    private int finalFrame = -1;

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        bg = CreateChild(new BackgroundLayer.Template()
        {
            Mode = BackgroundLayerModeEnum.UniformColor,
            UniformColor = Colors.Blue
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
            bg.UniformColor = Colors.Green;
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

        for(int i=0; i<samples[0].Length; i++)
        {
            yield return new()
            {
                TestName = $"Frame[0] Pixel[{i}] color check",
                ExpectedResult = PixelAssertions.DescribeColor(Colors.Blue),
                ActualResult = PixelAssertions.DescribeColor(samples[0][i]),
                Passed = PixelAssertions.ColorsMatch(samples[0][i], Colors.Blue)
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
                    ExpectedResult = PixelAssertions.DescribeColor(Colors.Green),
                    ActualResult = PixelAssertions.DescribeColor(samples[finalFrame][i]),
                    Passed = PixelAssertions.ColorsMatch(samples[finalFrame][i], Colors.Green)
                };
            }
        }
    }
}