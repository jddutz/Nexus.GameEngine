namespace Nexus.GameEngine.GUI;

/// <summary>
/// Base class for most user interface components.
/// Extends Transformable to add 2D UI-specific positioning and bounds management.
/// Provides layout system integration through SetBounds/GetBounds.
/// All coordinates are in NDC space (Normalized Device Coordinates: -1 to 1).
/// </summary>
public partial class Element : Drawable, IUserInterfaceElement
{
    [ComponentProperty]
    protected Rectangle<int> _bounds = new(0, 0, 0, 0);

    partial void OnBoundsChanged(Rectangle<int> oldValue)
    {
        UpdateGeometry();
        OnOriginChanged(oldValue.Origin);
        OnSizeChanged(oldValue.Size);
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
    /// Default: For root elements (no parent), set bounds to match constraints.
    /// </summary>
    protected virtual void OnSizeConstraintsChanged(Rectangle<int> constraints)
    {
        // If this is a root element (no parent Element), fill constraints
        var parent = FindParent<Element>();
        if (parent == null && constraints.Size.X > 0 && constraints.Size.Y > 0)
        {
            SetBounds(constraints);
        }
    }

    public Vector2D<int> Origin => Bounds.Origin;
    public void SetOrigin(Vector2D<int> origin) => SetBounds(new(origin, Size));
    public virtual void OnOriginChanged(Vector2D<int> oldValue) { }

    public Vector2D<int> Size => Bounds.Size;
    public void SetSize(Vector2D<int> size) => SetBounds(new(Origin, size));
    public virtual void OnSizeChanged(Vector2D<int> oldValue) { }

    [ComponentProperty]
    protected int _zIndex = 0;

    partial void OnZIndexChanged(int oldValue)
    {
        UpdateGeometry();
    }

    [ComponentProperty]
    protected Vector4D<float> _tintColor = Colors.White;

    protected virtual GeometryDefinition GetGeometryDefinition() => GeometryDefinitions.UniformColorQuad;

    public override PipelineHandle Pipeline =>
        PipelineManager.GetOrCreate(PipelineDefinitions.UIElement);
        
    protected GeometryResource? Geometry { get; set; }

    public virtual void UpdateGeometry()
    {
        var newPosition = new Vector3D<float>(Bounds.Origin.X, Bounds.Origin.Y, ZIndex);
        SetPosition(newPosition);

        var newScale = new Vector3D<float>(Size.X, Size.Y, 1.0f);
        SetScale(newScale);
    }

    protected override void OnActivate()
    {
        base.OnActivate();

        // Create geometry resource
        Geometry = ResourceManager?.Geometry.GetOrCreate(GetGeometryDefinition());
    }

    public override IEnumerable<DrawCommand> GetDrawCommands(RenderContext context)
    {
        if (Geometry == null)
            yield break;

        yield return new DrawCommand
        {
            RenderMask = RenderPasses.UI,
            Pipeline = Pipeline,
            VertexBuffer = Geometry.Buffer,
            VertexCount = Geometry.VertexCount,
            InstanceCount = 1,
            RenderPriority = ZIndex,
            PushConstants = TransformedColorPushConstants.FromMatrixAndColor(
                context.ViewProjectionMatrix,
                TintColor
            )
        };
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
