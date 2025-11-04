using Nexus.GameEngine.Resources.Geometry.Definitions;
using Nexus.GameEngine.Resources.Textures;
using Nexus.GameEngine.Resources.Textures.Definitions;

namespace Nexus.GameEngine.GUI;

/// <summary>
/// Base class for UI components with anchor-based positioning.
/// 
/// POSITIONING SYSTEM:
/// - Position: Where the element IS in screen space (pixels) - inherited from Transformable
/// - AnchorPoint: Which point of the element aligns with Position (normalized -1 to 1)
/// - Size: Pixel dimensions of the element
/// - Scale: Multiplier applied to Size for effects - inherited from Transformable
/// 
/// LAYOUT INTEGRATION:
/// - Parents call SetSizeConstraints() to define available space
/// - Element positions itself within constraints via OnSizeConstraintsChanged()
/// - Default: Fill constraints with top-left anchor
/// - Override OnSizeConstraintsChanged() for custom layout behavior
/// 
/// RENDERING:
/// - WorldMatrix (from Transformable) used as model matrix
/// - Transforms from local geometry space to screen space
/// - Combined with camera view-projection matrix in shader
/// </summary>
public partial class Element(IDescriptorManager descriptorManager) : Drawable, IUserInterfaceElement
{
    protected IDescriptorManager DescriptorManager { get; } = descriptorManager;

    /// <summary>
    /// Overrides UpdateLocalMatrix to account for Size and AnchorPoint.
    /// Computes the position of the quad's center based on where the anchor point should be.
    /// </summary>
    protected override void UpdateLocalMatrix()
    {
        // Compute quad center position from anchor point
        // Position is where the AnchorPoint is located in screen space
        // We offset backwards to find the center of the quad
        var centerX = Position.X - (AnchorPoint.X * Size.X * 0.5f * Scale.X);
        var centerY = Position.Y - (AnchorPoint.Y * Size.Y * 0.5f * Scale.Y);
        var centerZ = Position.Z;

        // Scale: Geometry is 2x2 (±1), so scale by Size/2 to get pixel dimensions
        // Then multiply by Scale for effects
        var scaleX = Size.X * 0.5f * Scale.X;
        var scaleY = Size.Y * 0.5f * Scale.Y;
        var scaleZ = Scale.Z;

        // Build transform: Scale then Translate (no rotation for basic UI)
        // Store directly in the _localMatrix field from base class
        _localMatrix = Matrix4X4.CreateScale(scaleX, scaleY, scaleZ)
                     * Matrix4X4.CreateTranslation(centerX, centerY, centerZ);
    }

    /// <summary>
    /// Anchor point in normalized element space (-1 to 1).
    /// Defines which point of the element aligns with Position in screen space.
    /// (-1, -1) = top-left, (0, 0) = center, (1, 1) = bottom-right.
    /// Default: (-1, -1) for top-left alignment (standard UI behavior).
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<float> _anchorPoint = new(-1, -1);

    partial void OnAnchorPointChanged(Vector2D<float> oldValue)
    {
        // When AnchorPoint changes, regenerate the cached local matrix
        UpdateLocalMatrix();
    }

    /// <summary>
    /// Pixel dimensions of the element (width, height).
    /// Combined with Scale (inherited from Transformable) to determine final rendered size.
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    protected Vector2D<int> _size = new(0, 0);

    partial void OnSizeChanged(Vector2D<int> oldValue)
    {
        // When Size changes, regenerate the cached local matrix
        UpdateLocalMatrix();
    }

    /// <summary>
    /// UV rectangle for texture atlas/sprite sheet support.
    /// MinUV = top-left corner of sprite in texture (default 0,0)
    /// MaxUV = bottom-right corner of sprite in texture (default 1,1)
    /// For full texture: MinUV=(0,0), MaxUV=(1,1)
    /// For sprite in atlas: set to sub-rectangle coordinates
    /// </summary>
    [ComponentProperty]
    [TemplateProperty]
    private Vector2D<float> _minUV = new(0f, 0f);

