using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Runtime;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Stub Vulkan renderer implementation.
/// This will be rebuilt from scratch to use Vulkan instead of OpenGL.
/// </summary>
public class Renderer(
    IVkContext vk,
    ILoggerFactory loggerFactory,
    IContentManager contentManager) : IRenderer
{
    private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(Renderer));

    public IVkContext VK => vk;

    public event EventHandler? BeforeRendering;
    public event EventHandler? AfterRendering;

    public void OnRender(double deltaTime)
    {
        // TODO: Implement Vulkan rendering pipeline
        // For now, this is a stub to get compilation working
        if (contentManager.Viewport.Content == null)
        {
            throw new InvalidOperationException("ContentManager.Viewport.Content is null, nothing to render.");
        }

        BeforeRendering?.Invoke(this, EventArgs.Empty);

        _logger.LogTrace("Render frame - deltaTime: {DeltaTime:F4}s", deltaTime);

        // TODO: 
        // 1. Acquire swapchain image
        // 2. Begin command buffer recording
        // 3. Begin render pass

        // 4. Walk component tree and collect render elements
        var renderables = contentManager.Viewport.Content.GetChildren<IRenderable>();
        foreach (var element in renderables.SelectMany(r => r.GetElements()))
        {
            Draw(element);
        }

        // 5. Bind pipelines and draw
        // 6. End render pass
        // 7. End command buffer
        // 8. Submit to queue
        // 9. Present swapchain image

        AfterRendering?.Invoke(this, EventArgs.Empty);
    }

    public void Draw(ElementData element) { }
}
