using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Cameras;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Viewport interface for managing rendering regions with cameras and content.
/// </summary>
public interface IViewport : IRuntimeComponent
{
    /// <summary>
    /// The camera used for this viewport's view/projection matrices
    /// </summary>
    ICamera? Camera { get; set; }

    /// <summary>
    /// The root content to render in this viewport
    /// </summary>
    IRuntimeComponent? Content { get; set; }

    /// <summary>
    /// X position of the viewport (normalized 0-1)
    /// </summary>
    float X { get; }

    /// <summary>
    /// Y position of the viewport (normalized 0-1)
    /// </summary>
    float Y { get; }

    /// <summary>
    /// Width of the viewport (normalized 0-1)
    /// </summary>
    float Width { get; }

    /// <summary>
    /// Height of the viewport (normalized 0-1)
    /// </summary>
    float Height { get; }

    /// <summary>
    /// Background color for clearing the viewport (RGBA values 0-1)
    /// </summary>
    Vector4D<float> BackgroundColor { get; }
    
    /// <summary>
    /// Set the X position of the viewport (normalized 0-1)
    /// </summary>
    void SetX(float value, float duration = -1f, Animation.InterpolationMode interpolation = (Animation.InterpolationMode)(-1));
    
    /// <summary>
    /// Set the Y position of the viewport (normalized 0-1)
    /// </summary>
    void SetY(float value, float duration = -1f, Animation.InterpolationMode interpolation = (Animation.InterpolationMode)(-1));
    
    /// <summary>
    /// Set the Width of the viewport (normalized 0-1)
    /// </summary>
    void SetWidth(float value, float duration = -1f, Animation.InterpolationMode interpolation = (Animation.InterpolationMode)(-1));
    
    /// <summary>
    /// Set the Height of the viewport (normalized 0-1)
    /// </summary>
    void SetHeight(float value, float duration = -1f, Animation.InterpolationMode interpolation = (Animation.InterpolationMode)(-1));
    
    /// <summary>
    /// Set the background color for clearing the viewport (RGBA values 0-1)
    /// </summary>
    void SetBackgroundColor(Vector4D<float> value, float duration = -1f, Animation.InterpolationMode interpolation = (Animation.InterpolationMode)(-1));
    
    /// <summary>
    /// Gets the cached Vulkan clear color value.
    /// This is automatically updated when BackgroundColor changes.
    /// </summary>
    ClearValue ClearColorValue { get; }

    /// <summary>
    /// Gets the Vulkan viewport structure for this viewport.
    /// Converts normalized coordinates to pixel coordinates based on swapchain extent.
    /// </summary>
    Silk.NET.Vulkan.Viewport VulkanViewport { get; }

    /// <summary>
    /// Gets the Vulkan scissor rectangle for this viewport.
    /// Converts normalized coordinates to pixel coordinates based on swapchain extent.
    /// </summary>
    Rect2D VulkanScissor { get; }
}
