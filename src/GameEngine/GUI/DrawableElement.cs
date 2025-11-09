using Nexus.GameEngine.Graphics;
using Nexus.GameEngine.Resources.Geometry.Definitions;
using Nexus.GameEngine.Resources.Textures.Definitions;

namespace Nexus.GameEngine.GUI;

/// <summary>
/// Render-capable UI element. Separates rendering responsibilities from layout/transform
/// responsibilities which remain in `Element`.
/// </summary>
public partial class DrawableElement : Element, IDrawable
{
    public DrawableElement(IDescriptorManager descriptorManager, IResourceManager resourceManager, IPipelineManager pipelineManager)
        : base(descriptorManager)
    {
        // Constructor injection: assign managers to base properties
        ResourceManager = resourceManager;
        PipelineManager = pipelineManager;
    }
    // Graphics managers owned by drawable element (injected via ctor)
    public IResourceManager ResourceManager { get; }
    public IPipelineManager PipelineManager { get; }

    [ComponentProperty]
    [TemplateProperty]
    protected int _zIndex = 0;

    [ComponentProperty]
    [TemplateProperty]
    protected Vector4D<float> _tintColor = Colors.White;

    /// <summary>
    /// Texture definition from template (private, not exposed).
    /// Used to create TextureResource in OnActivate.
    /// Cached for re-creation if SetTexture(TextureDefinition) is called.
    /// Cleared when SetTexture(TextureResource) is called directly.
    /// </summary>
    [TemplateProperty(Name = "Texture")]
    private TextureDefinition? _textureDefinition = null;

    /// <summary>
    /// Texture resource created from TextureDefinition.
    /// Has ComponentProperty for runtime behavior but NOT TemplateProperty (texture set via _texture_definition in template).
    /// </summary>
    [ComponentProperty]
    private TextureResource? _texture;

    [ComponentProperty]
    [TemplateProperty]
    protected bool _visible = true;

    public bool IsVisible() => IsValid && IsLoaded && Visible;

    public virtual PipelineHandle Pipeline =>
        PipelineManager.GetOrCreate(PipelineDefinitions.UIElement);

    protected IGeometryResource? Geometry { get; set; }
    protected DescriptorSet? TextureDescriptorSet { get; set; }

    /// <summary>
    /// Sets texture from a definition.
    /// Caches the definition and creates the TextureResource via ResourceManager.
    /// </summary>
    public void SetTexture(TextureDefinition? definition)
    {
        if (ResourceManager == null) return;

        if (definition == null)
        {
            SetTintColor(Colors.Magenta);

            var resource = ResourceManager.Textures.GetOrCreate(TextureDefinitions.UniformColor);
            SetTexture(resource);
        }
        else
        {
            var resource = ResourceManager.Textures.GetOrCreate(definition);
            SetTexture(resource);
        }

        _textureDefinition = definition;  // Cache the definition
    }

    /// <summary>
    /// Sets texture resource directly.
    /// Clears the cached definition since it's no longer relevant.
    /// </summary>
    public void SetTexture(TextureResource? resource)
    {
        _textureDefinition = null;  // Clear definition cache
        _texture = resource;

        // Update descriptor set if already activated
        if (IsActive() && TextureDescriptorSet.HasValue && _texture != null)
        {
            DescriptorManager.UpdateDescriptorSet(
                TextureDescriptorSet.Value,
                _texture.ImageView,
                _texture.Sampler,
                ImageLayout.ShaderReadOnlyOptimal,
                binding: 0);
        }
    }

    protected override void OnActivate()
    {
        base.OnActivate();

        // Use shared TexturedQuad geometry (UVs provided via push constants)
        Geometry = ResourceManager?.Geometry.GetOrCreate(GeometryDefinitions.TexturedQuad);

        // Convert texture definition to resource if provided, or use default uniform color texture
        if (_textureDefinition != null && _texture == null)
        {
            SetTexture(_textureDefinition);
        }
        else if (_texture == null)
        {
            // Default to UniformColor texture (1x1 white pixel) for elements without explicit textures
            // This ensures all elements can use the same uber-shader pipeline
            SetTexture(TextureDefinitions.UniformColor);
        }

        // Create descriptor set for texture (set=1)
        var shader = ShaderDefinitions.UIElement;
        if (shader.DescriptorSetLayouts != null && shader.DescriptorSetLayouts.TryGetValue(1, out var textureBindings))
        {
            // Create layout for set=1 (texture sampler)
            var layout = DescriptorManager.CreateDescriptorSetLayout(textureBindings);
            TextureDescriptorSet = DescriptorManager.AllocateDescriptorSet(layout);
            if (TextureDescriptorSet.HasValue && _texture != null)
            {
                DescriptorManager.UpdateDescriptorSet(
                    TextureDescriptorSet.Value,
                    _texture.ImageView,
                    _texture.Sampler,
                    ImageLayout.ShaderReadOnlyOptimal,
                    binding: 0);
            }
        }

        // Touch pipeline to ensure it's created/cached
        var pipeline = Pipeline;
    }

    public virtual IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (Geometry == null) yield break;

        var pushConstants = UIElementPushConstants.FromModelColorAndUV(WorldMatrix, TintColor, MinUV, MaxUV);

        var drawCommand = new DrawCommand
        {
            RenderMask = RenderPasses.UI,
            Pipeline = Pipeline,
            VertexBuffer = Geometry.Buffer,
            VertexCount = Geometry.VertexCount,
            InstanceCount = 1,
            RenderPriority = ZIndex,
            // For UIElement shader: bind texture descriptor set at set=1
            // ViewProjection UBO is already bound at set=0 by camera system
            DescriptorSet = TextureDescriptorSet ?? default,
            // Push model matrix + color + UV rect (96 bytes: 64 for matrix + 16 for color + 16 for UV)
            PushConstants = pushConstants
        };

        yield return drawCommand;
    }

    protected override void OnDeactivate()
    {
        if (Geometry != null)
        {
            ResourceManager?.Geometry.Release(GeometryDefinitions.TexturedQuad);
            Geometry = null;
        }

        base.OnDeactivate();
    }
}
