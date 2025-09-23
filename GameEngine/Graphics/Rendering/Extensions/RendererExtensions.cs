using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Numerics;
using Nexus.GameEngine.Graphics.Rendering.Extensions;

namespace Nexus.GameEngine.Graphics.Rendering.Extensions;

/// <summary>
/// Extension methods for IRenderer that provide lambda-based batching for common rendering operations.
/// These methods use the GetOrCreateBatch() system to efficiently group similar draw calls.
/// </summary>
public static class RendererExtensions
{
    /// <summary>
    /// Draws a 3D mesh using lambda-based batching.
    /// Automatically batches similar rendering state for optimal performance.
    /// </summary>
    /// <param name="renderer">The renderer instance</param>
    /// <param name="vao">Vertex Array Object identifier</param>
    /// <param name="indexCount">Number of indices to draw</param>
    /// <param name="transform">World transformation matrix</param>
    /// <param name="textureId">OpenGL texture identifier</param>
    /// <param name="shaderId">OpenGL shader program identifier</param>
    /// <param name="blendMode">Blending mode for transparency</param>
    public static void DrawMesh(this IRenderer renderer, uint vao, int indexCount,
        Matrix4x4 transform, uint textureId, uint shaderId, BlendingMode blendMode)
    {
        // Use tuple-based batch key for state grouping
        var batchKey = (shaderId, textureId, blendMode, vao);

        renderer.GetOrCreateBatch(batchKey).Draw(() =>
        {
            var gl = renderer.GL;

            // Set shader program
            gl.UseProgram(shaderId);

            // Set texture
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, textureId);

            // Set transform uniform (assumes "uTransform" uniform name)
            var location = gl.GetUniformLocation(shaderId, "uTransform");
            if (location != -1)
            {
                // Convert Matrix4x4 to span for Silk.NET
                Span<float> matrixData = stackalloc float[16];
                System.Runtime.InteropServices.MemoryMarshal.Write(
                    System.Runtime.InteropServices.MemoryMarshal.AsBytes(matrixData),
                    in transform);
                gl.UniformMatrix4(location, 1, false, matrixData);
            }

            // Configure blending
            ConfigureBlending(gl, blendMode);

            // Draw the mesh
            gl.BindVertexArray(vao);
            var offset = 0;
            gl.DrawElements(PrimitiveType.Triangles, (uint)indexCount, DrawElementsType.UnsignedInt, ref offset);
        });
    }

    /// <summary>
    /// Draws a 2D sprite using lambda-based batching.
    /// Sprites are rendered as textured quads with support for transformation and tinting.
    /// </summary>
    /// <param name="renderer">The renderer instance</param>
    /// <param name="textureId">OpenGL texture identifier</param>
    /// <param name="position">World position of the sprite</param>
    /// <param name="size">Size of the sprite in world units</param>
    /// <param name="rotation">Rotation in radians (default: 0)</param>
    /// <param name="tint">Color tint to apply (default: white)</param>
    public static void DrawSprite(this IRenderer renderer, uint textureId,
        Vector3 position, Vector2 size, float rotation = 0f, Vector4 tint = default)
    {
        if (tint == default) tint = Vector4.One; // Default to white

        // Get default sprite shader (assumes it exists in shared resources)
        var shaderId = renderer.GetSharedResource<uint>("SpriteShader");
        if (shaderId == 0)
        {
            // Fall back to a basic shader if sprite shader not available
            shaderId = renderer.GetSharedResource<uint>("BasicShader");
        }

        var batchKey = (shaderId, textureId, BlendingMode.Alpha, "SpriteQuad");

        renderer.GetOrCreateBatch(batchKey).Draw(() =>
        {
            var gl = renderer.GL;

            // Calculate transformation matrix
            var transform = Matrix4x4.CreateScale(size.X, size.Y, 1.0f) *
                           Matrix4x4.CreateRotationZ(rotation) *
                           Matrix4x4.CreateTranslation(position);

            // Set shader program
            gl.UseProgram(shaderId);

            // Set texture
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, textureId);

            // Set transform uniform
            var transformLocation = gl.GetUniformLocation(shaderId, "uTransform");
            if (transformLocation != -1)
            {
                Span<float> matrixData = stackalloc float[16];
                System.Runtime.InteropServices.MemoryMarshal.Write(
                    System.Runtime.InteropServices.MemoryMarshal.AsBytes(matrixData),
                    in transform);
                gl.UniformMatrix4(transformLocation, 1, false, matrixData);
            }

            // Set tint uniform
            var tintLocation = gl.GetUniformLocation(shaderId, "uTint");
            if (tintLocation != -1)
            {
                gl.Uniform4(tintLocation, tint.X, tint.Y, tint.Z, tint.W);
            }

            // Configure alpha blending for sprites
            ConfigureBlending(gl, BlendingMode.Alpha);

            // Get or create quad VAO
            var quadVAO = renderer.GetSharedResource<uint>("SpriteQuad");
            if (quadVAO == 0)
            {
                quadVAO = CreateQuadVAO(gl);
                renderer.SetSharedResource("SpriteQuad", quadVAO);
            }

            // Draw the sprite quad
            gl.BindVertexArray(quadVAO);
            var offset = 0;
            gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, ref offset);
        });
    }

    /// <summary>
    /// Draws a UI element using lambda-based batching.
    /// UI elements are rendered with depth sorting and screen-space coordinates.
    /// </summary>
    /// <param name="renderer">The renderer instance</param>
    /// <param name="textureId">OpenGL texture identifier</param>
    /// <param name="screenPos">Screen position (0,0 = top-left)</param>
    /// <param name="size">Size in screen pixels</param>
    /// <param name="depth">Depth for sorting (lower values render first)</param>
    /// <param name="tint">Color tint to apply</param>
    public static void DrawUI(this IRenderer renderer, uint textureId,
        Vector2 screenPos, Vector2 size, float depth, Vector4 tint)
    {
        // Get UI shader (assumes it exists in shared resources)
        var shaderId = renderer.GetSharedResource<uint>("UIShader");
        if (shaderId == 0)
        {
            shaderId = renderer.GetSharedResource<uint>("BasicShader");
        }

        // Use depth as part of batch key for sorting
        var batchKey = ("UI", depth, textureId, shaderId);

        renderer.GetOrCreateBatch(batchKey).Draw(() =>
        {
            var gl = renderer.GL;

            // UI elements use orthographic projection
            // Convert screen coordinates to normalized device coordinates
            // This assumes the viewport size is available somehow

            // Set shader program
            gl.UseProgram(shaderId);

            // Set texture
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, textureId);

            // Set screen position and size uniforms
            var posLocation = gl.GetUniformLocation(shaderId, "uScreenPos");
            if (posLocation != -1)
            {
                gl.Uniform2(posLocation, screenPos.X, screenPos.Y);
            }

            var sizeLocation = gl.GetUniformLocation(shaderId, "uScreenSize");
            if (sizeLocation != -1)
            {
                gl.Uniform2(sizeLocation, size.X, size.Y);
            }

            var depthLocation = gl.GetUniformLocation(shaderId, "uDepth");
            if (depthLocation != -1)
            {
                gl.Uniform1(depthLocation, depth);
            }

            // Set tint uniform
            var tintLocation = gl.GetUniformLocation(shaderId, "uTint");
            if (tintLocation != -1)
            {
                gl.Uniform4(tintLocation, tint.X, tint.Y, tint.Z, tint.W);
            }

            // Configure alpha blending for UI
            ConfigureBlending(gl, BlendingMode.Alpha);

            // Get or create quad VAO
            var quadVAO = renderer.GetSharedResource<uint>("UIQuad");
            if (quadVAO == 0)
            {
                quadVAO = CreateQuadVAO(gl);
                renderer.SetSharedResource("UIQuad", quadVAO);
            }

            // Draw the UI quad
            gl.BindVertexArray(quadVAO);
            var offset = 0;
            gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, ref offset);
        });
    }

    /// <summary>
    /// Configures OpenGL blending state for the specified blending mode.
    /// </summary>
    /// <param name="gl">OpenGL context</param>
    /// <param name="blendMode">Blending mode to configure</param>
    private static void ConfigureBlending(GL gl, BlendingMode blendMode)
    {
        switch (blendMode)
        {
            case BlendingMode.None:
                gl.Disable(EnableCap.Blend);
                break;
            case BlendingMode.Alpha:
                gl.Enable(EnableCap.Blend);
                gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
            case BlendingMode.Additive:
                gl.Enable(EnableCap.Blend);
                gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                break;
            case BlendingMode.Multiply:
                gl.Enable(EnableCap.Blend);
                gl.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
                break;
            case BlendingMode.PremultipliedAlpha:
                gl.Enable(EnableCap.Blend);
                gl.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
                break;
            case BlendingMode.Subtract:
                gl.Enable(EnableCap.Blend);
                gl.BlendEquation(BlendEquationModeEXT.FuncReverseSubtract);
                gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                break;
            default:
                gl.Disable(EnableCap.Blend);
                break;
        }
    }

    /// <summary>
    /// Creates a unit quad VAO for sprite and UI rendering.
    /// The quad is centered at origin with vertices from -0.5 to 0.5.
    /// </summary>
    /// <param name="gl">OpenGL context</param>
    /// <returns>Vertex Array Object identifier</returns>
    private static uint CreateQuadVAO(GL gl)
    {
        // Quad vertices: position (x, y, z) + texture coordinates (u, v)
        float[] vertices = [
            // Position        // TexCoords
            -0.5f, -0.5f, 0.0f,  0.0f, 0.0f, // Bottom-left
             0.5f, -0.5f, 0.0f,  1.0f, 0.0f, // Bottom-right
             0.5f,  0.5f, 0.0f,  1.0f, 1.0f, // Top-right
            -0.5f,  0.5f, 0.0f,  0.0f, 1.0f  // Top-left
        ];

        uint[] indices = [
            0, 1, 2, // First triangle
            2, 3, 0  // Second triangle
        ];

        // Create VAO, VBO, and EBO
        uint vao = gl.GenVertexArray();
        uint vbo = gl.GenBuffer();
        uint ebo = gl.GenBuffer();

        gl.BindVertexArray(vao);

        // Upload vertex data
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        gl.BufferData<float>(BufferTargetARB.ArrayBuffer, vertices, BufferUsageARB.StaticDraw);

        // Upload index data
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        gl.BufferData<uint>(BufferTargetARB.ElementArrayBuffer, indices, BufferUsageARB.StaticDraw);

        // Set vertex attributes
        // Position attribute (location 0)
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        gl.EnableVertexAttribArray(0);

        // Texture coordinate attribute (location 1)
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        gl.EnableVertexAttribArray(1);

        // Unbind VAO
        gl.BindVertexArray(0);

        return vao;
    }
}