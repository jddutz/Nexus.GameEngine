using System.Data;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Components;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace TestApp.TestComponents;

/// <summary>
/// Test component that creates BackgroundLayer children to validate both UniformColor and PerVertexColor modes.
/// 
/// VERTEX ORDER SPECIFICATION (per ColorQuad standard):
///   Index 0 → Top-Left     (Screen: offset, offset)
///   Index 1 → Bottom-Left  (Screen: offset, height-offset)
///   Index 2 → Top-Right    (Screen: width-offset, offset)
///   Index 3 → Bottom-Right (Screen: width-offset, height-offset)
/// 
/// TEST PHASES:
/// Phase 1: UniformColor mode - Blue → Green (0.4s animation)
///   Start: All corners Blue
///   End:   All corners Green
/// 
/// Phase 2: PerVertexColor mode - Starting colors (immediate)
///   TL=Blue, BL=Green, TR=Red, BR=Yellow
/// 
/// Phase 3: PerVertexColor mode - Animate to rotated colors (0.4s animation)
///   TL=Yellow, BL=Red, TR=Green, BR=Blue (clockwise rotation)
/// 
/// Total test duration: ~2.0 seconds
/// Uses AnimationEnded events to capture exact frame numbers when animations complete.
/// </summary>
public class BackgroundLayerTestComponent(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    private IWindow window = windowService.GetWindow();

    public double TestDuration { get; set; } = 0.8d;
    public int FramesRendered { get; private set; } = 0;

    private System.Diagnostics.Stopwatch? _testStopwatch;
    private int currentPhase = 0;  // 0=not started, 1=phase1, 2=phase2

    private const int offset = 2;
    private Vector2D<int>[] sampleCoords = [];
    
    private Vector4D<float>?[][] sampledPixelSets = [];

    private float tolerance = 0.05f;  // Increased tolerance for edge sampling with bilinear filtering

    #region Phase 1 Setup

    private int phase1StartFrame = -1;
    private int phase1CompleteFrame = -1;
    
    // UniformColor constants
    private static readonly Vector4D<float> PH1_START_COLOR = Colors.Blue;
    private static readonly Vector4D<float> PH1_END_COLOR = Colors.Green;

    // Expected colors for validation
    private Vector4D<float>[] phase1StartColors = [
        PH1_START_COLOR,  // Top-left
        PH1_START_COLOR,  // Bottom-left
        PH1_START_COLOR,  // Top-right
        PH1_START_COLOR   // Bottom-right
    ];

    private Vector4D<float>[] phase1CompleteColors = [
        PH1_END_COLOR,  // Top-left
        PH1_END_COLOR,  // Bottom-left
        PH1_END_COLOR,  // Top-right
        PH1_END_COLOR   // Bottom-right
    ];

    private void Setup_Phase1()
    {
        // Phase 1: Create BackgroundLayer with UniformColor mode (Blue)
        var bg = CreateChild(new BackgroundLayer.Template
        {
            Mode = BackgroundLayerModeEnum.UniformColor,
            UniformColor = PH1_START_COLOR
        }) as BackgroundLayer ?? throw new InvalidOperationException("BackgroundLayer component is null");

        // Start animation after the BackgroundLayer component is activated
        bg.Activated += (sender, e) =>
        {
            Logger?.LogInformation("Activating Phase 1");
            // Start animation
            bg.UniformColor = PH1_END_COLOR;
        };

        // Subscribe to animation events to capture exact frames when animations complete
        bg.AnimationStarted += (sender, e) =>
        {
            if (e.PropertyName == nameof(BackgroundLayer.UniformColor))
                phase1StartFrame = FramesRendered;
        };
        bg.AnimationEnded += (sender, e) =>
        {
            if (e.PropertyName == nameof(BackgroundLayer.UniformColor))
            {
                phase1CompleteFrame = FramesRendered - 1;
                bg.Deactivate();
                Setup_Phase2();
            }
        };

        bg.Activate();

        currentPhase = 1;
    }
    
    #endregion

    #region Phase 2 Setup

    private int phase2StartFrame = -1;
    private int phase2CompleteFrame = -1;
    
    // Phase 2: PerVertexColor start constants (matches array indices 0,1,2,3 = TL,BL,TR,BR)
    private static readonly Vector4D<float> PH2_START_COLOR_TL = Colors.Blue;    // Index 0
    private static readonly Vector4D<float> PH2_START_COLOR_BL = Colors.Green;   // Index 1
    private static readonly Vector4D<float> PH2_START_COLOR_TR = Colors.Red;     // Index 2
    private static readonly Vector4D<float> PH2_START_COLOR_BR = Colors.Yellow;  // Index 3
    
    // Phase 3: PerVertexColor end constants (clockwise rotation)
    private static readonly Vector4D<float> PH2_END_COLOR_TL = Colors.Yellow;  // Index 0 (was BR)
    private static readonly Vector4D<float> PH2_END_COLOR_BL = Colors.Red;     // Index 1 (was TR)
    private static readonly Vector4D<float> PH2_END_COLOR_TR = Colors.Green;   // Index 2 (was BL)
    private static readonly Vector4D<float> PH2_END_COLOR_BR = Colors.Blue;    // Index 3 (was TL)

    // Expected colors for validation
    private Vector4D<float>[] phase2StartColors = [
        PH2_START_COLOR_TL,  // Top-left
        PH2_START_COLOR_BL,  // Bottom-left
        PH2_START_COLOR_TR,  // Top-right
        PH2_START_COLOR_BR   // Bottom-right
    ];

    private Vector4D<float>[] phase2CompleteColors = [
        PH2_END_COLOR_TL,  // Top-left (index 0)
        PH2_END_COLOR_BL,  // Bottom-left (index 1)
        PH2_END_COLOR_TR,  // Top-right (index 2)
        PH2_END_COLOR_BR   // Bottom-right (index 3)
    ];

    private void Setup_Phase2()
    {
        // Phase 1: Create BackgroundLayer with UniformColor mode (Blue)
        var bg = CreateChild(new BackgroundLayer.Template
        {
            Mode = BackgroundLayerModeEnum.PerVertexColor,
            VertexColors = phase2StartColors
        }) as BackgroundLayer ?? throw new InvalidOperationException("BackgroundLayer component is null");

        // Start animation after the BackgroundLayer component is activated
        bg.Activated += (sender, e) =>
        {
            // Start animation
            bg.VertexColors = phase2CompleteColors;
        };

        // Subscribe to animation events to capture exact frames when animations complete
        bg.AnimationStarted += (sender, e) =>
        {
            if (e.PropertyName == nameof(BackgroundLayer.VertexColors))
                phase2StartFrame = FramesRendered; // Takes 1 frame for the change to happen
        };
        bg.AnimationEnded += (sender, e) =>
        {
            if (e.PropertyName == nameof(BackgroundLayer.VertexColors))
            {
                phase2CompleteFrame = FramesRendered - 1;
                bg.Deactivate();
            }

            // TODO: Start Phase 3
        };

        bg.Activate();

        currentPhase = 2;
    }

    #endregion

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        sampleCoords = [
            new(offset, offset),                                  // Top-left corner
            new(offset, window.Size.Y - offset),                  // Bottom-left corner
            new(window.Size.X - offset, offset),                  // Top-right corner
            new(window.Size.X - offset, window.Size.Y - offset),  // Bottom-right corner
        ];

        pixelSampler.SampleCoordinates = sampleCoords;
        pixelSampler.Enabled = true;
    }

    protected override void OnUpdate(double deltaTime)
    {
        if (FramesRendered == 0)
        {
            _testStopwatch = System.Diagnostics.Stopwatch.StartNew();
            pixelSampler.Activate();  // Start capturing frames
            Setup_Phase1();
        }

        FramesRendered++;

        if (phase2CompleteFrame > -1)
        {
            // Stop stopwatch and calculate FPS
            _testStopwatch?.Stop();
            Deactivate();
        }
    }
    
    protected void OnDeactivated()
    {
        base.OnDeactivate();
        pixelSampler.Deactivate();
    }

    public IEnumerable<TestResult> GetTestResults()
    {
        sampledPixelSets = pixelSampler.GetResults();
        var _elapsedSeconds = _testStopwatch?.Elapsed.TotalSeconds ?? 0d;
        var fps = FramesRendered / TestDuration;

        yield return new TestResult
        {
            TestName = $"BackgroundLayerTest duration should be at least as long as the animations",
            ExpectedResult = $"~{TestDuration:F1}s",
            ActualResult = $"{_elapsedSeconds:F3}s ({FramesRendered} frames, {fps:F0} fps)",
            Passed = _elapsedSeconds >= TestDuration
        };

        yield return new()
        {
            TestName = "Phase 1 should start at Frame 0",
            ExpectedResult = "0",
            ActualResult = phase1StartFrame.ToString(),
            Passed = phase1StartFrame == 0
        };

        // Phase 1 validation: Start frame (UniformColor start - Blue everywhere)
        if (phase1StartFrame >= 0 && sampledPixelSets.Length > phase1StartFrame)
        {
            var framePixels = sampledPixelSets[phase1StartFrame];
            for (int i = 0; i < 4; i++)
            {
                var testName = $"Phase 1 Frame {phase1StartFrame} (Start): Pixel at {sampleCoords[i]} should be Blue";
                var expectedResult = PixelAssertions.DescribeColor(phase1StartColors[i]);
                var actualColor = i < framePixels.Length ? framePixels[i] : null;

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
                        Passed = Vector4D.Distance(phase1StartColors[i], actualColor!.Value) < tolerance
                    };
                }
            }
        }

        // Phase 1 validation: Complete frame (UniformColor animation complete - Green everywhere)
        if (phase1CompleteFrame >= 0 && sampledPixelSets.Length > phase1CompleteFrame)
        {
            var framePixels = sampledPixelSets[phase1CompleteFrame];
            for(int i = 0; i < 4; i++)
            {
                var testName = $"Phase 1 Frame {phase1CompleteFrame} (Complete): Pixel at {sampleCoords[i]} should be Green";
                var expectedResult = PixelAssertions.DescribeColor(phase1CompleteColors[i]);
                var actualColor = i < framePixels.Length ? framePixels[i] : null;

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
                        Passed = Vector4D.Distance(phase1CompleteColors[i], actualColor!.Value) < tolerance
                    };
                }
            }
        }

        // Phase 2 validation: Start frame (PerVertexColor start - Red left, Green right)
        if (phase2StartFrame >= 0 && sampledPixelSets.Length > phase2StartFrame)
        {
            var framePixels = sampledPixelSets[phase2StartFrame];
            for(int i = 0; i < 4; i++)
            {
                var testName = $"Phase 2 Frame {phase2StartFrame} (Start): Pixel at {sampleCoords[i]} should match PerVertexColor start";
                var expectedResult = PixelAssertions.DescribeColor(phase2StartColors[i]);
                var actualColor = i < framePixels.Length ? framePixels[i] : null;

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
                        Passed = Vector4D.Distance(phase2StartColors[i], actualColor!.Value) < tolerance
                    };
                }
            }
        }

        // Phase 2 validation: Complete frame (PerVertexColor animation complete - rotated colors)
        if (phase2CompleteFrame >= 0 && sampledPixelSets.Length >= phase2CompleteFrame)
        {
            yield return new()
            {
                TestName = "Phase 2 should end before the last frame",
                ExpectedResult = sampledPixelSets.Length.ToString(),
                ActualResult = phase2CompleteFrame.ToString(),
                Passed = phase2CompleteFrame < sampledPixelSets.Length
            };

            var framePixels = sampledPixelSets[phase2CompleteFrame];
            for(int i = 0; i < 4; i++)
            {
                var testName = $"Phase 2 Frame {phase2CompleteFrame} (Complete): Pixel at {sampleCoords[i]} should match PerVertexColor end";
                var expectedResult = PixelAssertions.DescribeColor(phase2CompleteColors[i]);
                var actualColor = i < framePixels.Length ? framePixels[i] : null;

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
                        Passed = Vector4D.Distance(phase2CompleteColors[i], actualColor!.Value) < tolerance
                    };
                }
            }
        }
    }
}