using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics.Rendering.Buffers;
using Silk.NET.Maths;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics.Rendering.Lighting;

/// <summary>
/// Manages dynamic lighting, light culling, and uniform buffer updates
/// </summary>
public class LightingManager : IDisposable
{
    private readonly GL _gl;
    private readonly ILogger _logger;
    private readonly IUniformBufferManager _uniformBufferManager;
    private readonly ConcurrentDictionary<uint, Light> _lights;
    private readonly object _lockObject = new();

    private SceneLightingData _sceneLightingData;
    private uint _nextLightId = 1;
    private UniformBlock? _lightingUniformBlock;
    private bool _lightingDataDirty = true;
    private bool _disposed;

    /// <summary>
    /// Gets whether this manager has been disposed
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Gets the number of active lights
    /// </summary>
    public int ActiveLightCount => _lights.Values.Count(l => l.IsEnabled);

    /// <summary>
    /// Gets the maximum number of lights supported
    /// </summary>
    public int MaxLights => SceneLightingData.MaxLights;

    /// <summary>
    /// Gets or sets the ambient light color and intensity
    /// </summary>
    public Vector4D<float> AmbientLight
    {
        get => _sceneLightingData.AmbientLight;
        set
        {
            _sceneLightingData.AmbientLight = value;
            _lightingDataDirty = true;
        }
    }

    /// <summary>
    /// Gets or sets the camera position for specular calculations
    /// </summary>
    public Vector3D<float> CameraPosition
    {
        get => new(_sceneLightingData.CameraPosition.X, _sceneLightingData.CameraPosition.Y, _sceneLightingData.CameraPosition.Z);
        set
        {
            _sceneLightingData.CameraPosition = new Vector4D<float>(value, 1.0f);
            _lightingDataDirty = true;
        }
    }

    /// <summary>
    /// Gets or sets whether shadows are enabled
    /// </summary>
    public bool ShadowsEnabled
    {
        get => _sceneLightingData.ShadowsEnabled != 0;
        set
        {
            _sceneLightingData.ShadowsEnabled = value ? 1 : 0;
            _lightingDataDirty = true;
        }
    }

    /// <summary>
    /// Gets or sets whether Image-Based Lighting is enabled
    /// </summary>
    public bool IBLEnabled
    {
        get => _sceneLightingData.IBLEnabled != 0;
        set
        {
            _sceneLightingData.IBLEnabled = value ? 1 : 0;
            _lightingDataDirty = true;
        }
    }

    public LightingManager(GL gl, ILogger logger, IUniformBufferManager uniformBufferManager)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uniformBufferManager = uniformBufferManager ?? throw new ArgumentNullException(nameof(uniformBufferManager));
        _lights = new ConcurrentDictionary<uint, Light>();

