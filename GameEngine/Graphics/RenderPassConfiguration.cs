using Silk.NET.Maths;
using Nexus.GameEngine.Resources;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Configuration for a single rendering pass.
/// Defines the basic settings needed for each pass in a multi-pass rendering pipeline.
/// </summary>
public class RenderPassConfiguration
{
    /// <summary>
    /// Unique identifier for this pass.
    /// Pass 0 is typically the final/composition pass.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Human-readable name for this pass (e.g., "Shadow", "Main", "UI").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// List of pass IDs that must complete before this pass can execute.
    /// Used for dependency ordering in multi-pass rendering.
    /// </summary>
    public List<int> Dependencies { get; set; } = [];

    public Vector4D<float> FillColor { get; set; } = Colors.Transparent;

    /// <summary>
    /// Whether this pass renders directly to the screen/backbuffer.
    /// False means rendering to an intermediate render target.
    /// </summary>
    public bool DirectRenderMode { get; set; } = true;

    /// <summary>
    /// Enable depth testing for this pass.
    /// </summary>
    public bool DepthTestEnabled { get; set; } = true;

    /// <summary>
    /// Blending mode for this pass.
    /// Defines how new pixels are combined with existing pixels in the framebuffer.
    /// </summary>
    public BlendingMode BlendingMode { get; set; } = BlendingMode.None;
}