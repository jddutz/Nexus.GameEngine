using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Information about a cached pipeline.
/// Used for debugging, editor tools, and profiling.
/// </summary>
public record PipelineInfo
{
    /// <summary>
    /// Name of the pipeline.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Vulkan pipeline handle.
    /// </summary>
    public required Pipeline Handle { get; init; }

    /// <summary>
    /// Path to vertex shader.
    /// </summary>
    public required string VertexShaderPath { get; init; }

    /// <summary>
    /// Path to fragment shader.
    /// </summary>
    public required string FragmentShaderPath { get; init; }

    /// <summary>
    /// Path to geometry shader (if used).
    /// </summary>
    public string? GeometryShaderPath { get; init; }

    /// <summary>
    /// Number of times this pipeline has been accessed.
    /// </summary>
    public int AccessCount { get; init; }

    /// <summary>
    /// Time when pipeline was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Time when pipeline was last accessed.
    /// </summary>
    public DateTime LastAccessedAt { get; init; }

    /// <summary>
    /// Estimated GPU memory usage for this pipeline (bytes).
    /// </summary>
    public long EstimatedMemoryUsageBytes { get; init; }

    /// <summary>
    /// Whether this is a specialized/optimized pipeline.
    /// </summary>
    public bool IsSpecialized { get; init; }

    /// <summary>
    /// Primitive topology used by this pipeline.
    /// </summary>
    public PrimitiveTopology Topology { get; init; }

    /// <summary>
    /// Whether depth testing is enabled.
    /// </summary>
    public bool DepthTestEnabled { get; init; }

    /// <summary>
    /// Whether blending is enabled.
    /// </summary>
    public bool BlendingEnabled { get; init; }
}
