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
public partial class Element : Drawable, IUserInterfaceElement
{
    /// <summary>
    /// Anchor point in normalized element space (-1 to 1).
    /// Defines which point of the element aligns with Position in screen space.
    /// (-1, -1) = top-left, (0, 0) = center, (1, 1) = bottom-right.
    /// Default: (-1, -1) for top-left alignment (standard UI behavior).
    /// </summary>
    [ComponentProperty]
    protected Vector2D<float> _anchorPoint = new(-1, -1);

    /// <summary>
    /// Pixel dimensions of the element (width, height).
    /// Combined with Scale (inherited from Transformable) to determine final rendered size.
    /// </summary>
    [ComponentProperty]
    protected Vector2D<int> _size = new(0, 0);

    partial void OnSizeChanged(Vector2D<int> oldValue)
    {
        // When Size changes, regenerate the cached local matrix
        UpdateLocalMatrix();
    }

    partial void OnAnchorPointChanged(Vector2D<float> oldValue)
    {
        // When AnchorPoint changes, regenerate the cached local matrix
        UpdateLocalMatrix();
    }

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
    protected int _zIndex = 0;

    [ComponentProperty]
    protected Vector4D<float> _tintColor = Colors.White;

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

    protected virtual GeometryDefinition GetGeometryDefinition() => GeometryDefinitions.UniformColorQuad;

    public override PipelineHandle Pipeline =>
        PipelineManager.GetOrCreate(PipelineDefinitions.UIElement);
        
    protected GeometryResource? Geometry { get; set; }

    protected override void OnActivate()
    {
        base.OnActivate();

        Log.Debug($"=== Element.OnActivate() START ===");
        Log.Debug($"  Position: {Position}");
        Log.Debug($"  Size: {Size}");
        Log.Debug($"  AnchorPoint: {AnchorPoint}");
        Log.Debug($"  TintColor: R={TintColor.X:F3}, G={TintColor.Y:F3}, B={TintColor.Z:F3}, A={TintColor.W:F3}");
        Log.Debug($"  WorldMatrix: {WorldMatrix}");

        // Create geometry resource
        Geometry = ResourceManager?.Geometry.GetOrCreate(GetGeometryDefinition());
        Log.Debug($"  Geometry created: Handle={Geometry?.Buffer.Handle ?? 0}, VertexCount={Geometry?.VertexCount ?? 0}");

        var bounds = GetBounds();
        Log.Debug($"  Computed Bounds: ({bounds.Origin.X}, {bounds.Origin.Y}, {bounds.Size.X}, {bounds.Size.Y})");
        
        var pipeline = Pipeline;
        Log.Debug($"  Pipeline: Handle={pipeline.Pipeline.Handle}, Layout={pipeline.Layout.Handle}");
        Log.Debug($"=== Element.OnActivate() END ===");
    }    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        Log.Debug($"=== Element.GetDrawCommands() START ===");
        Log.Debug($"  Geometry: {(Geometry == null ? "NULL" : $"Handle={Geometry.Buffer.Handle}, VertexCount={Geometry.VertexCount}")}");
        
        if (Geometry == null)
        {
            Log.Warning($"  Element.GetDrawCommands(): Geometry is null, yielding no commands");
            yield break;
        }

        var bounds = GetBounds();
        Log.Debug($"  Position: {Position}, Size: {Size}, AnchorPoint: {AnchorPoint}");
        Log.Debug($"  Expected pixel rect - TopLeft: ({bounds.Origin.X}, {bounds.Origin.Y}), BottomRight: ({bounds.Origin.X + bounds.Size.X}, {bounds.Origin.Y + bounds.Size.Y})");
        Log.Debug($"  WorldMatrix: {WorldMatrix}");
        Log.Debug($"  TintColor: R={TintColor.X:F3}, G={TintColor.Y:F3}, B={TintColor.Z:F3}, A={TintColor.W:F3}");
        Log.Debug($"  ViewProjectionDescriptorSet.Handle: {context.ViewProjectionDescriptorSet.Handle}");

        var pushConstants = UniformColorPushConstants.FromModelAndColor(WorldMatrix, TintColor);
        Log.Debug($"  PushConstants created - Model: {pushConstants.Model}, Color: R={pushConstants.Color.X:F3}, G={pushConstants.Color.Y:F3}, B={pushConstants.Color.Z:F3}, A={pushConstants.Color.W:F3}");

        var drawCommand = new DrawCommand
        {
            RenderMask = RenderPasses.UI,
            Pipeline = Pipeline,
            VertexBuffer = Geometry.Buffer,
            VertexCount = Geometry.VertexCount,
            InstanceCount = 1,
            RenderPriority = ZIndex,
            // ViewProjection matrix bound via UBO from camera's descriptor set
            DescriptorSet = context.ViewProjectionDescriptorSet,
            // Push model matrix + color (80 bytes: 64 for matrix + 16 for color)
            PushConstants = pushConstants
        };
        
        Log.Debug($"  DrawCommand created: Pipeline={drawCommand.Pipeline.Pipeline.Handle}, VertexBuffer={drawCommand.VertexBuffer.Handle}, DescriptorSet={drawCommand.DescriptorSet.Handle}");
        Log.Debug($"=== Element.GetDrawCommands() END (yielding 1 command) ===");
        
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