    [ComponentProperty]
    [TemplateProperty]
    private Vector2D<float> _maxUV = new(1f, 1f);

    /// <summary>
    /// Size constraints available to this element (set by parent or viewport).
    /// Defines the maximum space this element can occupy.
    /// </summary>
    private Rectangle<int> _sizeConstraints = new(0, 0, 0, 0);

    /// <summary>
    /// Gets the current size constraints for this element.
    /// </summary>
    protected Rectangle<int> SizeConstraints => _sizeConstraints;

    /// <summary>
    /// Sets the size constraints for this element.
    /// Called by parent layout for root elements.
    /// </summary>
    public virtual void SetSizeConstraints(Rectangle<int> constraints)
    {
        _sizeConstraints = constraints;
        // When constraints change, element may need to recalculate its bounds
        OnSizeConstraintsChanged(constraints);
    }

    /// <summary>
    /// Called when size constraints change.
    /// Override to implement custom sizing behavior.
    /// Default behavior: Fill constraints completely with top-left anchor.
    /// </summary>
    protected virtual void OnSizeConstraintsChanged(Rectangle<int> constraints)
    {
        if (constraints.Size.X <= 0 || constraints.Size.Y <= 0)
            return;
        
        // Default: fill constraints with top-left anchor
        // Position = top-left corner of constraints
        SetPosition(new Vector3D<float>(
            constraints.Origin.X,
            constraints.Origin.Y,
            Position.Z  // Keep current Z
        ));
        
        // Size = full constraint size
        SetSize(new Vector2D<int>(constraints.Size.X, constraints.Size.Y));
        
        // Ensure anchor is top-left for this default behavior
        if (AnchorPoint.X != -1 || AnchorPoint.Y != -1)
        {
            SetAnchorPoint(new Vector2D<float>(-1, -1));
        }
    }

    /// <summary>
    /// Computes the bounding rectangle of this element in screen space.
    /// Derived from Position, AnchorPoint, and Size.
    /// </summary>
    public Rectangle<int> GetBounds()
    {
        // AnchorPoint is in normalized space (-1 to 1)
        // Convert to offset: (AnchorPoint + 1) / 2 gives 0 to 1
        // Multiply by Size to get pixel offset from Position
        var anchorOffsetX = (AnchorPoint.X + 1.0f) * 0.5f * Size.X;
        var anchorOffsetY = (AnchorPoint.Y + 1.0f) * 0.5f * Size.Y;
        
        // Top-left corner = Position - anchor offset
        var originX = (int)(Position.X - anchorOffsetX);
        var originY = (int)(Position.Y - anchorOffsetY);
        
        return new Rectangle<int>(originX, originY, Size.X, Size.Y);
    }

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
    /// Has ComponentProperty for runtime behavior but NOT TemplateProperty (texture set via _textureDefinition in template).
    /// </summary>
    [ComponentProperty]
    private TextureResource? _texture;

    public override PipelineHandle Pipeline =>
        PipelineManager.GetOrCreate(PipelineDefinitions.UIElement);
        
    protected GeometryResource? Geometry { get; set; }
    protected DescriptorSet? TextureDescriptorSet { get; set; }

    /// <summary>
    /// Sets texture from a definition.
    /// Caches the definition and creates the TextureResource via ResourceManager.
    /// </summary>
    public void SetTexture(TextureDefinition? definition)
    {
        if (definition == null)
        {
            _textureDefinition = null;
            SetTexture((TextureResource?)null);
            return;
        }

        if (ResourceManager == null) return;
        
        _textureDefinition = definition;  // Cache the definition
        var resource = ResourceManager.Textures.GetOrCreate(definition);
        SetTexture(resource);
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

        var bounds = GetBounds();

        var pipeline = Pipeline;
    }
    
    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (Geometry == null) yield break;

        var bounds = GetBounds();

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
        
        // Texture is managed by ResourceManager, no manual release needed
        // Descriptor sets are freed automatically when the pool is reset
        
        base.OnDeactivate();
    }
}
