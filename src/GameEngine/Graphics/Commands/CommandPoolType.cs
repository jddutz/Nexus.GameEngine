namespace Nexus.GameEngine.Graphics.Commands;

/// <summary>
/// Type of command pool based on its intended use.
/// Determines queue family and optimization flags.
/// </summary>
public enum CommandPoolType
{
    /// <summary>
    /// Graphics queue pool - for rendering commands.
    /// Most common type for drawing operations.
    /// </summary>
    Graphics,

    /// <summary>
    /// Transfer queue pool - for data uploads/downloads.
    /// Optimized for memory transfer operations.
    /// </summary>
    Transfer,

    /// <summary>
    /// Compute queue pool - for compute shader dispatch.
    /// Used for GPU compute workloads.
    /// </summary>
    Compute,

    /// <summary>
    /// Transient graphics pool - for short-lived commands.
    /// Optimized for single-use command buffers that are reset frequently.
    /// </summary>
    TransientGraphics
}
