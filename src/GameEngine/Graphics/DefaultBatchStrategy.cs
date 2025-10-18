namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Default batching strategy that optimizes OpenGL state changes by grouping render states
/// based on expensive state transitions (framebuffer, shader program, textures, VAO).
/// Prioritizes batches to minimize the most expensive state changes first.
/// 
/// <para>Optimization Strategy:</para>
/// <list type="bullet">
/// <item><b>Framebuffer binding</b> - Most expensive; off-screen targets rendered before main framebuffer</item>
/// <item><b>Render priority</b> - Secondary sort for layering (background → 3D → UI)</item>
/// <item><b>Shader program</b> - Third priority; groups objects using same shaders</item>
/// <item><b>Textures and VAO</b> - Included in hash for fine-grained batching</item>
/// </list>
/// 
/// <para>Hash-based change detection allows the renderer to detect when GL state updates are needed between consecutive render states.</para>
/// </summary>
public class DefaultBatchStrategy : IBatchStrategy
{
    /// <summary>
    /// Compares two draw commands for batch priority ordering.
    /// Sorts by render priority first (correctness), then by state changes (performance).
    /// This ensures correct layering while minimizing Vulkan state changes.
    /// </summary>
    /// <param name="x">First draw command to compare</param>
    /// <param name="y">Second draw command to compare</param>
    /// <returns>-1 if x should render before y, 1 if y should render before x, 0 if equal priority</returns>
    public int Compare(DrawCommand x, DrawCommand y)
    {
        // Sort by render priority first (lower priority renders first)
        // This ensures correct layering (e.g., background → scene → UI)
        var priorityCompare = x.RenderPriority.CompareTo(y.RenderPriority);
        if (priorityCompare != 0) return priorityCompare;
        
        // Within the same priority, optimize for batching to minimize state changes
        
        // Sort by pipeline (most expensive to change)
        var pipelineCompare = x.Pipeline.Pipeline.Handle.CompareTo(y.Pipeline.Pipeline.Handle);
        if (pipelineCompare != 0) return pipelineCompare;
        
        // Then by descriptor set (textures/uniforms)
        var descriptorCompare = x.DescriptorSet.Handle.CompareTo(y.DescriptorSet.Handle);
        if (descriptorCompare != 0) return descriptorCompare;
        
        // Then by vertex buffer
        var vertexCompare = x.VertexBuffer.Handle.CompareTo(y.VertexBuffer.Handle);
        if (vertexCompare != 0) return vertexCompare;
        
        // Finally by index buffer
        return x.IndexBuffer.Handle.CompareTo(y.IndexBuffer.Handle);
    }

    /// <summary>
    /// Gets a stable hash code for the draw command to enable efficient batch grouping.
    /// Hash encodes render priority and state change costs.
    /// Commands with similar hashes will batch together.
    /// Note: DepthSortKey is NOT included as it changes per-frame based on camera position.
    /// </summary>
    /// <param name="state">Draw command to hash</param>
    /// <returns>Hash code representing the batchable aspects of the draw command</returns>
    public int GetHashCode(DrawCommand state)
    {
        var hash = new HashCode();
        
        // Add RenderPriority first (correctness requirement)
        hash.Add(state.RenderPriority);
        
        // Then add in order of state change cost (most expensive first)
        hash.Add(state.Pipeline);
        hash.Add(state.DescriptorSet);
        hash.Add(state.VertexBuffer);
        hash.Add(state.IndexBuffer);
        
        // NOTE: DepthSortKey deliberately excluded - it's camera-relative and changes every frame
        
        return hash.ToHashCode();
    }

    /// <summary>
    /// Computes a consistent hash code for the given GL state parameters.
    /// This method ensures that both GetHashCode(DrawCommand) and GetHashCode(GL) 
    /// produce identical hash codes when the states are equivalent.
    /// </summary>
    /// <param name="framebuffer">Framebuffer ID or null for default framebuffer</param>
    /// <param name="shaderProgram">Shader program ID or null for no program</param>
    /// <param name="vertexArray">Vertex array object ID or null for no VAO</param>
    /// <param name="boundTextures">Array of bound texture IDs for each texture unit</param>
    /// <returns>Consistent hash code for the given state parameters</returns>
    private static int ComputeStateHash(uint? framebuffer, uint? shaderProgram, uint? vertexArray, uint?[] boundTextures)
    {
        var hash = new HashCode();

        // Include framebuffer (most important for batching)
        hash.Add(framebuffer);

        // Include shader program (second most important)
        hash.Add(shaderProgram);

        // Include VAO
        hash.Add(vertexArray);

        // Include all bound textures in order
        foreach (var texture in boundTextures)
        {
            hash.Add(texture);
        }

        return hash.ToHashCode();
    }    /// <summary>
         /// Compares framebuffer IDs with special handling for the default framebuffer.
         /// Off-screen framebuffers (non-null) should render before the default framebuffer (null).
         /// </summary>
    private static int CompareFramebuffers(uint? framebuffer1, uint? framebuffer2)
    {
        // Both are default framebuffer
        if (framebuffer1 == null && framebuffer2 == null)
            return 0;

        // framebuffer1 is default, framebuffer2 is off-screen -> framebuffer2 renders first
        if (framebuffer1 == null)
            return 1;

        // framebuffer2 is default, framebuffer1 is off-screen -> framebuffer1 renders first
        if (framebuffer2 == null)
            return -1;

        // Both are off-screen framebuffers, sort by ID for consistency
        return framebuffer1.Value.CompareTo(framebuffer2.Value);
    }

    /// <summary>
    /// Compares two nullable uint values, treating null as "less than" any actual value.
    /// </summary>
    private static int CompareNullableUInt(uint? value1, uint? value2)
    {
        if (value1 == null && value2 == null) return 0;
        if (value1 == null) return -1;
        if (value2 == null) return 1;
        return value1.Value.CompareTo(value2.Value);
    }
}