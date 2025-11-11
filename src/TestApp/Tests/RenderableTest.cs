using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace TestApp.Tests;

/// <summary>
/// Service responsible for discovering integration tests in assemblies.
/// </summary>
public partial class RenderableTest(
    IPixelSampler pixelSampler,
    IRenderer renderer,
    IWindowService windowService)
    : TestComponent
{
    protected IRenderer Renderer => renderer;
    protected IWindow Window => windowService.GetWindow();

    /// <summary>
    /// Converts top-left origin coordinates to centered coordinate system.
    /// For a 1280x720 window:
    /// - (0, 0) in top-left → (-640, -360) in centered
    /// - (100, 100) in top-left → (-540, -260) in centered
    /// - (640, 360) in top-left → (0, 0) in centered (center of screen)
    /// </summary>
    protected Vector3D<float> ToCenteredPosition(float x, float y, float z = 0)
    {
        var halfWidth = Window.Size.X / 2f;
        var halfHeight = Window.Size.Y / 2f;
        return new Vector3D<float>(x - halfWidth, y - halfHeight, z);
    }

    /// <summary>
    /// Static version of ToCenteredPosition for use in template initialization.
    /// Assumes standard test window size of 1280x720.
    /// </summary>
    protected static Vector3D<float> ToCenteredPosition(float x, float y, float z, int windowWidth, int windowHeight)
    {
        var halfWidth = windowWidth / 2f;
        var halfHeight = windowHeight / 2f;
        return new Vector3D<float>(x - halfWidth, y - halfHeight, z);
    }

    /// <summary>
    /// Static version assuming 1280x720 window for template initialization.
    /// </summary>
    protected static Vector3D<float> ToCenteredPositionDefault(float x, float y, float z = 0)
    {
        return ToCenteredPosition(x, y, z, 1280, 720);
    }

    [Test("GetDrawCommands() should be called at least once")]
    public readonly static RenderableTestTemplate RenderableBaseTest = new();

    [ComponentProperty]
    [TemplateProperty]
    private uint _renderPriority = 0;

    public uint FramesRendered { get; private set; }

    // Only visible while test is active
    public bool IsVisible() => IsActive();

    /// <summary>
    /// Virtual method to allow derived tests to calculate sample coordinates dynamically.
    /// Override this to compute coordinates based on window size, font metrics, etc.
    /// </summary>
    /// <returns>Array of screen coordinates to sample.</returns>
    private Vector2D<int>[] _sampleCoordinates = [];
    protected virtual Vector2D<int>[] GetSampleCoordinates()
    {
        int width = Window.FramebufferSize.X;
        int height = Window.FramebufferSize.Y;
        
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
