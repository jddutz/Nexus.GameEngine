using Nexus.GameEngine.Graphics.Pipelines;
using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Describes a single Vulkan draw command - what to draw and how.
/// Contains all information needed for batching, state management, and rendering.
/// </summary>
public readonly struct DrawCommand
{
    // REQUIRED
    public required uint RenderMask { get; init; }
    public required PipelineHandle Pipeline { get; init; }
    public required Silk.NET.Vulkan.Buffer VertexBuffer { get; init; }
    public required uint VertexCount { get; init; }

    // OPTIONAL with sensible defaults
    public Silk.NET.Vulkan.Buffer IndexBuffer { get; init; }
    public DescriptorSet DescriptorSet { get; init; }
    public uint FirstVertex { get; init; }
    public uint InstanceCount { get; init; }

    // PUSH CONSTANTS
    /// <summary>
    /// Push constant data to send to shaders before drawing.
    /// Push constants are small amounts of data (typically up to 128 bytes) that can be
    /// updated very efficiently between draw calls.
    /// </summary>
    public object? PushConstants { get; init; }

    // RENDER ORDERING
    /// <summary>
    /// Priority for render ordering within a RenderPass.
    /// Lower values render first. Use this for layering (e.g., background=0, scene=100, UI=1000).
    /// </summary>
    public int RenderPriority { get; init; }

    /// <summary>
    /// Distance from camera for depth sorting (typically for transparency).
    /// Higher values render first (back-to-front for correct alpha blending).
    /// Only used when batch strategy performs depth sorting.
    /// Components calculate this using RenderContext.Camera.Position in GetDrawCommands().
    /// Example: DepthSortKey = Vector3D.DistanceSquared(myPosition, context.Camera.Position)
    /// </summary>
    public float DepthSortKey { get; init; }

    /// <summary>
    /// Constructor with default values for optional fields.
    /// </summary>
    public DrawCommand()
    {
        InstanceCount = 1;
        RenderPriority = 0;
        DepthSortKey = 0f;
    }
}
