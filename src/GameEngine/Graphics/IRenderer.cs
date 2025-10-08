
namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Renderer interface providing Vulkan context access and render orchestration.
/// Renders IRenderable components by walking the component tree.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Access to Vulkan context for component rendering.
    /// Components use this to access Vulkan resources during their render phase.
    /// </summary>
    VulkanContext VulkanContext { get; }

    /// <summary>
    /// Event fired before rendering begins for the frame
    /// </summary>
    event EventHandler? BeforeRendering;

    /// <summary>
    /// Event fired after rendering completes for the frame
    /// </summary>
    event EventHandler? AfterRendering;

    /// <summary>
    /// Handles window render events. Walks the component tree.
    /// Calls GetRenderElements() on each IRenderable component to collect rendering data.
    /// </summary>
    void OnRender(double deltaTime);
}
