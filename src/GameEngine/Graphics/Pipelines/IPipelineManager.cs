using Silk.NET.Vulkan;

namespace Nexus.GameEngine.Graphics.Pipelines;

/// <summary>
/// Manages Vulkan graphics pipelines with caching, lifecycle management, and hot-reload support.
/// Thread-safe pipeline creation and access for multi-threaded loading scenarios.
/// </summary>
/// <remarks>
/// <para><strong>Responsibilities:</strong></para>
/// <list type="bullet">
/// <item>Pipeline creation with descriptor-based caching</item>
/// <item>Pipeline invalidation when resources change</item>
/// <item>Shader hot-reload for development workflow</item>
/// <item>Render pass compatibility validation</item>
/// <item>Window resize event handling for viewport-dependent pipelines</item>
/// <item>Thread-safe concurrent pipeline access</item>
/// <item>Graceful degradation with fallback pipelines</item>
/// </list>
/// 
/// <para><strong>Lifecycle:</strong></para>
/// <list type="number">
/// <item>Created once during application startup</item>
/// <item>Subscribes to window resize events</item>
/// <item>Pipelines created on-demand via GetOrCreatePipeline()</item>
/// <item>Pipelines invalidated when shaders/resources change</item>
/// <item>All pipelines disposed on manager disposal</item>
/// </list>
/// 
/// <para><strong>Usage Example:</strong></para>
/// <code>
/// var descriptor = new PipelineDescriptor
/// {
///     Name = "SpritePipeline",
///     VertexShaderPath = "shaders/sprite.vert.spv",
///     FragmentShaderPath = "shaders/sprite.frag.spv",
///     VertexInputDescription = SpriteVertex.GetDescription(),
///     Topology = PrimitiveTopology.TriangleList,
///     RenderPass = renderPass,
///     // ... other pipeline state
/// };
/// 
/// var pipeline = pipelineManager.GetOrCreatePipeline(descriptor);
/// </code>
/// </remarks>
public interface IPipelineManager : IDisposable
{
    /// <summary>
    /// Gets or creates a pipeline based on the provided descriptor.
    /// Caches pipelines by descriptor hash for efficient reuse.
    /// Thread-safe for concurrent access during multi-threaded loading.
    /// </summary>
    /// <param name="descriptor">Complete description of the pipeline to create</param>
    /// <returns>Vulkan pipeline handle, or error pipeline on failure</returns>
    /// <exception cref="ArgumentNullException">If descriptor is null</exception>
    /// <remarks>
    /// <para>Pipelines are expensive to create (~10-50ms). This method:</para>
    /// <list type="number">
    /// <item>Computes hash from descriptor</item>
    /// <item>Returns cached pipeline if exists</item>
    /// <item>Creates and caches new pipeline if not found</item>
    /// <item>Returns fallback "error" pipeline on creation failure</item>
    /// </list>
    /// 
    /// <para>The descriptor must include:</para>
    /// <list type="bullet">
    /// <item>Shader paths (vertex + fragment required)</item>
    /// <item>Vertex input description (layout and bindings)</item>
    /// <item>Topology (point list, line list, triangle list, etc.)</item>
    /// <item>Target render pass (for compatibility)</item>
    /// <item>Optional: blend state, depth state, rasterization state</item>
    /// </list>
    /// </remarks>
    Pipeline GetOrCreatePipeline(PipelineDescriptor descriptor);

    /// <summary>
    /// Creates a new pipeline builder for fluent pipeline configuration.
    /// </summary>
    /// <returns>A new pipeline builder instance.</returns>
    /// <remarks>
    /// Use the builder to configure a pipeline fluently:
    /// <code>
    /// var pipeline = pipelineManager.GetBuilder()
    ///     .WithShader(shader)
    ///     .WithRenderPass(renderPass)
    ///     .WithTopology(PrimitiveTopology.TriangleFan)
    ///     .Build();
    /// </code>
    /// </remarks>
    IPipelineBuilder GetBuilder();