        Initialize();
    }

    /// <summary>
    /// Adds a light to the scene
    /// </summary>
    /// <param name="light">Light to add</param>
    /// <returns>Unique light ID</returns>
    public uint AddLight(Light light)
    {
        ThrowIfDisposed();

        if (light == null)
            throw new ArgumentNullException(nameof(light));

        lock (_lockObject)
        {
            light.Id = _nextLightId++;
            _lights[light.Id] = light;
            _lightingDataDirty = true;

            _logger.LogDebug("Added light {LightId} of type {LightType}", light.Id, light.Type);
            return light.Id;
        }
    }

    /// <summary>
    /// Removes a light from the scene
    /// </summary>
    /// <param name="lightId">ID of the light to remove</param>
    /// <returns>True if the light was removed, false if not found</returns>
    public bool RemoveLight(uint lightId)
    {
        ThrowIfDisposed();

        lock (_lockObject)
        {
            if (_lights.TryRemove(lightId, out var light))
            {
                _lightingDataDirty = true;
                _logger.LogDebug("Removed light {LightId} of type {LightType}", lightId, light.Type);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Gets a light by ID
    /// </summary>
    /// <param name="lightId">Light ID</param>
    /// <returns>Light instance or null if not found</returns>
    public Light? GetLight(uint lightId)
    {
        ThrowIfDisposed();
        return _lights.TryGetValue(lightId, out var light) ? light : null;
    }

    /// <summary>
    /// Gets all lights in the scene
    /// </summary>
    /// <returns>Collection of all lights</returns>
    public IEnumerable<Light> GetAllLights()
    {
        ThrowIfDisposed();
        return _lights.Values.ToList();
    }

    /// <summary>
    /// Updates a light's properties
    /// </summary>
    /// <param name="lightId">Light ID</param>
    /// <param name="updateAction">Action to update the light</param>
    /// <returns>True if the light was updated, false if not found</returns>
    public bool UpdateLight(uint lightId, Action<Light> updateAction)
    {
        ThrowIfDisposed();

        if (updateAction == null)
            throw new ArgumentNullException(nameof(updateAction));

        if (_lights.TryGetValue(lightId, out var light))
        {
            lock (_lockObject)
            {
                updateAction(light);
                _lightingDataDirty = true;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Performs frustum culling on lights based on camera view
    /// </summary>
    /// <param name="viewMatrix">Camera view matrix</param>
    /// <param name="projectionMatrix">Camera projection matrix</param>
    /// <returns>List of visible lights</returns>
    public List<Light> CullLights(Matrix4X4<float> viewMatrix, Matrix4X4<float> projectionMatrix)
    {
        ThrowIfDisposed();

        var visibleLights = new List<Light>();
        var viewProjectionMatrix = viewMatrix * projectionMatrix;

        foreach (var light in _lights.Values.Where(l => l.IsEnabled))
        {
            if (IsLightVisible(light, viewProjectionMatrix))
            {
                visibleLights.Add(light);
            }
        }

        // Sort by distance for optimal rendering order
        var cameraPosition = CameraPosition;
        visibleLights.Sort((a, b) =>
        {
            var distA = Vector3D.DistanceSquared(a.Position, cameraPosition);
            var distB = Vector3D.DistanceSquared(b.Position, cameraPosition);
            return distA.CompareTo(distB);
        });

        return visibleLights;
    }

    /// <summary>
    /// Updates the lighting uniform buffer with current scene data
    /// </summary>
    /// <param name="visibleLights">List of visible lights (optional, uses all lights if null)</param>
    public void UpdateLightingBuffer(List<Light>? visibleLights = null)
    {
        ThrowIfDisposed();

        if (!_lightingDataDirty)
            return;

        lock (_lockObject)
        {
            var lightsToUse = visibleLights ?? _lights.Values.Where(l => l.IsEnabled).ToList();
            var lightCount = Math.Min(lightsToUse.Count, MaxLights);

            _sceneLightingData.ActiveLightCount = lightCount;

            // Convert lights to GPU data format
            for (int i = 0; i < lightCount; i++)
            {
                var light = lightsToUse[i];
                var shadowMatrix = Matrix4X4<float>.Identity; // TODO: Get from shadow map renderer
                _sceneLightingData.Lights[i] = LightData.FromLight(light, shadowMatrix);
            }

            // Clear unused light slots
            for (int i = lightCount; i < MaxLights; i++)
            {
                _sceneLightingData.Lights[i] = default;
            }

            // Update uniform buffer
            if (_lightingUniformBlock != null)
            {
                var dataSize = Marshal.SizeOf<SceneLightingData>();
                var dataPtr = Marshal.AllocHGlobal(dataSize);
                try
                {
                    Marshal.StructureToPtr(_sceneLightingData, dataPtr, false);
                    unsafe
                    {
                        var dataBytes = new ReadOnlySpan<byte>((byte*)dataPtr.ToPointer(), dataSize);
                        _uniformBufferManager.UpdateBlock(_lightingUniformBlock, dataBytes);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(dataPtr);
                }
            }
            _lightingDataDirty = false;

            _logger.LogDebug("Updated lighting buffer with {LightCount} lights", lightCount);
        }
    }

    /// <summary>
    /// Binds the lighting uniform buffer to the specified binding point
    /// </summary>
    /// <param name="bindingPoint">Uniform buffer binding point</param>
    public void BindLightingBuffer(uint bindingPoint = 1)
    {
        ThrowIfDisposed();
        if (_lightingUniformBlock != null)
        {
            _uniformBufferManager.BindBlock(_lightingUniformBlock);
        }
    }

    /// <summary>
    /// Clears all lights from the scene
    /// </summary>
    public void ClearLights()
    {
        ThrowIfDisposed();

        lock (_lockObject)
        {
            var lightCount = _lights.Count;
            _lights.Clear();
            _lightingDataDirty = true;
            _logger.LogDebug("Cleared {LightCount} lights from scene", lightCount);
        }
    }

    private void Initialize()
    {
        _sceneLightingData = SceneLightingData.CreateDefault();
        _lightingUniformBlock = _uniformBufferManager.CreateBlock(
            "SceneLighting",
            SceneLightingData.SizeInBytes
        );

        _logger.LogDebug("LightingManager initialized with max {MaxLights} lights", MaxLights);
    }

    private bool IsLightVisible(Light light, Matrix4X4<float> viewProjectionMatrix)
    {
        // Directional lights are always visible
        if (light.Type == LightType.Directional)
            return true;

        // For point and spot lights, check if their influence sphere intersects the view frustum
        // This is a simplified check - a full implementation would use proper frustum culling
        var lightPosition = light.Position;
        var lightRange = light.Range;

        // Transform light position to clip space
        var clipPosition = Vector4D.Transform(new Vector4D<float>(lightPosition, 1.0f), viewProjectionMatrix);

        // Simple distance-based culling (can be improved with proper frustum planes)
        if (clipPosition.W <= 0)
            return false;

        var ndcPosition = clipPosition / clipPosition.W;

        // Check if light sphere intersects NDC cube (with some margin for light range)
        var margin = lightRange * 0.1f; // Approximate margin based on light range
        return ndcPosition.X >= -1.0f - margin && ndcPosition.X <= 1.0f + margin &&
               ndcPosition.Y >= -1.0f - margin && ndcPosition.Y <= 1.0f + margin &&
               ndcPosition.Z >= -1.0f - margin && ndcPosition.Z <= 1.0f + margin;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(LightingManager));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _lights.Clear();
        _lightingUniformBlock = null; // UniformBufferManager will handle cleanup
        _disposed = true;

        _logger.LogDebug("LightingManager disposed");
    }
}