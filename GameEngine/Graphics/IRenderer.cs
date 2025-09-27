using Silk.NET.OpenGL;
using System.Collections.Concurrent;
using Nexus.GameEngine.Components;

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
    /// Root component for the component tree to render.
    /// The renderer walks this tree during RenderFrame() to find and render IRenderable components.
    /// </summary>
    IRuntimeComponent? RootComponent { get; set; }

    /// <summary>
    /// Configured render passes that define rendering pipeline stages.
    /// Each pass can have different GL state configuration (depth testing, blending, etc.).
    /// </summary>
    RenderPassConfiguration[] RenderPasses { get; set; }

    /// <summary>
    /// Shared resources dictionary for caching common rendering resources.
    /// Used by GLRenderingExtensions for resource management and caching.
    /// </summary>
    ConcurrentDictionary<string, object> SharedResources { get; }

    /// <summary>
    /// Walks the component tree and calls OnRender() on IRenderable components.
    /// Components are processed in RenderPriority order (lower values render first).
    /// Each render pass may apply different GL state before rendering components.
    /// </summary>
    void RenderFrame(double deltaTime);
}