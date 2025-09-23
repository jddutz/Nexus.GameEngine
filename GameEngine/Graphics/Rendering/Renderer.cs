using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using System.Collections.Concurrent;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics.Cameras;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Runtime;

namespace Nexus.GameEngine.Graphics.Rendering;

/// <summary>
/// Basic unified renderer implementation that provides GL access, shared resource management, 
/// and simple render pass orchestration.
/// </summary>
public class Renderer : IRenderer, IDisposable
{
    private readonly IWindowService _windowService;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, object> _sharedResources;
    private readonly RenderPassConfiguration[] _passes;
    private readonly Dictionary<object, DrawBatch> _batches;
    private readonly List<object> _batchExecutionOrder;
    private readonly List<ICamera> _cameras;
    private GL? _gl;
    private bool _disposed;

    /// <summary>
    /// Direct access to Silk.NET OpenGL interface for component rendering.
    /// </summary>
    public GL GL
    {
        get
        {
            if (_gl == null)
            {
                var window = _windowService.GetOrCreateWindow();
                _gl = window.CreateOpenGL();
            }
            return _gl;
        }
    }

    /// <summary>
    /// Collection of draw batches for lambda-based batching support.
    /// Provides read-only access to all batches for debugging and statistics.
    /// </summary>
    public List<DrawBatch> Batches => [.. _batches.Values];

    /// <summary>
    /// Root component for the component tree to render.
    /// The renderer will walk this tree during RenderFrame() to collect draw commands.
    /// Cameras are discovered when this property is set.
    /// </summary>
    public IRuntimeComponent? RootComponent
    {
        get => _rootComponent;
        set
        {
            _rootComponent = value;
            RefreshCameras();
        }
    }
    private IRuntimeComponent? _rootComponent;

    /// <summary>
    /// List of discovered cameras in the component tree.
    /// Refreshed when RootComponent is set or when RefreshCameraList() is called.
    /// </summary>
    public IReadOnlyList<ICamera> Cameras => _cameras.AsReadOnly();

    /// <summary>
    /// Manually refresh the camera list.
    /// Call this when component tree structure changes after initial setup.
    /// </summary>
    public void RefreshCameraList()
    {
        RefreshCameras();
    }

    public Renderer(IWindowService windowService, ILogger<Renderer> logger)
    {
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sharedResources = new ConcurrentDictionary<string, object>();
        _batches = new Dictionary<object, DrawBatch>();
        _batchExecutionOrder = new List<object>();
        _cameras = new List<ICamera>();

        // Initialize with a simple single-pass configuration
        _passes = [
            new RenderPassConfiguration
            {
                Id = 0,
                Name = "Main",
                DirectRenderMode = true,
                DepthTestEnabled = true,
                BlendingMode = BlendingMode.Alpha
            }
        ];

        _logger.LogDebug("Renderer initialized with {PassCount} render passes", _passes.Length);
    }

    /// <summary>
    /// Gets or creates a draw batch for the specified batch key.
    /// Uses object-based keys for maximum flexibility in batching strategies.
    /// </summary>
    /// <param name="batchKey">Unique key identifying the batch (typically tuple of render state)</param>
    /// <returns>Existing or newly created draw batch for the key</returns>
    public DrawBatch GetOrCreateBatch(object batchKey)
    {
        if (!_batches.TryGetValue(batchKey, out var batch))
        {
            batch = new DrawBatch { BatchKey = batchKey };
            _batches[batchKey] = batch;
            _batchExecutionOrder.Add(batchKey);
            _logger.LogTrace("Created new batch for key: {BatchKey}", batchKey);
        }
        return batch;
    }

    /// <summary>
    /// Gets a shared resource by name and type.
    /// </summary>
    public T GetSharedResource<T>(string name)
    {
        if (_sharedResources.TryGetValue(name, out var resource) && resource is T typedResource)
        {
            return typedResource;
        }
        return default(T)!;
    }

    /// <summary>
    /// Sets a shared resource by name.
    /// </summary>
    public void SetSharedResource<T>(string name, T resource)
    {
        if (resource != null)
        {
            _sharedResources[name] = resource;
            _logger.LogTrace("Set shared resource '{Name}' of type {Type}", name, typeof(T).Name);
        }
    }

