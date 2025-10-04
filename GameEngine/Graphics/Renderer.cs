using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Provides rendering functionality for the component tree, traversing all <see cref="IRenderable"/> components and invoking their <c>OnRender()</c> methods.
/// Components are processed in <see cref="IRenderable.RenderPriority"/> order using a sophisticated batching system to minimize OpenGL state changes.
/// 
/// <para>The renderer implements efficient batch processing by:</para>
/// <list type="bullet">
/// <item>Collecting <see cref="GLState"/> requirements from all renderable components</item>
/// <item>Sorting render states using the configured <see cref="IBatchStrategy"/> to minimize expensive state changes</item>
/// <item>Applying OpenGL state changes only when batches change (detected via hash code comparison)</item>
/// <item>Querying actual GL state before each update to avoid redundant glBindXxx() calls</item>
/// </list>
/// 
/// <para>Exposes direct OpenGL access and manages render passes, shared resources, and direct GL state queries.</para>
/// </summary>
public class Renderer(
        IWindowService windowService,
        ILogger<Renderer> logger,
        IBatchStrategy batchStrategy
        ) : IRenderer, IDisposable
{
    private static uint Vbo;
    private static uint Ebo;
    private static uint Vao;
    private static uint Shader;

    //Vertex shaders are run on each vertex.
    private static readonly string VertexShaderSource = @"
        #version 330 core //Using version GLSL version 3.3
        layout (location = 0) in vec4 vPos;
        
        void main()
        {
            gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";

    //Fragment shaders are run on each fragment/pixel of the geometry.
    private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
        }
        ";

    //Vertex data, uploaded to the VBO.
    private static readonly float[] Vertices =
        {
            //X    Y      Z
             0.5f,  0.5f, 0.0f,
             0.5f, -0.5f, 0.0f,
            -0.5f, -0.5f, 0.0f,
            -0.5f,  0.5f, 0.5f
        };

    //Index data, uploaded to the EBO.
    private static readonly uint[] Indices =
        {
            0, 1, 3,
            1, 2, 3
        };

    /// <summary>
    /// Gets the Silk.NET OpenGL interface for use by components during rendering.
    /// Lazily initializes the GL context on first access using the window service.
    /// </summary>
    private GL? _gl;
    public GL GL
    {
        get
        {
            _gl ??= windowService.GetOrCreateWindow().CreateOpenGL();
            return _gl;
        }

        private set
        {
            _gl = value;
        }
    }

    /// <summary>
    /// Gets or sets the root <see cref="IRuntimeComponent"/> of the component tree to render.
    /// The renderer traverses this tree to locate and render all <see cref="IRenderable"/> components.
    /// </summary>
    public IViewport Viewport { get; init; } = new Viewport();

    /// <summary>
    /// Updates the current framebuffer binding to match the target render state.
    /// Queries the actual GL state and only performs the GL call if the framebuffer has actually changed.
    /// </summary>
    /// <param name="targetFramebuffer">Target framebuffer ID, or null for default framebuffer</param>
    private void UpdateFramebuffer(uint? targetFramebuffer)
    {
        // Query current GL framebuffer state
        GL.GetInteger(GLEnum.FramebufferBinding, out int currentFramebuffer);

        // Convert target to expected GL value (null = 0 for default framebuffer)
        uint targetFramebufferValue = targetFramebuffer ?? 0;

        if (currentFramebuffer == targetFramebufferValue) return;

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, targetFramebufferValue);

        var errorCode = GL.GetError();
        if (errorCode != GLEnum.NoError)
        {
            logger.LogWarning("GL Error when binding framebuffer {FramebufferId}: {ErrorCode}",
                targetFramebuffer, errorCode);
        }
    }

    /// <summary>
    /// Updates the current shader program to match the target render state.
    /// Queries the actual GL state and only performs the GL call if the shader program has actually changed.
    /// </summary>
    /// <param name="targetProgram">Target shader program ID, or null for no program</param>
    private void UpdateShaderProgram(uint? targetProgram)
    {
        // Query current GL shader program state
        GL.GetInteger(GLEnum.CurrentProgram, out int currentProgram);

        // Convert target to expected GL value (null = 0 for no program)
        uint targetProgramValue = targetProgram ?? 0;

        if (currentProgram == targetProgramValue) return;

        GL.UseProgram(targetProgramValue);

        var errorCode = GL.GetError();
        if (errorCode != GLEnum.NoError)
        {
            logger.LogWarning("GL Error when using shader program {ProgramId}: {ErrorCode}",
                targetProgram, errorCode);
        }
    }

    /// <summary>
    /// Updates the current vertex array object binding to match the target render state.
    /// Queries the actual GL state and only performs the GL call if the VAO has actually changed.
    /// </summary>
    /// <param name="targetVAO">Target vertex array object ID, or null for no VAO</param>
    private void UpdateVertexArray(uint? targetVAO)
    {
        // Query current GL vertex array state
        GL.GetInteger(GLEnum.VertexArrayBinding, out int currentVAO);

        // Convert target to expected GL value (null = 0 for no VAO)
        uint targetVAOValue = targetVAO ?? 0;

        if (currentVAO == targetVAOValue) return;

        GL.BindVertexArray(targetVAOValue);

        var errorCode = GL.GetError();
        if (errorCode != GLEnum.NoError)
        {
            logger.LogWarning("GL Error when binding vertex array {VAOId}: {ErrorCode}",
                targetVAO, errorCode);
        }
    }

    /// <summary>
    /// Updates texture bindings to match the target render state.
    /// Queries the actual GL state for each texture unit and only updates slots that have changed.
    /// </summary>
    /// <param name="targetTextures">Target texture array with IDs for each texture unit</param>
    private void UpdateTextures(uint?[] targetTextures)
    {
        // Query current active texture unit
        GL.GetInteger(GLEnum.ActiveTexture, out int currentActiveTexture);
        int originalTextureSlot = currentActiveTexture - (int)GLEnum.Texture0;

        for (int slot = 0; slot < Math.Min(targetTextures.Length, 16); slot++) // Max 16 texture units
        {
            var targetTexture = targetTextures[slot];
            uint targetTextureValue = targetTexture ?? 0;

            // Set active texture unit for this slot
            GL.ActiveTexture(TextureUnit.Texture0 + slot);

            // Query current texture binding for this slot
            GL.GetInteger(GLEnum.TextureBinding2D, out int currentTexture);

            if (currentTexture != targetTextureValue)
            {
                GL.BindTexture(TextureTarget.Texture2D, targetTextureValue);

                var errorCode = GL.GetError();
                if (errorCode != GLEnum.NoError)
                {
                    logger.LogWarning("GL Error when binding texture {TextureId} to slot {Slot}: {ErrorCode}",
                        targetTexture, slot, errorCode);
                }
            }
        }

        // Restore original active texture unit
        GL.ActiveTexture((TextureUnit)(GLEnum.Texture0 + originalTextureSlot));
    }

    /// <summary>
    /// Applies uniform values to the currently active shader program.
    /// </summary>
    /// <param name="uniforms">Dictionary of uniform names and values to apply</param>
    private void ApplyUniforms(Dictionary<string, object> uniforms)
    {
        if (uniforms.Count == 0) return;

        logger.LogDebug("Applying {UniformCount} uniforms to current shader", uniforms.Count);
        foreach (var (name, value) in uniforms)
        {
            try
            {
                ApplyUniform(name, value);
                logger.LogDebug("Applied uniform {UniformName} = {Value}", name, value);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to apply uniform {UniformName} with value {Value}", name, value);
            }
        }
    }

    /// <summary>
    /// Applies a single uniform value based on its type.
    /// </summary>
    /// <param name="name">Uniform name</param>
    /// <param name="value">Uniform value</param>
    private void ApplyUniform(string name, object value)
    {
        // Get current program ID
        GL.GetInteger(GLEnum.CurrentProgram, out int currentProgram);
        if (currentProgram == 0)
        {
            logger.LogDebug("No shader program active, cannot set uniform {UniformName}", name);
            return;
        }

        // Get the uniform location
        var location = GL.GetUniformLocation((uint)currentProgram, name);
        if (location == -1)
        {
            logger.LogDebug("Uniform {UniformName} not found in current shader program", name);
            return;
        }

        // Apply uniform based on value type
        switch (value)
        {
            case float floatValue:
                GL.Uniform1(location, floatValue);
                logger.LogDebug("Set uniform {UniformName} (location {Location}) to float {Value}", name, location, floatValue);
                break;
            case int intValue:
                GL.Uniform1(location, intValue);
                logger.LogDebug("Set uniform {UniformName} (location {Location}) to int {Value}", name, location, intValue);
                break;
            case Silk.NET.Maths.Vector4D<float> vec4Value:
                GL.Uniform4(location, vec4Value.X, vec4Value.Y, vec4Value.Z, vec4Value.W);
                logger.LogDebug("Set uniform {UniformName} (location {Location}) to vec4 ({X}, {Y}, {Z}, {W})",
                    name, location, vec4Value.X, vec4Value.Y, vec4Value.Z, vec4Value.W);
                break;
            case Silk.NET.Maths.Vector3D<float> vec3Value:
                GL.Uniform3(location, vec3Value.X, vec3Value.Y, vec3Value.Z);
                logger.LogDebug("Set uniform {UniformName} (location {Location}) to vec3 ({X}, {Y}, {Z})",
                    name, location, vec3Value.X, vec3Value.Y, vec3Value.Z);
                break;
            case Silk.NET.Maths.Vector2D<float> vec2Value:
                GL.Uniform2(location, vec2Value.X, vec2Value.Y);
                logger.LogDebug("Set uniform {UniformName} (location {Location}) to vec2 ({X}, {Y})",
                    name, location, vec2Value.X, vec2Value.Y);
                break;
            default:
                logger.LogWarning("Unsupported uniform type {UniformType} for uniform {UniformName}",
                    value.GetType().Name, name);
                break;
        }
    }

    public unsafe void OnLoad()
    {
        //Creating a vertex array.
        Vao = GL.GenVertexArray();
        GL.BindVertexArray(Vao);

        //Initializing a vertex buffer that holds the vertex data.
        Vbo = GL.GenBuffer(); //Creating the buffer.
        GL.BindBuffer(BufferTargetARB.ArrayBuffer, Vbo); //Binding the buffer.
        fixed (void* v = &Vertices[0])
        {
            GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(Vertices.Length * sizeof(uint)), v, BufferUsageARB.StaticDraw); //Setting buffer data.
        }

        //Initializing a element buffer that holds the index data.
        Ebo = GL.GenBuffer(); //Creating the buffer.
        GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, Ebo); //Binding the buffer.
        fixed (void* i = &Indices[0])
        {
            GL.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(Indices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw); //Setting buffer data.
        }

        //Creating a vertex shader.
        uint vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, VertexShaderSource);
        GL.CompileShader(vertexShader);

        //Checking the shader for compilation errors.
        string infoLog = GL.GetShaderInfoLog(vertexShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            Console.WriteLine($"Error compiling vertex shader {infoLog}");
        }

        //Creating a fragment shader.
        uint fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, FragmentShaderSource);
        GL.CompileShader(fragmentShader);

        //Checking the shader for compilation errors.
        infoLog = GL.GetShaderInfoLog(fragmentShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            Console.WriteLine($"Error compiling fragment shader {infoLog}");
        }

        //Combining the shaders under one shader program.
        Shader = GL.CreateProgram();
        GL.AttachShader(Shader, vertexShader);
        GL.AttachShader(Shader, fragmentShader);
        GL.LinkProgram(Shader);

        //Checking the linking for errors.
        GL.GetProgram(Shader, GLEnum.LinkStatus, out var status);
        if (status == 0)
        {
            Console.WriteLine($"Error linking shader {GL.GetProgramInfoLog(Shader)}");
        }

        //Delete the no longer useful individual shaders;
        GL.DetachShader(Shader, vertexShader);
        GL.DetachShader(Shader, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        //Tell opengl how to give the data to the shaders.
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);
        GL.EnableVertexAttribArray(0);
    }

    public event EventHandler<PreRenderEventArgs>? BeforeRenderFrame;
    public event EventHandler<PostRenderEventArgs>? AfterRenderFrame;

    /// <summary>
    /// Renders a frame by traversing the component tree and invoking <c>OnRender()</c> on all enabled and visible <see cref="IRenderable"/> components.
    /// Uses an efficient batching system to minimize OpenGL state changes:
    /// 
    /// <para>Rendering Pipeline:</para>
    /// <list type="number">
    /// <item>Traverse component tree to find all <see cref="IRenderable"/> components</item>
    /// <item>Collect <see cref="GLState"/> requirements from each component's OnRender() method</item>
    /// <item>Sort render states using <see cref="IBatchStrategy"/> to group compatible states</item>
    /// <item>Process render states in batches, applying OpenGL state changes only when batch hash codes differ</item>
    /// <item>Query actual GL state before each update to avoid redundant API calls</item>
    /// </list>
    /// 
    /// <para>This approach significantly reduces expensive GL state transitions such as framebuffer switches, shader program changes, and texture bindings.</para>
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last frame, used for animations and time-based rendering</param>
    public void OnRender(double deltaTime)
    {
        BeforeRenderFrame?.Invoke(this, new());

        RenderFrame();

        AfterRenderFrame?.Invoke(this, new());
    }

    private unsafe void RenderFrame()
    {
        //Clear the color channel.
        GL.Clear((uint)ClearBufferMask.ColorBufferBit);

        //Bind the geometry and shader.
        GL.BindVertexArray(Vao);
        GL.UseProgram(Shader);

        //Draw the geometry.
        GL.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
    }

    private bool _disposed;

    /// <summary>
    /// Releases all shared resources and disposes the renderer instance.
    /// Safe to call multiple times; subsequent calls have no effect.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            // Clean up all shared resources
            Viewport.Dispose();

            logger.LogDebug("Renderer disposed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during renderer disposal");
        }
        finally
        {
            _disposed = true;
        }
    }
}
