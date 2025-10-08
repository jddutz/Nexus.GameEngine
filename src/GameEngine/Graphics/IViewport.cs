using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Cameras;
using Silk.NET.Maths;

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
    float X { get; set; }

    /// <summary>
    /// Y position of the viewport (normalized 0-1)
    /// </summary>
    float Y { get; set; }

    /// <summary>
    /// Width of the viewport (normalized 0-1)
    /// </summary>
    float Width { get; set; }

    /// <summary>
    /// Height of the viewport (normalized 0-1)
    /// </summary>
    float Height { get; set; }

    /// <summary>
    /// Background color for clearing the viewport (RGBA values 0-1)
    /// </summary>
    Vector4D<float> BackgroundColor { get; set; }
}
