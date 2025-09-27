using System.Collections.Concurrent;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Rendering;
using Silk.NET.OpenGL;

namespace OpenGLTests;

/// <summary>
/// Test implementation of IRenderer for unit and integration tests.
/// </summary>
public class TestRenderer : IRenderer, IDisposable
{
    public GL GL { get; }
    public IRuntimeComponent? RootComponent { get; set; }
    public RenderPassConfiguration[] RenderPasses { get; set; } =
    [
        new RenderPassConfiguration
        {
            Id = 0,
            Name = "Test",
            DirectRenderMode = true,
            DepthTestEnabled = true,
            BlendingMode = BlendingMode.None
        }
    ];
    public ConcurrentDictionary<string, object> SharedResources { get; } = new();

    public TestRenderer(GL gl)
    {
        GL = gl;
    }

    public void RenderFrame(double deltaTime)
    {
        if (RootComponent == null)
            return;

        foreach (var component in FindRenderableComponents(RootComponent).OrderBy(c => c.RenderPriority))
            component.OnRender(this, deltaTime);
    }

    private static IEnumerable<IRenderable> FindRenderableComponents(IRuntimeComponent component)
    {
        if (component is null)
            yield break;
        if (component is IRenderable renderable)
            yield return renderable;
        foreach (var child in component.Children)
        {
            foreach (var r in FindRenderableComponents(child))
                yield return r;
        }
    }

    public void Dispose()
    {
        SharedResources.Clear();
    }
}