using System.Dynamic;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents;

/// <summary>
/// Service responsible for discovering integration tests in assemblies.
/// </summary>
public partial class RenderableTest(
    IPixelSampler pixelSampler,
    IRenderer renderer
    ) : TestComponent
{
    [Test("GetDrawCommands() should be called at least once")]
    public readonly static RenderableTestTemplate RenderableBaseTest = new()
    {
        SampleCoordinates = [
            new(960, 540),  // Center
            new(480, 270),  // Upper-left quadrant
            new(1440, 270), // Upper-right quadrant
            new(480, 810),  // Lower-left quadrant
            new(1440, 810)  // Lower-right quadrant
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            // DarkBlue background at all sample points
            [0] = [
                Colors.DarkBlue,  
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue
            ]
        }
    };

    [ComponentProperty]
    [TemplateProperty]
    private Vector2D<int>[] _sampleCoordinates = [];

    [ComponentProperty]
    [TemplateProperty]
    private Dictionary<int, Vector4D<float>[]> _expectedResults = [];

    [ComponentProperty]
    [TemplateProperty]
    private uint _renderPriority = 0;

    public uint FramesRendered { get; private set; }

    // Only visible while test is active
    public bool IsVisible() => IsActive();

    protected override void OnActivate()
    {
        base.OnActivate();

        renderer.AfterRendering += OnRenderComplete;
        
        pixelSampler.SampleCoordinates = SampleCoordinates;
        pixelSampler.Enabled = SampleCoordinates.Length > 0;
        pixelSampler.Activate();
        
        // Explicitly activate all children
        foreach (var child in Children.OfType<IRuntimeComponent>())
        {
            child.Activate();
        }
    }

    protected override void OnUpdate(double deltaTime)
    {
        // Deactivate one frame after FrameCount, 
        // to allow the last frame to be fully executed
        if (FramesRendered > FrameCount) Deactivate();
    }

    private void OnRenderComplete(object? sender, RenderEventArgs e)
    {
        FramesRendered++;
    }

    protected override void OnDeactivate()
    {
        pixelSampler.Deactivate();
        
        base.OnDeactivate();
    }

    public override IEnumerable<TestResult> GetTestResults()
    {
        if (ExpectedResults.Count == 0)
        {
            yield return new TestResult
            {
                ExpectedResult = "Pixel sampling configured with expected results",
                ActualResult = "No expected results configured",
                Passed = false
            };

            yield break;
        }

        if (pixelSampler == null)
        {
            yield return new TestResult
            {
                ExpectedResult = "Pixel sampler is properly configured",
                ActualResult = "Pixel sampler is null",
                Passed = false
            };

            yield break;
        }

        if (pixelSampler.SampleCoordinates.Length == 0)
        {
            yield return new TestResult
            {
                ExpectedResult = "Pixel sampler is properly configured",
                ActualResult = "Sample coordinates == []",
                Passed = false
            };

            yield break;
        }

        var samples = pixelSampler.GetResults();

        yield return new()
        {
            ExpectedResult = $"FramesRendered >= {FrameCount}",
            ActualResult = $"FramesRendered: {samples.Length}",
            Passed = samples.Length >= FrameCount
        };


        if (samples == null)
        {
            yield return new TestResult
            {
                ExpectedResult = "Sampled output is not null",
                ActualResult = "null",
                Passed = false
            };

            yield break;
        }

        if (samples.Length == 0)
        {
            yield return new TestResult
            {
                ExpectedResult = "Sampled output is not empty",
                ActualResult = "[]",
                Passed = false
            };

            yield break;
        }

        for (int i = 0; i < samples.Length; i++)
        {
            if (!ExpectedResults.TryGetValue(i, out Vector4D<float>[]? expected)) continue;
            if (expected == null) continue;

            for (int x = 0; x < expected.Length; x++)
            {
                yield return new()
                {
                    ExpectedResult = $"({SampleCoordinates[x].X}, {SampleCoordinates[x].Y}) {PixelAssertions.DescribeColor(expected[x])}",
                    ActualResult = PixelAssertions.DescribeColor(samples[i][x]),
                    Passed = PixelAssertions.ColorsMatch(samples[i][x], expected[x], tolerance: 0.05f)
                };
            }
        }
    }
}