    /// <summary>
    /// Retrieves a handle for the specified pipeline from the cache
    /// or throws <see cref="NotFoundException"/>
    /// </summary>
    /// <param name="name">The name of the pipeline to be retrieved from the cache.</param>
    /// <returns>The specified pipeline handle.</returns>
    Pipeline Get(string name);

    /// <summary>
    /// Invalidates and removes a specific pipeline from the cache.
    /// Pipeline will be recreated on next access.
    /// Thread-safe - safe to call during rendering.
    /// </summary>
    /// <param name="pipelineName">Name of the pipeline to invalidate</param>
    /// <returns>True if pipeline was found and invalidated, false if not found</returns>
    /// <remarks>
    /// Use this when you know a specific pipeline needs to be rebuilt,
    /// such as when its shader files are modified during development.
    /// </remarks>
    bool InvalidatePipeline(string pipelineName);

    /// <summary>
    /// Invalidates all pipelines using the specified shader.
    /// Useful for shader hot-reload scenarios.
    /// Thread-safe - safe to call during rendering.
    /// </summary>
    /// <param name="shaderPath">Path to the shader file that changed</param>
    /// <returns>Number of pipelines invalidated</returns>
    /// <remarks>
    /// When a shader file changes on disk, this method finds all pipelines
    /// that reference it and marks them for recreation. Pipelines are lazily
    /// rebuilt on next access via GetOrCreatePipeline().
    /// </remarks>
    int InvalidatePipelinesUsingShader(string shaderPath);

    /// <summary>
    /// Reloads all shader files and recreates affected pipelines.
    /// Development feature for hot-reload workflow.
    /// Blocking operation - waits for GPU idle before destroying pipelines.
    /// </summary>
    /// <remarks>
    /// <para><strong>Workflow:</strong></para>
    /// <list type="number">
    /// <item>Wait for GPU idle (vkDeviceWaitIdle)</item>
    /// <item>Destroy all existing pipelines</item>
    /// <item>Clear pipeline cache</item>
    /// <item>Pipelines recreated lazily on next access</item>
    /// </list>
    /// 
    /// <para>
    /// This is a heavy operation and should only be used during development.
    /// For production, use InvalidatePipelinesUsingShader() for targeted updates.
    /// </para>
    /// </remarks>
    void ReloadAllShaders();

    /// <summary>
    /// Gets statistics about pipeline usage and cache performance.
    /// Useful for debugging and profiling.
    /// </summary>
    /// <returns>Statistics including cache hits, misses, and active pipeline count</returns>
    PipelineStatistics GetStatistics();

    /// <summary>
    /// Gets information about all currently cached pipelines.
    /// Useful for debugging and editor tools.
    /// </summary>
    /// <returns>Collection of pipeline information (name, shader paths, memory usage)</returns>
    IEnumerable<PipelineInfo> GetAllPipelines();

    /// <summary>
    /// Validates that a pipeline descriptor is compatible with its target render pass.
    /// Checks attachment formats, sample counts, and subpass compatibility.
    /// </summary>
    /// <param name="descriptor">Pipeline descriptor to validate</param>
    /// <returns>True if compatible, false otherwise</returns>
    /// <remarks>
    /// This is called automatically during pipeline creation, but can be used
    /// to validate descriptors before attempting to create expensive pipelines.
    /// </remarks>
    bool ValidatePipelineDescriptor(PipelineDescriptor descriptor);

    /// <summary>
    /// Gets the fallback "error" pipeline used when pipeline creation fails.
    /// Renders geometry in bright pink/magenta for visual debugging.
    /// </summary>
    /// <param name="renderPass">Target render pass</param>
    /// <returns>Error pipeline that renders everything pink</returns>
    Pipeline GetErrorPipeline(RenderPass renderPass);
}
