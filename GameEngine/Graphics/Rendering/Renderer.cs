using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using System.Collections.Concurrent;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;

namespace Nexus.GameEngine.Graphics.Rendering;

/// <summary>
/// Renderer implementation that walks the component tree and calls OnRender() directly on IRenderable components.
/// Processes components in RenderPriority order and provides direct GL access for rendering operations.
/// </summary>
public class Renderer(IWindowService windowService, ILogger<Renderer> logger)
    : IRenderer, IDisposable
{
    /// <summary>
    /// Direct access to Silk.NET OpenGL interface for component rendering.
    /// Components use this to make direct GL calls in their OnRender() methods.
    /// Lazily initialized when first accessed.
    /// </summary>
    private GL? _gl;
    public GL GL
    {
        get
        {
            _gl ??= windowService.GetOrCreateWindow().CreateOpenGL();
            return _gl;
        }
    }

    /// <summary>
    /// Root component for the component tree to render.
    /// The renderer walks this tree during RenderFrame() to find IRenderable components and call their OnRender() methods.
    /// </summary>
    public IRuntimeComponent? RootComponent { get; set; }

    /// <summary>
    /// Configured render passes that define rendering pipeline stages.
    /// Each pass can configure different GL state (depth testing, blending, etc.) before rendering components.
    /// Default includes a single "Main" pass with alpha blending and depth testing enabled.
    /// </summary>
    public RenderPassConfiguration[] RenderPasses { get; set; } = [
            new RenderPassConfiguration
            {
                Id = 0,
                Name = "Main",
                DirectRenderMode = true,
                DepthTestEnabled = true,
                BlendingMode = BlendingMode.Alpha
            }
        ];

    /// <summary>
    /// Shared resources dictionary for caching common rendering resources across components.
    /// Used by GLRenderingExtensions and components for efficient resource management.
    /// Thread-safe for concurrent access during rendering.
    /// </summary>
    public ConcurrentDictionary<string, object> SharedResources { get; init; } = new();

    /// <summary>
    /// Walks the component tree and calls OnRender() on IRenderable components.
    /// Components are processed in RenderPriority order within each configured render pass.
    /// </summary>
    public void RenderFrame(double deltaTime)
    {
        if (_disposed)
        {
            logger.LogWarning("Attempted to render frame on disposed renderer");
            return;
        }

        if (RootComponent == null)
        {
            logger.LogTrace("No root component set, skipping frame");
            return;
        }

        // Walk the component tree and collect IRenderable components
        var renderables = FindRenderableComponents(RootComponent);
        var sorted = renderables.OrderBy(component => component.RenderPriority);

        foreach (var component in sorted)
            component.OnRender(this, deltaTime);

        // Process render passes
        foreach (var renderPass in RenderPasses)
        {
            // TODO: define and implement render pass behavior
        }
    }

    /// <summary>
    /// Enumerates all IRenderable components in the component tree
    /// </summary>
    /// <param name="component"></param>
    /// <returns></returns> <summary>
    private IEnumerable<IRenderable> FindRenderableComponents(IRuntimeComponent component)
    {
        if (component is null)
            yield break;

        if (component is IRenderable renderable)
            yield return renderable;

        foreach (var child in component.Children)
        {
            FindRenderableComponents(child);
        }
    }

    private bool _disposed;

    /// <summary>
    /// Cleans up shared resources and disposes the renderer.
    /// Safe to call multiple times.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            // Clean up all shared resources
            SharedResources.Clear();
            logger.LogDebug("Renderer disposed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during renderer disposal");
        }
        finally
        {
            _disposed = true;
        }
    }
}
