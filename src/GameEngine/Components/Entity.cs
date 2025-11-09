namespace Nexus.GameEngine.Components;

/// <summary>
/// Represents a uniquely identifiable object.
/// Classes deriving from Entity with [ComponentProperty] and [TemplateProperty] fields will have
/// animated properties generated via source generation.
/// </summary>
public abstract partial class Entity
{
    private bool _isDirty = false;

    /// <summary>
    /// Unique identifier for this component instance.
    /// </summary>
    public ComponentId Id { get; set; } = ComponentId.None;

    /// <summary>
    /// Human-readable name for this component instance.
    /// Changes are deferred until next ApplyUpdates() call.
    /// </summary>
    private string _targetName = string.Empty;
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (_targetName != value)
            {
                _targetName = value;
                _isDirty = true;
            }
        }
    }

    /// <summary>
    /// Applies deferred property updates with optional interpolation over time.
    /// Called every frame with deltaTime. Base implementation updates Name property.
    /// Derived classes with [ComponentProperty] fields should call base.ApplyUpdates(deltaTime)
    /// at the start of their generated ApplyUpdates override.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last frame in seconds</param>
    public virtual void ApplyUpdates(double deltaTime)
    {
        // Apply deferred Name property update (instant, no interpolation)
        if (_isDirty)
        {
            _name = _targetName;
            _isDirty = false;
        }
    }

    /// <summary>
    /// Helper method for canceling animations on layout-affecting properties.
    /// Use with [ComponentProperty(BeforeChange = nameof(CancelAnimation))] attribute.
    /// Forces immediate updates by setting duration to 0 and interpolation mode to Step.
    /// </summary>
    // CancelAnimation helper moved to Component so it is available to components
    // in the component inheritance chain (Component -> RuntimeComponent -> Transformable ...)
}