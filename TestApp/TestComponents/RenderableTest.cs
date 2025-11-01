using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents;

/// <summary>
/// Service responsible for discovering integration tests in assemblies.
/// </summary>
public partial class RenderableTest(
    IPixelSampler pixelSampler
    ) : TestComponent, IDrawable
{
    [Test("GetDrawCommands() should be called at least once")]
    public readonly static RenderableTestTemplate RenderableBaseTest = new();

    [ComponentProperty]
    private Vector2D<int>[] _sampleCoordinates = [];

    [ComponentProperty]
    private Dictionary<int, Vector4D<float>[]> _expectedResults = [];

    [ComponentProperty]
    private uint _renderPriority = 0;
    
    public bool IsVisible() => true;

    public int FramesRendered { get; private set; } = 0;

    protected override void OnActivate()
    {
        pixelSampler.SampleCoordinates = SampleCoordinates;
        pixelSampler.Enabled = SampleCoordinates.Length > 0;
        pixelSampler.Activate();
    }

    public virtual IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        FramesRendered++;
        yield break;
    }

    protected override void OnDeactivate()
    {
        pixelSampler.Deactivate();
    }

    public override IEnumerable<TestResult> GetTestResults()
    {
        yield return new()
        {
            ExpectedResult = $"FramesRendered >= {FrameCount}",
            ActualResult = $"FramesRendered: {FramesRendered}",
            Passed = FramesRendered >= FrameCount
        };

        if (ExpectedResults.Count == 0) yield break;

        var samples = pixelSampler.GetResults();

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
