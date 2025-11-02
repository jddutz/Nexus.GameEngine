using Nexus.GameEngine;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Testing;
using Silk.NET.Maths;

namespace TestApp.TestComponents.UserInterfaceComponents;

/// <summary>
/// Tests ColorRect component with basic rendering using NDC coordinates and identity matrix.
/// SIMPLIFIED: Just draw a red quad in the center to verify rendering works.
/// </summary>
public partial class ColoredRectTest(
    IPixelSampler pixelSampler
    ) : RenderableTest(pixelSampler)
{
    [Test("ColorRect test")]
    public readonly static ColoredRectTestTemplate TestTemplate = new()
    {
        FrameCount = 1,
        Subcomponents = [
            new ElementTemplate()
            {
                TintColor = Colors.Red,
                Position = new Vector3D<float>(0, 0, 0),  // Will be overridden in GetDrawCommands
                Scale = new Vector3D<float>(0.5f, 0.5f, 1.0f),  // Will be overridden in GetDrawCommands
                Visible = true
            }
        ],
        SampleCoordinates = [
            new(960, 540),   // Center - should be red
        ],
        ExpectedResults = new Dictionary<int, Vector4D<float>[]>()
        {
            [0] = [
                Colors.Red,  // Center
            ]
        }
    };

    protected override void OnActivate()
    {
        base.OnActivate();
        Log.Debug("=== ColoredRectTest.OnActivate() ===");
        Log.Debug($"  Children count: {Children.Count()}");
        foreach (var child in Children)
        {
            Log.Debug($"  Child: {child.GetType().Name}, IsLoaded={child.IsLoaded}, IsValid={child.IsValid}");
            if (child is IDrawable drawable)
            {
                Log.Debug($"    IsDrawable: true, IsVisible={drawable.IsVisible()}");
            }
        }
    }

    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        Log.Debug("=== ColoredRectTest.GetDrawCommands() CALLED ===");
        Log.Debug($"  Children count: {Children.Count()}");
        Log.Debug($"  Context.Camera: {context.Camera?.GetType().Name}");
        
        // Debug camera matrices
        if (context.Camera is Nexus.GameEngine.Graphics.Cameras.StaticCamera staticCam)
        {
            Log.Debug($"  StaticCamera.ViewMatrix: {staticCam.ViewMatrix}");
            Log.Debug($"  StaticCamera.ProjectionMatrix: {staticCam.ProjectionMatrix}");
            Log.Debug($"  StaticCamera.GetViewProjectionMatrix(): {staticCam.GetViewProjectionMatrix()}");
        }
        
        int commandCount = 0;
        
        foreach (var child in Children)
        {
            Log.Debug($"  Processing child: {child.GetType().Name}");
            
            if (child is Element element && element.IsVisible())
            {
                Log.Debug($"    Element IsVisible: true");
                
                // Get the geometry resource from the child element
                var elementCommands = element.GetDrawCommands(context).ToList();
                Log.Debug($"    Element returned {elementCommands.Count} draw commands");
                
                foreach (var cmd in elementCommands)
                {
                    commandCount++;
                    
                    Log.Debug($"    === DrawCommand #{commandCount} ===");
                    Log.Debug($"      RenderMask: {cmd.RenderMask}");
                    Log.Debug($"      Pipeline.Handle: {cmd.Pipeline.Pipeline.Handle}");
                    Log.Debug($"      VertexBuffer.Handle: {cmd.VertexBuffer.Handle}");
                    Log.Debug($"      VertexCount: {cmd.VertexCount}");
                    Log.Debug($"      InstanceCount: {cmd.InstanceCount}");
                    Log.Debug($"      RenderPriority: {cmd.RenderPriority}");
                    
                    if (cmd.PushConstants is TransformedColorPushConstants tpc)
                    {
                        Log.Debug($"      Original ViewProjectionMatrix: {tpc.ViewProjectionMatrix}");
                        Log.Debug($"      Original Color: R={tpc.Color.X:F3}, G={tpc.Color.Y:F3}, B={tpc.Color.Z:F3}, A={tpc.Color.W:F3}");
                    }
                    
                    var identity = Matrix4X4<float>.Identity;
                    var redColor = Colors.Red;
                    
                    Log.Debug($"      NEW Identity Matrix: {identity}");
                    Log.Debug($"      NEW Red Color: R={redColor.X:F3}, G={redColor.Y:F3}, B={redColor.Z:F3}, A={redColor.W:F3}");
                    
                    var newPushConstants = TransformedColorPushConstants.FromMatrixAndColor(identity, redColor);
                    Log.Debug($"      NEW PushConstants created - Matrix: {newPushConstants.ViewProjectionMatrix}");
                    Log.Debug($"      NEW PushConstants created - Color: R={newPushConstants.Color.X:F3}, G={newPushConstants.Color.Y:F3}, B={newPushConstants.Color.Z:F3}, A={newPushConstants.Color.W:F3}");
                    
                    yield return cmd with
                    {
                        PushConstants = newPushConstants
                    };
                }
                
                break; // Only process first element
            }
            else
            {
                Log.Debug($"    Child is not visible Element");
            }
        }
        
        Log.Debug($"  ColoredRectTest yielded {commandCount} draw commands");
        
        // Call base to increment FramesRendered counter
        foreach (var cmd in base.GetDrawCommands(context))
        {
            yield return cmd;
        }
        
        Log.Debug("=== ColoredRectTest.GetDrawCommands() COMPLETE ===");
    }
}