    /// <summary>
    /// Orchestrates the rendering process through camera-centric multi-pass rendering.
    /// Uses previously discovered cameras to walk component tree and execute render passes.
    /// </summary>
    public void RenderFrame()
    {
        if (_disposed)
        {
            _logger.LogWarning("Attempted to render frame on disposed renderer");
            return;
        }

        if (_rootComponent == null)
        {
            _logger.LogTrace("No root component set, skipping frame");
            return;
        }

        if (_cameras.Count == 0)
        {
            _logger.LogTrace("No cameras available, skipping frame");
            return;
        }

        try
        {
            // CAMERA-CENTRIC RENDERING: Process each camera
            foreach (var camera in _cameras)
            {
                // Skip inactive cameras
                if (camera is IRuntimeComponent cameraComponent && !cameraComponent.IsActive)
                    continue;

                // Walk component tree ONCE per camera to collect draw commands
                WalkComponentTreeForCamera(_rootComponent, camera);

                // Execute ALL render passes for this camera
                foreach (var pass in camera.RenderPasses)
                {
                    ExecuteBatchesForPass(pass);
                }

                // Clear batches before next camera
                ClearBatches();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during frame rendering");
            throw;
        }
    }

    /// <summary>
    /// Refreshes the camera list by walking the current component tree.
    /// Called when RootComponent is set or when component tree structure changes.
    /// </summary>
    private void RefreshCameras()
    {
        _cameras.Clear();

        if (_rootComponent != null)
        {
            FindCamerasRecursive(_rootComponent);
            _logger.LogDebug("Refreshed cameras: found {CameraCount} cameras in component tree", _cameras.Count);
        }
        else
        {
            _logger.LogDebug("Cleared cameras: no root component");
        }
    }

    /// <summary>
    /// Recursively finds all cameras in the component tree.
    /// </summary>
    private void FindCamerasRecursive(IRuntimeComponent component)
    {
        _logger.LogTrace("Checking component: {ComponentType}, IsCamera: {IsCamera}, ChildCount: {ChildCount}",
            component.GetType().Name, component is ICamera, component.Children.Count());

        // Check if this component is a camera
        if (component is ICamera camera)
        {
            _cameras.Add(camera);
            _logger.LogTrace("Found camera: {CameraType}", camera.GetType().Name);
        }

        // Recursively check children
        foreach (var child in component.Children)
        {
            FindCamerasRecursive(child);
        }
    }

    /// <summary>
    /// Walks the component tree once for a specific camera, collecting draw commands.
    /// Applies camera-specific frustum culling and pass filtering.
    /// </summary>
    private void WalkComponentTreeForCamera(IRuntimeComponent component, ICamera camera)
    {
        WalkComponentRecursiveForCamera(component, camera);
    }

    /// <summary>
    /// Recursively walks components for a specific camera, collecting renderable components.
    /// </summary>
    private void WalkComponentRecursiveForCamera(IRuntimeComponent component, ICamera camera)
    {
        // Skip if component is not active
        if (!component.IsActive)
            return;

        // If component is renderable, collect draw commands for camera's passes
        if (component is IRenderable renderable)
        {
            // Check each render pass of the camera
            foreach (var pass in camera.RenderPasses)
            {
                // Check if renderable should render for this pass using bit flags
                uint passFlag = 1u << pass.Id;
                if ((renderable.RenderPassFlags & passFlag) != 0)
                {
                    // Apply frustum culling if component has bounds
                    if (camera.IsVisible(renderable.BoundingBox))
                    {
                        // TODO: Add actual render command collection here
                        // This is where individual components would add their draw commands to batches
                        _logger.LogTrace("Would render {ComponentType} for pass {PassName}",
                            component.GetType().Name, pass.Name);
                    }
                }
            }
        }

        // Recursively process children if this component allows it
        if (component is not IRenderable renderableComponent || renderableComponent.ShouldRenderChildren)
        {
            foreach (var child in component.Children)
            {
                WalkComponentRecursiveForCamera(child, camera);
            }
        }
    }

    /// <summary>
    /// Executes all batches for a specific render pass.
    /// </summary>
    private void ExecuteBatchesForPass(RenderPassConfiguration pass)
    {
        _logger.LogTrace("Executing batches for render pass: {PassName} (ID: {PassId})", pass.Name, pass.Id);

        // Configure OpenGL state for this pass
        ConfigureRenderPassState(pass);

        // Execute all batches (they should be filtered by pass during collection)
        foreach (var batch in _batches.Values)
        {
            if (batch.CommandCount > 0)
            {
                batch.Execute();
            }
        }
    }

    /// <summary>
    /// Configures OpenGL state for a specific render pass.
    /// </summary>
    private void ConfigureRenderPassState(RenderPassConfiguration pass)
    {
        // Configure depth testing
        if (pass.DepthTestEnabled)
        {
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
        }
        else
        {
            GL.Disable(EnableCap.DepthTest);
        }

        // Configure alpha blending based on BlendingMode
        switch (pass.BlendingMode)
        {
            case BlendingMode.None:
                GL.Disable(EnableCap.Blend);
                break;
            case BlendingMode.Alpha:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
            case BlendingMode.Additive:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                break;
            case BlendingMode.Multiply:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
                break;
            case BlendingMode.PremultipliedAlpha:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
                break;
            case BlendingMode.Subtract:
                GL.Enable(EnableCap.Blend);
                GL.BlendEquation(BlendEquationModeEXT.FuncReverseSubtract);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                break;
            default:
                GL.Disable(EnableCap.Blend);
                break;
        }

        // Additional pass configuration can be added here
    }

    private void ExecuteRenderPass(RenderPassConfiguration pass)
    {
        _logger.LogTrace("Executing render pass: {PassName} (ID: {PassId})", pass.Name, pass.Id);

        // Configure depth testing
        if (pass.DepthTestEnabled)
        {
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);
        }
        else
        {
            GL.Disable(EnableCap.DepthTest);
        }

        // Configure blending
        ConfigureBlending(pass.BlendingMode);

        // Additional pass-specific setup can be added here
        // For now, this is where component rendering would happen
        // Components with IRenderable interface would call OnRender() during this phase
    }

