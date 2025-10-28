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
    public new record Template : TestComponent.Template { }

    [Test("GetDrawCommands() should be called at least once")]
    public readonly static Template RenderableBaseTest = new();

    public virtual Vector2D<int>[] SampleCoordinates { get; set; } = [];
    public virtual Dictionary<int, Vector4D<float>[]> ExpectedResults { get; set; } = [];

    public uint RenderPriority { get; set; } = 0;
    
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

        for (int i = 0; i < samples[0].Length; i++)
        {
            if (!ExpectedResults.TryGetValue(i, out Vector4D<float>[]? expected)) continue;
            if (expected == null) continue;

            if (expected.Length > i)
            {
                yield return new()
                {
                    ExpectedResult = $"Pixel[{i}] {PixelAssertions.DescribeColor(expected[i])}",
                    ActualResult = PixelAssertions.DescribeColor(samples[0][i]),
                    Passed = PixelAssertions.ColorsMatch(samples[0][i], expected[i], tolerance: 0.05f)
                };
            }
        }
    }
}