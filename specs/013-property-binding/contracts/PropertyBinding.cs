namespace Nexus.GameEngine.Components;

/// <summary>
/// Encapsulates the configuration and runtime state for a property binding.
/// Manages the lifecycle of event subscriptions between source and target components.
/// </summary>
public class PropertyBinding
{
    #region Fluent API - Static Factory Methods

    /// <summary>
    /// Creates a binding that searches for the first parent component of type T.
    /// </summary>
    public static PropertyBinding FromParent<T>() where T : IComponent;

    /// <summary>
    /// Creates a binding that searches siblings for the first component of type T.
    /// </summary>
    public static PropertyBinding FromSibling<T>() where T : IComponent;

    /// <summary>
    /// Creates a binding that searches immediate children for the first component of type T.
    /// </summary>
    public static PropertyBinding FromChild<T>() where T : IComponent;

    /// <summary>
    /// Creates a binding that searches up the tree for the first ancestor of type T.
    /// </summary>
    public static PropertyBinding FromContext<T>() where T : IComponent;

    /// <summary>
    /// Creates a binding that searches the tree for a component with the specified name.
    /// </summary>
    public static PropertyBinding FromNamedObject(string name);

    #endregion

    #region Fluent API - Configuration Methods

    /// <summary>
    /// Specifies the source property to bind to.
    /// </summary>
    /// <param name="propertyName">The name of the property on the source component.</param>
    public PropertyBinding GetPropertyValue(string propertyName);

    /// <summary>
    /// Adds a value converter to transform values during binding updates.
    /// </summary>
    public PropertyBinding WithConverter(IValueConverter converter);

    /// <summary>
    /// Adds a format string converter for converting values to formatted strings.
    /// </summary>
    /// <param name="format">The format string (e.g., "{0:F1}").</param>
    public PropertyBinding AsFormattedString(string format);

    /// <summary>
    /// Configures the binding for two-way synchronization (source ↔ target).
    /// </summary>
    /// <remarks>
    /// Requires IBidirectionalConverter if a converter is used.
    /// </remarks>
    public PropertyBinding TwoWay();

    #endregion

    #region Lifecycle Methods (Internal)

    /// <summary>
    /// Activates the binding by resolving the source component and subscribing to events.
    /// Performs initial synchronization of values.
    /// </summary>
    /// <param name="targetComponent">The component that owns this binding.</param>
    /// <param name="targetPropertyName">The name of the property to update on the target.</param>
    /// <remarks>
    /// Called during Component.OnActivate() lifecycle phase.
    /// - Resolves source component using LookupStrategy
    /// - Subscribes to source's PropertyChanged event
    /// - Performs initial sync (source → target)
    /// - If TwoWay mode, subscribes to target's PropertyChanged event
    /// </remarks>
    internal void Activate(IComponent targetComponent, string targetPropertyName);

    /// <summary>
    /// Deactivates the binding by unsubscribing from events and clearing references.
    /// </summary>
    /// <remarks>
    /// Called during Component.OnDeactivate() lifecycle phase.
    /// - Unsubscribes from source PropertyChanged event
    /// - Unsubscribes from target PropertyChanged event (TwoWay mode)
    /// - Clears cached component references
    /// </remarks>
    internal void Deactivate();

    #endregion
}
