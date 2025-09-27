using Microsoft.Extensions.Logging;
using Nexus.GameEngine.Assets;
using Nexus.GameEngine.Components;
using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Graphics.Extensions;
using Nexus.GameEngine.Graphics.Resources;
using Nexus.GameEngine.Graphics.Textures;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Nexus.GameEngine.Graphics.Sprites;

/// <summary>
/// Component that renders a 2D sprite using a texture asset.
/// Demonstrates integration with the asset management system.
/// </summary>
public class SpriteComponent(IAssetService assetService, IResourceManager resourceManager)
    : RuntimeComponent, IRenderable
{
    /// <summary>
    /// Template for configuring a sprite component with texture asset reference.
    /// </summary>
    public new record Template : RuntimeComponent.Template
    {
        /// <summary>
        /// Reference to the texture asset for this sprite.
        /// </summary>
        public AssetReference<ManagedTexture> Texture { get; init; } = new();

        /// <summary>
        /// The size of the sprite in world units.
        /// </summary>
        public Vector2D<float> Size { get; init; } = Vector2D<float>.One;

        /// <summary>
        /// The tint color applied to the sprite (RGBA as Vector4D<float>).
        /// </summary>
        public Vector4D<float> Tint { get; init; } = Vector4D<float>.One; // White color (1,1,1,1)

        /// <summary>
        /// Whether to flip the sprite horizontally.
        /// </summary>
        public bool FlipX { get; init; } = false;

        /// <summary>
        /// Whether to flip the sprite vertically.
        /// </summary>
        public bool FlipY { get; init; } = false;
    }

    private readonly IAssetService _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
    private readonly IResourceManager _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
    private Template? _template;
    private ManagedTexture? _loadedTexture;

    /// <summary>
    /// Gets the currently loaded texture, if any.
    /// </summary>
    public ManagedTexture? Texture => _loadedTexture;

    /// <summary>
    /// Gets the sprite size in world units.
    /// </summary>
    public Vector2D<float> Size => _template?.Size ?? new Vector2D<float>(1.0f, 1.0f);

    /// <summary>
    /// Gets the sprite tint color (RGBA as Vector4D<float>).
    /// </summary>
    public Vector4D<float> Tint => _template?.Tint ?? Vector4D<float>.One;

    /// <summary>
    /// Gets whether the sprite is flipped horizontally.
    /// </summary>
    public bool FlipX => _template?.FlipX ?? false;

    /// <summary>
    /// Gets whether the sprite is flipped vertically.
    /// </summary>
    public bool FlipY => _template?.FlipY ?? false;

    /// <summary>
    /// Whether this sprite is visible and should be rendered.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Effects to be applied when rendering this sprite.
    /// </summary>
    public IEnumerable<IRenderEffect> Effects { get; } = Array.Empty<IRenderEffect>();

    protected override void OnConfigure(IComponentTemplate template)
    {
        base.OnConfigure(template);
        _template = template as Template;
    }

    protected override void OnActivate()
    {
        base.OnActivate();

        if (_template?.Texture?.AssetId != null && !string.IsNullOrEmpty(_template.Texture.AssetId.Address))
        {
            // Load the texture asset asynchronously
            Task.Run(async () =>
            {
                try
                {
                    Logger?.LogDebug("Loading texture asset: {AssetId}", _template.Texture.AssetId.Address);
                    _loadedTexture = await _assetService.LoadAsync<ManagedTexture>(_template.Texture.AssetId.Address);
                    _template.Texture.CachedAsset = _loadedTexture;

                    Logger?.LogDebug("Successfully loaded texture asset: {AssetId}", _template.Texture.AssetId.Address);
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "Failed to load texture asset: {AssetId}", _template.Texture.AssetId.Address);
                }
            });
        }
    }

    protected override void OnDeactivate()
    {
        if (_template?.Texture?.AssetId != null && _loadedTexture != null)
        {
            // Note: We don't unload the asset here as it might be used by other components
            // The asset service manages the lifecycle based on caching strategy
            _template.Texture.CachedAsset = null;
            _loadedTexture = null;
        }

        base.OnDeactivate();
    }

    public bool ShouldRender => _loadedTexture != null && IsVisible;
    public int RenderPriority => 400; // UI layer

    /// <summary>
    /// Bounding box for frustum culling based on sprite size.
    /// Returns a minimal box since sprites are typically UI elements that should always render.
    /// </summary>
    public Box3D<float> BoundingBox => new(Vector3D<float>.Zero, Vector3D<float>.Zero); // Minimal box

    /// <summary>
    /// Sprite participates in UI render pass (pass 1).
    /// </summary>
    public uint RenderPassFlags => 1u << 1; // UI pass

    /// <summary>
    /// Sprites are leaf components and don't need to render children.
    /// </summary>
    public bool ShouldRenderChildren => false;

    public void OnRender(IRenderer renderer, double deltaTime)
    {
        if (!ShouldRender) return;

        // Use extension methods for common operations
        renderer.SetTexture(_loadedTexture!.TextureId, 0);
        renderer.SetBlending(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Get shared sprite quad from resource manager
        var quadVAO = _resourceManager.GetOrCreateResource("SpriteQuad", this);
        if (quadVAO != null) renderer.DrawMesh(quadVAO.Value, 6); // 6 indices for 2 triangles

        Logger?.LogTrace("Rendered sprite: TextureId={TextureId}, Size={Size}, Tint={Tint}",
            _loadedTexture.TextureId, Size, Tint);
    }
}