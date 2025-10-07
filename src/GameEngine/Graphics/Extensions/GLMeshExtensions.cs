using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics.Extensions;

/// <summary>
/// Extension methods for GL that provide mesh-related functionality.
/// </summary>
public static class GLMeshExtensions
{
    /// <summary>
    /// Helper method to draw indexed mesh
    /// </summary>
    public static void DrawMesh(this IRenderer renderer, uint vaoId, int indexCount)
    {
        var gl = renderer.GL;

        gl.BindVertexArray(vaoId);
        nint offset = 0;
        gl.DrawElements(PrimitiveType.Triangles, (uint)indexCount, DrawElementsType.UnsignedInt, in offset);
    }

    /// <summary>
    /// Helper method to create mesh with vertices and indices
    /// </summary>
    public static uint CreateMesh(this IRenderer renderer, ReadOnlySpan<float> vertices, ReadOnlySpan<uint> indices)
    {
        var gl = renderer.GL;

        var vao = gl.GenVertexArray();
        var vbo = gl.GenBuffer();
        var ebo = gl.GenBuffer();

        gl.BindVertexArray(vao);

        // Upload vertex data
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        unsafe
        {
            fixed (float* ptr = vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), ptr, BufferUsageARB.StaticDraw);
            }
        }

        // Upload index data
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        unsafe
        {
            fixed (uint* ptr = indices)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), ptr, BufferUsageARB.StaticDraw);
            }
        }

        // Configure vertex attributes (assuming position + texcoord)
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        gl.EnableVertexAttribArray(1);

        gl.BindVertexArray(0);
        return vao;
    }
}