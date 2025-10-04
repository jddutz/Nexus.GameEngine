using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Cameras;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Represents a viewport that renders a component tree through a camera to a screen region or framebuffer.
/// Viewports manage their own content and can pass camera/viewport context to components during rendering.
/// </summary>
public interface IViewport : IRuntimeComponent
{
    /// <summary>
    /// Screen region in pixels where this viewport renders. Full screen is typically (0, 0, screenWidth, screenHeight).
    /// </summary>
    Rectangle<int> ScreenRegion { get; }

    /// <summary>
    /// Camera that defines the view transformation (position, orientation, projection).
    /// </summary>
    ICamera Camera { get; }

    /// <summary>
    /// Framebuffer target for off-screen rendering. Null for default framebuffer (screen).
    /// </summary>
    uint? FramebufferTarget { get; }

    /// <summary>
    /// Rendering priority. Lower values render first (background), higher values render last (overlays).
    /// </summary>
    int ViewportPriority { get; }

    /// <summary>
    /// Render passes that this viewport executes. Each pass can have different GL state configuration.
    /// </summary>
    List<RenderPassConfiguration> RenderPasses { get; }

    /// <summary>
    /// Component tree to render in this viewport. This allows different viewports to have completely separate content.
    /// Setting this property schedules a content change for the next update cycle to avoid state conflicts.
    /// </summary>
    IRuntimeComponent? Content { get; set; }

    /// <summary>
    /// Whether this viewport requires a GL flush after rendering completes.
    /// Usually false unless doing multi-pass effects or viewport synchronization.
    /// </summary>
    bool FlushAfterRender { get; }

    /// <summary>
    /// Render this viewport's content by walking the Content component tree.
    /// Returns all render states needed to display this viewport.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame</param>
    /// <returns>All render states from components in this viewport</returns>
    IEnumerable<RenderState> OnRender(double deltaTime);
}