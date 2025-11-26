namespace Nexus.GameEngine.Components;

/// <summary>
/// Represents a uniquely identifiable object.
/// Classes deriving from Entity with [ComponentProperty] and [TemplateProperty] fields will have
/// animated properties generated via source generation.
/// </summary>
public abstract partial class Entity
{
    public Entity()
    {
        _name = GetType().Name;
    }

    /// <summary>
    /// Unique identifier for this component instance.
    /// </summary>
    public ComponentId Id { get; set; } = ComponentId.None;

    /// <summary>
    /// Human-readable name for this component instance.
    /// Changes are deferred until next ApplyUpdates() call.
    /// </summary>
    private ComponentPropertyUpdater<string> _nameUpdater;
    private string _name;
    public string Name => _name;

    partial void OnNameChanged(string oldValue);

    public void SetName(string value, InterpolationFunction<string>? interpolator = null)
    {
        _nameUpdater.Set(ref _name, value, interpolator);
    }

    public void SetCurrentName(string value)
    {
        if (value == _name) return;

        var oldValue = _name;
        _name = value;

        OnNameChanged(oldValue);
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
        _nameUpdater.Apply(ref _name, deltaTime);
    }
}