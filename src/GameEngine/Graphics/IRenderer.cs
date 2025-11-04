
namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Event args for rendering events that include the swapchain image index.
/// </summary>
public class RenderEventArgs : EventArgs
{
    /// <summary>
    /// Gets the index of the swapchain image that was rendered/presented.
    /// </summary>
    public uint ImageIndex { get; init; }
}

/// <summary>
/// Renderer interface providing Vulkan context access and render orchestration.
/// Renders IDrawable components by walking the component tree.
/// </summary>
public interface IRenderer
{
    /// <summary>
    /// Event fired before rendering begins for the frame.
    /// Includes the swapchain image index that will be rendered.
    /// </summary>
    event EventHandler<RenderEventArgs>? BeforeRendering;

    /// <summary>
    /// Event fired after rendering completes for the frame.
    /// Includes the swapchain image index that was rendered.
    /// </summary>
    event EventHandler<RenderEventArgs>? AfterRendering;
    
    /// <summary>
    /// Event fired when batching statistics are collected (only if CollectBatchingStatistics is enabled).
    /// Provides information about batching effectiveness for validation and debugging.
    /// </summary>
    event EventHandler<BatchingStatisticsEventArgs>? BatchingStatisticsAvailable;
    
    /// <summary>
    /// Gets or sets whether to collect and report batching statistics.
    /// Disabled by default for performance. Enable for validation and debugging.
    /// </summary>
    bool CollectBatchingStatistics { get; set; }

    /// <summary>
    /// Handles window render events. Walks the component tree.
    /// Calls GetRenderElements() on each IDrawable component to collect rendering data.
    /// </summary>
    void OnRender(double deltaTime);
}
