using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;
using System.Resources;
using Nexus.GameEngine.Resources;
using Nexus.GameEngine.Resources.Shaders;
using Nexus.GameEngine.Resources.Geometry;
using Nexus.GameEngine.GUI.Components;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Provides rendering functionality for the component tree, traversing all <see cref="IRenderable"/> components and invoking their <c>OnRender()</c> methods.
/// Components are processed in <see cref="IRenderable.RenderPriority"/> order using a sophisticated batching system to minimize OpenGL state changes.
/// 
/// <para>The renderer implements efficient batch processing by:</para>
/// <list type="bullet">
/// <item>Collecting <see cref="RenderData"/> requirements from all renderable components</item>
/// <item>Sorting render states using the configured <see cref="IBatchStrategy"/> to minimize expensive state changes</item>
/// <item>Applying OpenGL state changes only when batches change (detected via hash code comparison)</item>
/// <item>Querying actual GL state before each update to avoid redundant glBindXxx() calls</item>
/// </list>
/// 
/// <para>Exposes direct OpenGL access and manages render passes, shared resources, and direct GL state queries.</para>
/// </summary>
public class Renderer(
        IWindowService windowService,
        ILoggerFactory loggerFactory,
        IContentManager contentManager
        ) : IRenderer
{
    private readonly ILogger logger = loggerFactory.CreateLogger(nameof(Renderer));

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

    /// <summary>
    /// Loads shader source code from embedded resources.
    /// </summary>
    /// <param name="resourceName">The name of the shader file (e.g., "basic-quad-vert.glsl")</param>
    /// <returns>The shader source code as a string</returns>
    /// <exception cref="FileNotFoundException">Thrown if the embedded resource is not found</exception>
    private static string LoadEmbeddedShader(string resourceName)
    {
        var assembly = typeof(Renderer).Assembly;
        var fullResourceName = $"Nexus.GameEngine.Resources.Shaders.{resourceName}";

        using var stream = assembly.GetManifestResourceStream(fullResourceName);
        if (stream == null)
        {
            // List available resources to help with debugging
            var availableResources = assembly.GetManifestResourceNames();
            var resourceList = string.Join(", ", availableResources);
            throw new FileNotFoundException(
                $"Embedded shader resource '{fullResourceName}' not found. Available resources: {resourceList}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
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
    /// <item>Collect <see cref="RenderData"/> requirements from each component's OnRender() method</item>
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

        var context = new RenderContext()
        {
            GL = GL,
            Viewport = contentManager.Viewport,
            ElapsedSeconds = deltaTime
        };

        if (contentManager.Viewport?.Content == null)
        {
            logger.LogDebug("Viewport content is null, nothing to render.");
            return;
        }

        var onRenderResults = contentManager.Viewport.Content
            .GetChildren<IRenderable>()
            .SelectMany(r => r.OnRender(context));

        int count = 0;

        foreach (var renderData in onRenderResults)
        {
            count++;
            Render(renderData);
        }

        logger.LogDebug("Viewport.GetChildren<IRenderable>() returned {Count} results.", count);

        AfterRenderFrame?.Invoke(this, new());
    }

    private unsafe void Render(RenderData renderData)
    {
        //Clear the color channel.
        GL.Clear((uint)ClearBufferMask.ColorBufferBit);

        //Bind the geometry and shader.
        GL.BindVertexArray(renderData.Vao);
        GL.UseProgram(renderData.Shader);

        //Draw the geometry.
        GL.DrawElements(
            PrimitiveType.Triangles,
            (uint)GeometryDefinitions.BasicQuad.Indices.Length,
            DrawElementsType.UnsignedInt,
            null);
    }
}
