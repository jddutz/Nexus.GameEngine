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
/// </summary>
[TestRunnerIgnore(reason: "Incomplete")]
public class BackgroundLayerHorizGradientTest(IPixelSampler pixelSampler, IWindowService windowService)
    : RuntimeComponent(), ITestComponent
{
    public IEnumerable<TestResult> GetTestResults()
    {
        throw new NotImplementedException();
    }
}