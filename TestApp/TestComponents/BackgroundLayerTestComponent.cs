using System.Data;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI.Components;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace TestApp.TestComponents;

/// <summary>
/// Test component that creates a BackgroundLayer child to validate component lifecycle
/// and child component rendering.
/// </summary>
public class BackgroundLayerTestComponent(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    private IWindow window = windowService.GetWindow();

    public int FrameCount { get; set; } = 1200;
    public int FramesRendered { get; private set; } = 0;

    private System.Diagnostics.Stopwatch? _fpsStopwatch;
    public double TargetFPS { get; set; } = 30;
    public double MeasuredFPS { get; private set; } = 0;

    private const int offset = 2;
    private Vector2D<int>[] sampleCoords = [];
    private Vector4D<float>[] startPixels = [];  // Frame 0 colors
    private Vector4D<float>[] endPixels = [];    // Frame 40 colors
    private Vector4D<float>?[][] sampledPixelSets = [];

    private float tolerance = 0.05f;  // Increased tolerance for edge sampling with bilinear filtering
    private bool _testComplete = false;

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);
        
        // Create BackgroundLayer as a child component
        var background = CreateChild(typeof(BackgroundLayer));
    }

    protected override void OnActivate()
    {
        base.OnActivate();

        sampleCoords = [
            new(offset, offset),                                  // Top-left corner
            new(offset, window.Size.Y - offset),                  // Bottom-left corner
            new(window.Size.X - offset, offset),                  // Top-right corner
            new(window.Size.X - offset, window.Size.Y - offset),  // Bottom-right corner
        ];

        // Frame 0 - start colors (Red on left, Green on right)
        startPixels = [
            Colors.Red,    // Top-left
            Colors.Red,    // Bottom-left
            Colors.Green,  // Top-right
            Colors.Green   // Bottom-right
        ];

        // Frame 1200 - end colors (swapped: Green on left, Red on right)
        endPixels = [
            Colors.Green,  // Top-left
            Colors.Green,  // Bottom-left
            Colors.Red,    // Top-right
            Colors.Red     // Bottom-right
        ];

        // Configure and enable pixel sampler
        pixelSampler.SampleCoordinates = sampleCoords;
        pixelSampler.Enabled = true;
        pixelSampler.Activate();  // Start capturing frames
    }
    
    protected override void OnUpdate(double deltaTime)
    {
        if (_testComplete)
            return;
            
        // Start stopwatch on first update
        if (_fpsStopwatch == null)
        {
            _fpsStopwatch = System.Diagnostics.Stopwatch.StartNew();
        }
        
        FramesRendered++;
        
        if (FramesRendered == FrameCount)
        {
            // Stop stopwatch and calculate FPS
            _fpsStopwatch.Stop();
            double elapsedSeconds = _fpsStopwatch.Elapsed.TotalSeconds;
            MeasuredFPS = FrameCount / elapsedSeconds;
            
            // Retrieve all captured pixel samples (one array per frame)
            _testComplete = true;
            Deactivate();
        }
    }
    
    protected override void OnDeactivate()
    {
        // Stop capturing and retrieve all results
        pixelSampler.Deactivate();
        sampledPixelSets = pixelSampler.GetResults();
        
        // Disable pixel sampling when test completes
        pixelSampler.Enabled = false;
        base.OnDeactivate();
    }

    public IEnumerable<TestResult> GetTestResults()
    {
        yield return new TestResult
        {
            TestName = $"BackgroundLayerTest should render for {FrameCount} frames",
            ExpectedResult = FrameCount.ToString(),
            ActualResult = FramesRendered.ToString(),
            Passed = FramesRendered >= FrameCount
        };

        // Validate frame 0 (start colors)
        if (sampledPixelSets.Length > 0)
        {
            var frame0Pixels = sampledPixelSets[0];
            for(int i = 0; i < 4; i++)
            {
                var testName = $"Frame 0: Pixel at {sampleCoords[i]} should match start color";
                var expectedResult = PixelAssertions.DescribeColor(startPixels[i]);
                var actualColor = i < frame0Pixels.Length ? frame0Pixels[i] : null;

                if (actualColor == null)
                {
                    yield return new()
                    {
                        TestName = testName,
                        ExpectedResult = expectedResult,
                        ActualResult = "null",
                        Passed = false
                    };
                }
                else
                {
                    yield return new TestResult
                    {
                        TestName = testName,
                        ExpectedResult = expectedResult,
                        ActualResult = PixelAssertions.DescribeColor(actualColor),
                        Passed = Vector4D.Distance(startPixels[i], actualColor!.Value) < tolerance
                    };
                }
            }
        }

        // Validate frame 40 (end colors)
        if (sampledPixelSets.Length > 0)
        {
            var lastFrame = sampledPixelSets[^1];
            for(int i = 0; i < 4; i++)
            {
                var testName = $"Frame {FrameCount}: Pixel at {sampleCoords[i]} should match end color";
                var expectedResult = PixelAssertions.DescribeColor(endPixels[i]);
                var actualColor = i < lastFrame.Length ? lastFrame[i] : null;

                if (actualColor == null)
                {
                    yield return new()
                    {
                        TestName = testName,
                        ExpectedResult = expectedResult,
                        ActualResult = "null",
                        Passed = false
                    };
                }
                else
                {
                    yield return new TestResult
                    {
                        TestName = testName,
                        ExpectedResult = expectedResult,
                        ActualResult = PixelAssertions.DescribeColor(actualColor),
                        Passed = Vector4D.Distance(endPixels[i], actualColor!.Value) < tolerance
                    };
                }
            }
        }
    }
}