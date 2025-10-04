using System.Collections.Concurrent;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Silk.NET.OpenGL;

namespace OpenGLTests;

/// <summary>
/// Test implementation of IRenderer for unit and integration tests.
/// </summary>
public class TestRenderer(GL gl) : IRenderer, IDisposable
{
    public GL GL { get; } = gl;
    public IViewport Viewport { get; set; } = new Viewport();

    public event EventHandler<PreRenderEventArgs>? BeforeRenderFrame;
    public event EventHandler<PostRenderEventArgs>? AfterRenderFrame;

    public void OnRender(double deltaTime)
    {
        BeforeRenderFrame?.Invoke(this, new());

        if (Viewport == null)
            return;

        var renderStates = Viewport.OnRender(deltaTime);
        // Process the render states for testing
        foreach (var state in renderStates)
        {
            // For test purposes, just validate that states were returned
        }

        AfterRenderFrame?.Invoke(this, new());
    }

    public void Dispose()
    {
        // Cleanup test resources if needed
    }

    public void OnLoad()
    {
        throw new NotImplementedException();
    }
}