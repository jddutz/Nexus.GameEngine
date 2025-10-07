using Silk.NET.OpenGL;

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
    /// Compares two render elements for batch priority ordering.
    /// Prioritizes by framebuffer first (off-screen before main), then by shader program.
    /// This minimizes the most expensive state changes.
    /// </summary>
    /// <param name="x">First render element to compare</param>
    /// <param name="y">Second render element to compare</param>
    /// <returns>-1 if x should render before y, 1 if y should render before x, 0 if equal priority</returns>
    public int Compare(ElementData? x, ElementData? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        // Sort by Framebuffer (render off-screen targets first)
        // null framebuffer = default framebuffer = main screen (render last)
        var framebufferComparison = CompareFramebuffers(x.Framebuffer, y.Framebuffer);
        if (framebufferComparison != 0)
            return framebufferComparison;

        // Sort by Priority (layer sets of objects: background, world view, UI)
        if (x.Priority != y.Priority) return x.Priority > y.Priority ? -1 : 1;

        // Sort by Shader Program (group by shader to minimize program switches)
        var shaderComparison = CompareNullableUInt(x.ShaderProgram, y.ShaderProgram);
        if (shaderComparison != 0)
            return shaderComparison;

        // If framebuffer and shader are the same, they should batch together
        // so the specific order doesn't matter much
        return 0;
    }

    /// <summary>
    /// Gets a stable hash code for the render element to enable efficient batch grouping.
    /// Hash includes framebuffer, shader program, VAO, and bound textures.
    /// Elements with the same hash can potentially be batched together.
    /// </summary>
    /// <param name="state">Render element to hash</param>
    /// <returns>Hash code representing the batchable aspects of the render element</returns>
    public int GetHashCode(ElementData state)
    {
        if (state == null)
            return 0;

        return ComputeStateHash(
            state.Framebuffer,
            state.ShaderProgram,
            state.VertexArray,
            state.BoundTextures
        );
    }

    /// <summary>
    /// Computes a stable hash code for the current OpenGL state to facilitate efficient batch grouping.
    /// Queries the GL context for current framebuffer, shader program, VAO, and active textures.
    /// This allows the renderer to detect when GL state changes are needed between batches.
    /// Uses the same hashing algorithm as GetHashCode(ElementData) to ensure consistency.
    /// </summary>
    /// <param name="gl">The OpenGL context to query for current state</param>
    /// <returns>Hash code representing the current GL state that affects batching</returns>
    public int GetHashCode(GL gl)
    {
        // Query current framebuffer (most expensive state change)
        gl.GetInteger(GLEnum.FramebufferBinding, out int currentFramebuffer);
        var framebuffer = currentFramebuffer == 0 ? (uint?)null : (uint)currentFramebuffer;

        // Query current shader program (second most expensive)
        gl.GetInteger(GLEnum.CurrentProgram, out int currentProgram);
        var shaderProgram = currentProgram == 0 ? (uint?)null : (uint)currentProgram;

        // Query current vertex array object
        gl.GetInteger(GLEnum.VertexArrayBinding, out int currentVAO);
        var vertexArray = currentVAO == 0 ? (uint?)null : (uint)currentVAO;

        // Query active texture unit and bound textures
        gl.GetInteger(GLEnum.ActiveTexture, out int activeTextureUnit);
        int originalTextureSlot = activeTextureUnit - (int)GLEnum.Texture0;

        // Create texture array matching ElementData.BoundTextures size
        var boundTextures = new uint?[16]; // Match ElementData.BoundTextures length

        // Query all texture units to match ElementData behavior
        for (int i = 0; i < boundTextures.Length; i++)
        {
            gl.ActiveTexture(TextureUnit.Texture0 + i);
            gl.GetInteger(GLEnum.TextureBinding2D, out int boundTexture);
            boundTextures[i] = boundTexture == 0 ? (uint?)null : (uint)boundTexture;
        }

        // Restore original active texture unit
        gl.ActiveTexture((TextureUnit)(GLEnum.Texture0 + originalTextureSlot));

        return ComputeStateHash(framebuffer, shaderProgram, vertexArray, boundTextures);
    }

    /// <summary>
    /// Computes a consistent hash code for the given GL state parameters.
    /// This method ensures that both GetHashCode(ElementData) and GetHashCode(GL) 
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