using System.Diagnostics;
using Nexus.GameEngine;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Runtime;
using Nexus.GameEngine.Runtime.Extensions;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace TestApp.Tests;

public partial class TestComponent : Component, ITestComponent
{
    [ComponentProperty]
    [TemplateProperty]
    public int _frameCount = 1;
    public int Updates { get; private set; } = 0;

    public TestComponent()
    {
        // Subscribe to validation events to log errors before activation is aborted
        ValidationFailed += OnValidationFailed;
    }

    private void OnValidationFailed(object? sender, EventArgs e)
    {
        Log.Error($"[{GetType().Name}] '{Name}' validation failed:");
        foreach (var error in ValidationErrors)
        {
            Log.Error($"  - {error.Message}");
        }
    }

    protected override void OnActivate()
    {
        base.OnActivate();

        // If we reach here, validation passed (Activate() checks IsValid() first)
        Log.Debug($"{GetType().Name} '{Name}' passed all validation tests");
        
        // Set size constraints for all UI element children
        // This ensures root UI elements get properly sized and positioned
        var windowSize = Window.GetWindow().FramebufferSize;
        var constraints = new Rectangle<float>(-windowSize.X / 2f, -windowSize.Y / 2f, windowSize.X, windowSize.Y);
        foreach (var child in GetChildren<IUserInterfaceElement>())
        {
            child.UpdateLayout(constraints);
            child.ApplyUpdates(0.0);
        }
    }

    protected override void OnUpdate(double deltaTime)
    {
        // Deactivate one frame after FrameCount, 
        // to allow the last frame to be fully executed
        if (Updates > FrameCount)
        {
            Log.Debug($"{GetType().Name} '{Name}' reached frame limit, deactivating");
            Deactivate();
        }
        Updates++;
    }

    public virtual IEnumerable<TestResult> GetTestResults() => [];
}
