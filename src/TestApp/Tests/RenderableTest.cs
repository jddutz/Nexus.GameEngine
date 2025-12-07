using Nexus.GameEngine;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Runtime.Extensions;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace TestApp.Tests;

/// <summary>
/// Service responsible for discovering integration tests in assemblies.
/// </summary>
public partial class RenderableTest(
    IPixelSampler pixelSampler,
    IRenderer renderer)
    : TestComponent
{
    protected IRenderer Renderer => renderer;

    [ComponentProperty]
    [TemplateProperty]
    private uint _renderPriority = 0;

    public uint FramesRendered { get; private set; }

    // Only visible while test is active
    public bool IsVisible()
    {
        var visible = IsActive();
        Log.Debug($"[{GetType().Name}] IsVisible() => {visible} (IsActive={IsActive()})");
        return visible;
    }

    /// <summary>
    /// Virtual method to allow derived tests to calculate sample coordinates dynamically.
    /// Override this to compute coordinates based on window size, font metrics, etc.
    /// </summary>
    /// <returns>Array of screen coordinates to sample.</returns>
    private Vector2D<int>[] _sampleCoordinates = [];
    protected virtual Vector2D<int>[] GetSampleCoordinates()
    {
        int width = Window.GetWindow().FramebufferSize.X;
        int height = Window.GetWindow().FramebufferSize.Y;
        
        return [
            new(width / 2, height / 2),           // Center
            new(width / 4, height / 4),           // Upper-left quadrant
            new(width * 3 / 4, height / 4),       // Upper-right quadrant
            new(width / 4, height * 3 / 4),       // Lower-left quadrant
            new(width * 3 / 4, height * 3 / 4)    // Lower-right quadrant
        ];
    }

    /// <summary>
    /// Virtual method to allow derived tests to calculate expected results dynamically.
    /// Override this to compute expected values based on runtime calculations.
    /// </summary>
    /// <returns>Dictionary mapping frame indices to expected color arrays.</returns>
    protected virtual Dictionary<int, Vector4D<float>[]> GetExpectedResults()
    {
        return new Dictionary<int, Vector4D<float>[]>()
        {
            // DarkBlue background at all sample points
            [0] = [
                Colors.DarkBlue,  
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue,
                Colors.DarkBlue
            ]
        };
    }
    private Dictionary<int, Vector4D<float>[]> _expectedResults = new Dictionary<int, Vector4D<float>[]>();

    public virtual IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        Log.Debug($"[{GetType().Name}] GetDrawCommands called (IsActive={IsActive()})");
        
        // Base RenderableTest doesn't render anything - it's just a container for child elements
        // Derived tests should override this if they need to render directly
        return [];
    }

    protected override void OnActivate()
    {
        base.OnActivate();
        
        renderer.AfterRendering += OnRenderComplete;
        
        // Defer coordinate calculation until after children are fully activated
        // (needed for tests that measure child elements like TextElement)
        this.Activated += OnActivatedComplete;
    }

    private void OnActivatedComplete(object? sender, EventArgs e)
    {
        // Unsubscribe immediately - only need this once
        this.Activated -= OnActivatedComplete;
        
        // Now children are fully activated and we can measure them
        _sampleCoordinates = GetSampleCoordinates();
        _expectedResults = GetExpectedResults();
        
        pixelSampler.SampleCoordinates = _sampleCoordinates;
        pixelSampler.Enabled = _sampleCoordinates.Length > 0;
        pixelSampler.Activate();
    }

    protected override void OnUpdate(double deltaTime)
    {        
        Log.Info($"[{GetType().Name}] OnUpdate called (FramesRendered={FramesRendered}/{FrameCount})");
        
        // Deactivate when we've reached the target frame count
        // Note: Visibility is checked at START of Update (before this runs), so the current frame
        // will still render even after we deactivate here. This is expected behavior.
        if (FramesRendered >= FrameCount)
        {
            Log.Info($"[{GetType().Name}] Test complete after {FramesRendered} frames, deactivating");
            Deactivate();
        }
    }

    private void OnRenderComplete(object? sender, RenderEventArgs e)
    {
        FramesRendered++;
        Log.Debug($"[{GetType().Name}] Frame {FramesRendered} rendered");
    }

    protected override void OnDeactivate()
    {
        Log.Info($"[{GetType().Name}] OnDeactivate called");
        
        renderer.AfterRendering -= OnRenderComplete;
        pixelSampler.Deactivate();
        
        base.OnDeactivate();
    }

    public override IEnumerable<TestResult> GetTestResults()
    {
        if (_expectedResults.Count == 0)
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

        if (_sampleCoordinates.Length == 0)
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
            if (!_expectedResults.TryGetValue(i, out Vector4D<float>[]? expected)) continue;
            if (expected == null) continue;

            // Verify samples[i] has data
            if (samples[i] == null || samples[i].Length == 0)
            {
                yield return new TestResult
                {
                    ExpectedResult = $"Frame {i} has {expected.Length} samples",
                    ActualResult = $"Frame {i} has no sample data",
                    Passed = false
                };
                continue;
            }

            // Check if sample count matches expected count
            if (samples[i].Length != expected.Length)
            {
                yield return new TestResult
                {
                    ExpectedResult = $"Frame {i} has {expected.Length} samples",
                    ActualResult = $"Frame {i} has {samples[i].Length} samples",
                    Passed = false
                };
                continue;
            }

            for (int x = 0; x < expected.Length; x++)
            {
                // Extra safety check (should never happen given above check, but prevents crashes)
                if (x >= samples[i].Length)
                {
                    yield return new TestResult
                    {
                        ExpectedResult = $"Frame {i} sample {x} exists",
                        ActualResult = $"Sample index {x} out of bounds (length: {samples[i].Length})",
                        Passed = false
                    };
                    continue;
                }

                if (x >= _sampleCoordinates.Length)
                {
                    yield return new TestResult
                    {
                        ExpectedResult = $"Frame {i} coordinate {x} exists",
                        ActualResult = $"Coordinate index {x} out of bounds (length: {_sampleCoordinates.Length})",
                        Passed = false
                    };
                    continue;
                }

                yield return new()
                {
                    ExpectedResult = $"Frame {i} ({_sampleCoordinates[x].X}, {_sampleCoordinates[x].Y}) {PixelAssertions.DescribeColor(expected[x])}",
                    ActualResult = PixelAssertions.DescribeColor(samples[i][x]),
                    Passed = PixelAssertions.ColorsMatch(samples[i][x], expected[x], tolerance: 0.05f)
                };
            }
        }
    }
}
