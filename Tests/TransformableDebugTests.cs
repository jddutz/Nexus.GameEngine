using Silk.NET.Maths;
using Xunit;
using Xunit.Abstractions;

namespace Nexus.GameEngine.Components.Tests;

public class TransformableDebugTests
{
    private readonly ITestOutputHelper _output;

    public TransformableDebugTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Debug_RotateAndCheckForward()
    {
        var transform = new Transformable();
        
        _output.WriteLine($"IsActive: {transform.IsActive}");
        _output.WriteLine($"Initial Rotation: {transform.Rotation}");
        _output.WriteLine($"Initial Forward: {transform.Forward}");
        
        // Rotate 90 degrees around Y
        transform.RotateY(MathF.PI / 2);
        
        _output.WriteLine($"After RotateY(PI/2) Rotation: {transform.Rotation}");
        _output.WriteLine($"After RotateY(PI/2) Forward: {transform.Forward}");
        
        var expected = Vector3D<float>.UnitX;
        _output.WriteLine($"Expected Forward: {expected}");
        
        var diff = new Vector3D<float>(
            MathF.Abs(expected.X - transform.Forward.X),
            MathF.Abs(expected.Y - transform.Forward.Y),
            MathF.Abs(expected.Z - transform.Forward.Z));
        _output.WriteLine($"Difference: {diff}");
    }
}
