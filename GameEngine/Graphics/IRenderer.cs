using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Renderer interface providing direct GL access and render orchestration.
/// Renders IRenderable components by walking the component tree and calling OnRender() methods.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Direct access to Silk.NET OpenGL interface for component rendering.
    /// Components use this to make direct GL calls in their OnRender() methods.
    /// </summary>
    GL GL { get; }

    event EventHandler<RenderEventArgs>? BeforeRendering;
    event EventHandler<RenderEventArgs>? AfterRendering;

    /// <summary>
    /// Handles IWindow.Render events. Walks the component tree.
    /// Calls OnRender() on each IRenderable component to collect RenderStates.
    /// RenderStates are sorted and processed using IBatchStrategy.
    /// </summary>
    void OnRender(double deltaTime);
}