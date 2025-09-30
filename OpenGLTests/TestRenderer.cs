using System.Collections.Concurrent;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Silk.NET.OpenGL;

namespace OpenGLTests;

/// <summary>
/// Test implementation of IRenderer for unit and integration tests.
/// </summary>
public class TestRenderer : IRenderer, IDisposable
{
    public GL GL { get; }
    public IViewport? Viewport { get; set; }

    public TestRenderer(GL gl)
    {
        GL = gl;
    }

    public void RenderFrame(double deltaTime)
    {
        if (Viewport == null)
            return;

        var renderStates = Viewport.OnRender(deltaTime);
        // Process the render states for testing
        foreach (var state in renderStates)
        {
            // For test purposes, just validate that states were returned
        }
    }

    public void Dispose()
    {
        // Cleanup test resources if needed
    }
}