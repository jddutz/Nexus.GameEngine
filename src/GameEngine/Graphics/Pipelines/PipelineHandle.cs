namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Immutable wrapper containing both a Vulkan Pipeline and its associated PipelineLayout.
/// Components hold references to this for rendering. The PipelineManager owns the lifecycle.
/// </summary>
/// <param name="Pipeline">The Vulkan graphics pipeline handle</param>
/// <param name="Layout">The Vulkan pipeline layout handle (needed for push constants and descriptor sets)</param>
/// <param name="Name">Human-readable name for debugging and identification</param>
public readonly record struct PipelineHandle(Pipeline Pipeline, PipelineLayout Layout, string Name)
{
    /// <summary>
    /// Returns true if this handle contains valid (non-zero) pipeline and layout handles.
    /// </summary>
    public bool IsValid => Pipeline.Handle != 0 && Layout.Handle != 0;

    /// <summary>
    /// Returns an invalid/empty pipeline handle.
    /// </summary>
    public static PipelineHandle Invalid => new(default, default, "Invalid");
}
