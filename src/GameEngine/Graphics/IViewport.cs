using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Cameras;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Defines a viewport, which is a distinct screen region or framebuffer responsible for rendering a specific camera and component tree.
/// Viewports manage their own content, camera, and rendering passes, and can be used for split screens, overlays, or off-screen rendering.
/// </summary>
public interface IViewport : IRuntimeComponent
{
    /// <summary>
    /// Gets the screen region in pixels where this viewport renders its content.
    /// For full screen, this is typically (0, 0, screenWidth, screenHeight).
    /// </summary>
    Rectangle<int> ScreenRegion { get; }

    /// <summary>
    /// Gets the camera instance that defines the view transformation (position, orientation, projection) for this viewport.
    /// Assigning a camera determines the perspective and view for all rendered content.
    /// </summary>
    ICamera? Camera { get; }

    /// <summary>
    /// Gets the framebuffer target for off-screen rendering. If null, the default framebuffer (screen) is used.
    /// Useful for post-processing effects or rendering to textures.
    /// </summary>
    uint? FramebufferTarget { get; }

    /// <summary>
    /// Gets the rendering priority for this viewport. Lower values render first (background), higher values render last (overlays).
    /// Used to control draw order when multiple viewports are present.
    /// </summary>
    int ViewportPriority { get; }

    /// <summary>
    /// Gets the list of render passes that this viewport executes. Each pass can have different OpenGL state configuration.
    /// Enables advanced rendering techniques such as multi-pass effects and custom state management.
    /// </summary>
    List<RenderPassConfiguration> RenderPasses { get; }

    /// <summary>
    /// Gets or sets the root runtime component tree to render in this viewport.
    /// Allows different viewports to have completely separate content.
    /// Setting this property schedules a content change for the next update cycle to avoid state conflicts.
    /// </summary>
    IRuntimeComponent? Content { get; set; }

    /// <summary>
    /// Gets a value indicating whether this viewport requires a GL flush after rendering completes.
    /// Usually false unless doing multi-pass effects or viewport synchronization.
    /// </summary>
    bool FlushAfterRender { get; }

    /// <summary>
    /// Gets or sets the background color used to clear the viewport before rendering its content.
    /// This color is rendered behind all other content in the viewport.
    /// </summary>
    Vector4D<float> BackgroundColor { get; set; }
}