    private void ConfigureBlending(BlendingMode mode)
    {
        switch (mode)
        {
            case BlendingMode.None:
                GL.Disable(EnableCap.Blend);
                break;
            case BlendingMode.Alpha:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
            case BlendingMode.Additive:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                break;
            case BlendingMode.Multiply:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
                break;
            case BlendingMode.PremultipliedAlpha:
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
                break;
            default:
                _logger.LogWarning("Unknown blending mode: {Mode}", mode);
                GL.Disable(EnableCap.Blend);
                break;
        }
    }


    /// <summary>
    /// Recursively walks the component tree to collect draw commands
    /// </summary>
    private void WalkComponentTree(IRuntimeComponent component)
    {
        if (component == null) return;

        // Check if component should be rendered
        if (component is IRenderable renderable && renderable.ShouldRender)
        {
            // TODO: Add frustum culling check
            // TODO: Add render pass filtering

            renderable.OnRender(this, 0.0); // deltaTime will be handled differently
        }

        // Recursively walk children if allowed
        bool shouldWalkChildren = true;
        if (component is IRenderable renderableWithCulling)
        {
            shouldWalkChildren = renderableWithCulling.ShouldRenderChildren;
        }

        if (shouldWalkChildren && component.Children != null)
        {
            foreach (var child in component.Children)
            {
                WalkComponentTree(child);
            }
        }
    }

    /// <summary>
    /// Executes all batches in sorted order for state optimization
    /// </summary>
    private void ExecuteBatches()
    {
        if (_batches.Count == 0) return;

        // Sort batches by key for state optimization
        var sortedBatches = _batches.OrderBy(kvp => kvp.Key.GetHashCode()).ToList();

        foreach (var (batchKey, batch) in sortedBatches)
        {
            if (batch.DrawCommands.Count == 0) continue;

            try
            {
                // Execute all commands in this batch
                batch.Execute();

                // Check for GL errors
                var error = GL.GetError();
                if (error != GLEnum.NoError)
                {
                    _logger.LogWarning("OpenGL error after batch {BatchKey}: {Error}", batchKey, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing batch {BatchKey}", batchKey);
                // Continue with other batches
            }
        }
    }

    /// <summary>
    /// Clears all batches for the next frame
    /// </summary>
    private void ClearBatches()
    {
        foreach (var batch in _batches.Values)
        {
            batch.Clear();
        }
        _batches.Clear();
        _batchExecutionOrder.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            // Clean up all shared resources
            _sharedResources.Clear();
            _logger.LogDebug("Renderer disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during renderer disposal");
        }
        finally
        {
            _disposed = true;
        }
    }
}
