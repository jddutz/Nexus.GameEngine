using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Components;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;

namespace TestApp.TestComponents;

/// <summary>
/// Tests BackgroundLayer with UniformColor mode.
/// Single frame test: verifies uniform blue color across all sample points.
/// </summary>
public class BackgroundLayerUniformColorTest(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    private bool rendered = false;

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        CreateChild(new BackgroundLayer.Template()
        {
            Mode = BackgroundLayerModeEnum.UniformColor,
            UniformColor = Colors.Blue
        });

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

        for(int i=0; i<samples[0].Length; i++)
        {
            yield return new()
            {
                TestName = $"Uniform color Pixel[{i}] color check",
                ExpectedResult = PixelAssertions.DescribeColor(Colors.Blue),
                ActualResult = PixelAssertions.DescribeColor(samples[0][i]),
                Passed = PixelAssertions.ColorsMatch(samples[0][i], Colors.Blue)
            };
        }
    }
}