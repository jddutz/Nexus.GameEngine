using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using System.Collections.Concurrent;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Runtime;

namespace Nexus.GameEngine.Graphics;

/// <summary>
/// Provides rendering functionality for the component tree, traversing all <see cref="IRenderable"/> components and invoking their <c>OnRender()</c> methods.
/// Components are processed in <see cref="IRenderable.RenderPriority"/> order using a sophisticated batching system to minimize OpenGL state changes.
/// 
/// <para>The renderer implements efficient batch processing by:</para>
/// <list type="bullet">
/// <item>Collecting <see cref="RenderState"/> requirements from all renderable components</item>
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
    IBatchStrategy? batchStrategy = null
    )
    : IRenderer, IDisposable
{
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

    public IBatchStrategy BatchStrategy { get; set; } = batchStrategy ?? new DefaultBatchStrategy();

    /// <summary>
    /// Gets or sets the root <see cref="IRuntimeComponent"/> of the component tree to render.
    /// The renderer traverses this tree to locate and render all <see cref="IRenderable"/> components.
    /// </summary>
    public IRuntimeComponent? RootComponent { get; set; }

    /// <summary>
    /// Gets or sets the configured render passes that define the rendering pipeline stages.
    /// Each pass can configure different OpenGL state (e.g., depth testing, blending) before rendering components.
    /// By default, includes a single "Main" pass with alpha blending and depth testing enabled.
    /// </summary>
    public RenderPassConfiguration[] RenderPasses { get; set; } = [
            new RenderPassConfiguration
            {
                Id = 0,
                Name = "Main",
                DirectRenderMode = true,
                DepthTestEnabled = true,
                BlendingMode = BlendingMode.Alpha
            }
        ];

    /// <summary>
    /// Gets the shared resources dictionary for caching and sharing rendering resources across components.
    /// Used by <c>GLRenderingExtensions</c> and components for efficient resource management.
    /// Thread-safe for concurrent access during rendering.
    /// </summary>
    public ConcurrentDictionary<string, object> SharedResources { get; init; } = new();

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
    /// Renders a frame by traversing the component tree and invoking <c>OnRender()</c> on all enabled and visible <see cref="IRenderable"/> components.
    /// Uses an efficient batching system to minimize OpenGL state changes:
    /// 
    /// <para>Rendering Pipeline:</para>
    /// <list type="number">
    /// <item>Traverse component tree to find all <see cref="IRenderable"/> components</item>
    /// <item>Collect <see cref="RenderState"/> requirements from each component's OnRender() method</item>
    /// <item>Sort render states using <see cref="IBatchStrategy"/> to group compatible states</item>
    /// <item>Process render states in batches, applying OpenGL state changes only when batch hash codes differ</item>
    /// <item>Query actual GL state before each update to avoid redundant API calls</item>
    /// </list>
    /// 
    /// <para>This approach significantly reduces expensive GL state transitions such as framebuffer switches, shader program changes, and texture bindings.</para>
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last frame, used for animations and time-based rendering</param>
    public void RenderFrame(double deltaTime)
    {
        if (_disposed)
        {
            logger.LogError("Attempted to render frame on disposed renderer");
            return;
        }

        if (BatchStrategy == null)
        {
            logger.LogError("BatchStrategy is required but currently null");
            return;
        }

        if (RootComponent == null)
        {
            logger.LogWarning("No root component set, nothing to render");
            return;
        }

        // Walk the component tree and collect requirements from components
        var renderStates = FindRenderableComponents(RootComponent)
            .Where(component => component.IsEnabled && component.IsVisible)
            .SelectMany(c => c.OnRender(deltaTime));

        // Sort the requirements
        var sorted = new SortedSet<RenderState>(BatchStrategy);
        foreach (var state in renderStates) sorted.Add(state);

        var errorCodes = new List<GLEnum>();
        int currentHashCode = BatchStrategy.GetHashCode(GL);

        foreach (var targetState in sorted)
        {
            int targetHashCode = BatchStrategy.GetHashCode(targetState);

            // Apply state changes only when batch changes (hash code differs)
            if (currentHashCode != targetHashCode)
            {
                // Apply all necessary GL state changes for the new batch
                UpdateFramebuffer(targetState.Framebuffer);
                UpdateShaderProgram(targetState.ShaderProgram);
                UpdateVertexArray(targetState.VertexArray);
                UpdateTextures(targetState.BoundTextures);

                // Process render passes for this batch
                foreach (var renderPass in RenderPasses)
                {
                    GL.ClearColor(renderPass.FillColor);
                    GL.Clear((uint)ClearBufferMask.ColorBufferBit);

                    // TODO: define and implement render pass behavior
                    // This will be implemented in a future iteration
                    logger.LogTrace("Processing render pass: {PassName}", renderPass.Name);
                }

                currentHashCode = targetHashCode;
            }

            // Check for GL errors after state changes
            var errorCode = GL.GetError();
            if (errorCode != GLEnum.NoError)
            {
                logger.LogDebug("GL Error Code {ErrorCode} for render state with hash {HashCode}",
                    errorCode, targetHashCode);
                errorCodes.Add(errorCode);

                // TODO: Adaptive degradation to address GL errors
                // TODO: Handle critical errors (OutOfMemory, etc)
                // These will be implemented in future iterations
            }
        }

        // Log performance metrics if we encountered any errors
        if (errorCodes.Count > 0)
        {
            logger.LogWarning("Encountered {ErrorCount} GL errors during frame rendering", errorCodes.Count);
        }
    }

    /// <summary>
    /// Enumerates all <see cref="IRenderable"/> components in the given component tree, recursively.
    /// </summary>
    /// <param name="component">The root component to search from.</param>
    /// <returns>An enumerable of all <see cref="IRenderable"/> components found in the tree.</returns>
    private IEnumerable<IRenderable> FindRenderableComponents(IRuntimeComponent component)
    {
        if (component is null)
            yield break;

        if (component is IRenderable renderable)
            yield return renderable;

        foreach (var child in component.Children.SelectMany(c => FindRenderableComponents(c)))
            yield return child;
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
            SharedResources.Clear();
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
