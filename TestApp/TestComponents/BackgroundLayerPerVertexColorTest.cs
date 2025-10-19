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
/// Single frame test: TL=Blue, BL=Green, TR=Red, BR=Yellow
/// </summary>
public class BackgroundLayerPerVertexColorTest(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    private bool rendered = false;

    // Vertex colors: TL=Blue, BL=Green, TR=Red, BR=Yellow
    private static readonly Vector4D<float>[] vertexColors = [
        Colors.Blue,    // Index 0 - Top-Left
        Colors.Green,   // Index 1 - Bottom-Left
        Colors.Red,     // Index 2 - Top-Right
        Colors.Yellow   // Index 3 - Bottom-Right
    ];

    protected override void OnConfigure(IComponentTemplate? componentTemplate)
    {
        base.OnConfigure(componentTemplate);

        CreateChild(new BackgroundLayer.Template()
        {
            Mode = BackgroundLayerModeEnum.PerVertexColor,
            VertexColors = vertexColors
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

        // Check vertex colors
        for(int i=0; i<samples[0].Length; i++)
        {
            yield return new()
            {
                TestName = $"Per-vertex color Pixel[{i}] color check",
                ExpectedResult = PixelAssertions.DescribeColor(vertexColors[i]),
                ActualResult = PixelAssertions.DescribeColor(samples[0][i]),
                Passed = PixelAssertions.ColorsMatch(samples[0][i], vertexColors[i])
            };
        }
    }
}