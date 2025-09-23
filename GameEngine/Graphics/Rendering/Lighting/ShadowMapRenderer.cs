using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Graphics.Rendering.Resources;
using Silk.NET.OpenGL;
using Silk.NET.Maths;

namespace Nexus.GameEngine.Graphics.Rendering.Lighting;

/// <summary>
/// Manages shadow map rendering for dynamic lighting
/// </summary>
public class ShadowMapRenderer : IDisposable
{
    private readonly GL _gl;
    private readonly ILogger _logger;
    private readonly IResourcePool _resourcePool;

    private PooledRenderTarget? _directionalShadowMap;
    private PooledRenderTarget? _pointShadowCubemap;
    private PooledRenderTarget? _spotShadowMap;
    private bool _disposed;

    /// <summary>
    /// Gets the size of directional light shadow maps
    /// </summary>
    public int DirectionalShadowMapSize { get; private set; } = 2048;

    /// <summary>
    /// Gets the size of point light shadow cubemaps
    /// </summary>
    public int PointShadowMapSize { get; private set; } = 1024;

    /// <summary>
    /// Gets the size of spot light shadow maps
    /// </summary>
    public int SpotShadowMapSize { get; private set; } = 1024;

    /// <summary>
    /// Gets the number of cascade levels for directional shadows
    /// </summary>
    public int CascadeLevels { get; private set; } = 4;

    /// <summary>
    /// Gets whether this renderer has been disposed
    /// </summary>
    public bool IsDisposed => _disposed;

    public ShadowMapRenderer(GL gl, ILogger logger, IResourcePool resourcePool)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourcePool = resourcePool ?? throw new ArgumentNullException(nameof(resourcePool));

        Initialize();
    }

    /// <summary>
    /// Renders shadow map for a directional light using cascaded shadow maps
    /// </summary>
    /// <param name="light">Directional light</param>
    /// <param name="cameraPosition">Camera position</param>
    /// <param name="cameraDirection">Camera view direction</param>
    /// <param name="renderables">Objects to render to shadow map</param>
    public void RenderDirectionalShadow(Light light, Vector3D<float> cameraPosition, Vector3D<float> cameraDirection,
        IEnumerable<IShadowCaster> renderables)
    {
        ThrowIfDisposed();

        if (light.Type != LightType.Directional)
            throw new ArgumentException("Light must be directional", nameof(light));

        if (_directionalShadowMap == null)
            return;

        _logger.LogDebug("Rendering directional shadow map for light {LightId}", light.GetHashCode());

        // TODO: Implement cascaded shadow map rendering
        // 1. Calculate cascade frustum splits
        // 2. Render each cascade
        // 3. Update shadow matrices
    }

    /// <summary>
    /// Renders shadow cubemap for a point light
    /// </summary>
    /// <param name="light">Point light</param>
    /// <param name="renderables">Objects to render to shadow map</param>
    public void RenderPointShadow(Light light, IEnumerable<IShadowCaster> renderables)
    {
        ThrowIfDisposed();

        if (light.Type != LightType.Point)
            throw new ArgumentException("Light must be point light", nameof(light));

        if (_pointShadowCubemap == null)
            return;

        _logger.LogDebug("Rendering point shadow cubemap for light {LightId}", light.GetHashCode());

        // TODO: Implement point light shadow cubemap rendering
        // 1. Render to each cubemap face
        // 2. Update shadow matrices for all faces
    }

    /// <summary>
    /// Renders shadow map for a spot light
    /// </summary>
    /// <param name="light">Spot light</param>
    /// <param name="renderables">Objects to render to shadow map</param>
    public void RenderSpotShadow(Light light, IEnumerable<IShadowCaster> renderables)
    {
        ThrowIfDisposed();

        if (light.Type != LightType.Spot)
            throw new ArgumentException("Light must be spot light", nameof(light));

        if (_spotShadowMap == null)
            return;

        _logger.LogDebug("Rendering spot shadow map for light {LightId}", light.GetHashCode());

        // TODO: Implement spot light shadow map rendering
        // 1. Set up shadow camera from light perspective
        // 2. Render shadow casters
        // 3. Update shadow matrix
    }

    /// <summary>
    /// Gets the shadow matrix for a light
    /// </summary>
    /// <param name="light">Light to get shadow matrix for</param>
    /// <returns>Shadow transformation matrix</returns>
    public Matrix4X4<float> GetShadowMatrix(Light light)
    {
        ThrowIfDisposed();

        // TODO: Return appropriate shadow matrix based on light type
        return Matrix4X4<float>.Identity;
    }

    /// <summary>
    /// Binds shadow maps for use in shaders
    /// </summary>
    /// <param name="textureUnit">Starting texture unit</param>
    public void BindShadowMaps(uint textureUnit)
    {
        ThrowIfDisposed();

        // TODO: Implement texture unit binding when GL supports ActiveTexture
        // For now, just bind the textures to default units

        if (_directionalShadowMap != null)
        {
            _gl.BindTexture(TextureTarget.Texture2D, _directionalShadowMap.DepthTextureId ?? 0);
        }

        if (_pointShadowCubemap != null)
        {
            _gl.BindTexture(TextureTarget.TextureCubeMap, _pointShadowCubemap.DepthTextureId ?? 0);
        }

        if (_spotShadowMap != null)
        {
            _gl.BindTexture(TextureTarget.Texture2D, _spotShadowMap.DepthTextureId ?? 0);
        }

        _logger.LogDebug("Bound shadow maps starting at texture unit {TextureUnit}", textureUnit);
    }

    private void Initialize()
    {
        CreateShadowMaps();
        _logger.LogDebug("Initialized ShadowMapRenderer with {DirectionalSize}x{DirectionalSize} directional, " +
                        "{PointSize}x{PointSize} point, {SpotSize}x{SpotSize} spot shadow maps",
                        DirectionalShadowMapSize, DirectionalShadowMapSize,
                        PointShadowMapSize, PointShadowMapSize,
                        SpotShadowMapSize, SpotShadowMapSize);
    }

    private void CreateShadowMaps()
    {
        try
        {
            // Directional shadow map (cascaded)
            _directionalShadowMap = _resourcePool.RentRenderTarget(
                DirectionalShadowMapSize * CascadeLevels,
                DirectionalShadowMapSize,
                true // include depth
            );

            // Point shadow cubemap
            _pointShadowCubemap = _resourcePool.RentRenderTarget(
                PointShadowMapSize,
                PointShadowMapSize,
                true // include depth
            );

            // Spot shadow map
            _spotShadowMap = _resourcePool.RentRenderTarget(
                SpotShadowMapSize,
                SpotShadowMapSize,
                true // include depth
            );

            _logger.LogDebug("Created shadow map render targets");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create shadow map render targets");
            throw;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ShadowMapRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogDebug("Disposing ShadowMapRenderer");

        if (_directionalShadowMap != null)
            _resourcePool.ReturnRenderTarget(_directionalShadowMap);
        if (_pointShadowCubemap != null)
            _resourcePool.ReturnRenderTarget(_pointShadowCubemap);
        if (_spotShadowMap != null)
            _resourcePool.ReturnRenderTarget(_spotShadowMap);

        _disposed = true;
    }
}

/// <summary>
/// Interface for objects that can cast shadows
/// </summary>
public interface IShadowCaster
{
    /// <summary>
    /// Renders this object to the shadow map
    /// </summary>
    /// <param name="shadowMatrix">Shadow transformation matrix</param>
    void RenderShadow(Matrix4X4<float> shadowMatrix);
}