using Silk.NET.OpenGL;
using System.Collections.Concurrent;

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

    /// <summary>
    /// Main viewport, defines how components should be displayed on the screen.
    /// </summary>
    IViewport Viewport { get; }

    /// <summary>
    /// Walks the component tree and calls OnRender() on IRenderable components.
    /// Components are processed in RenderPriority order (lower values render first).
    /// Each render pass may apply different GL state before rendering components.
    /// </summary>
    void RenderFrame(double deltaTime);
}