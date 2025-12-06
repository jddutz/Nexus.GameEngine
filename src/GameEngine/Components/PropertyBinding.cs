using System;
using System.Reflection;
using Nexus.GameEngine.Components.Lookups;
using Nexus.GameEngine.Data;
using Nexus.GameEngine.Events;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Encapsulates the configuration and runtime state for a property binding.
/// Manages the lifecycle of event subscriptions between source and target components.
/// </summary>
public class PropertyBinding
{
    private ILookupStrategy? _lookupStrategy;
    private string? _sourcePropertyName;
    private IValueConverter? _converter;
    private BindingMode _mode = BindingMode.OneWay;

    private IComponent? _sourceComponent;
    private EventInfo? _sourceEvent;
    private Delegate? _sourceHandler;
    
    private IComponent? _targetComponent;
    private PropertyInfo? _targetProperty;
    private EventInfo? _targetEvent;
    private Delegate? _targetHandler;
    private PropertyInfo? _sourceProperty;

    private bool _isUpdating;

    internal PropertyBinding(ILookupStrategy strategy)
    {
        _lookupStrategy = strategy;
    }

    #region Fluent API - Static Factory Methods

    /// <summary>
    /// Creates a binding that searches for the first parent component of type T.
    /// </summary>
    /// <typeparam name="T">The type of the parent component to find.</typeparam>
    /// <returns>A new PropertyBinding instance configured with ParentLookup strategy.</returns>
    public static PropertyBinding FromParent<T>() where T : class, IComponent
    {
        return new PropertyBinding(new ParentLookup<T>());
    }

    /// <summary>
    /// Creates a binding that searches the entire component tree for a component with the specified name.
    /// </summary>
    /// <param name="name">The name of the component to find.</param>
    /// <returns>A new PropertyBinding instance configured with NamedObjectLookup strategy.</returns>
    public static PropertyBinding FromNamedObject(string name)
    {
        return new PropertyBinding(new NamedObjectLookup(name));
    }

    /// <summary>
    /// Creates a binding that searches siblings for the first component of type T.
    /// </summary>
    /// <typeparam name="T">The type of the sibling component to find.</typeparam>
    /// <returns>A new PropertyBinding instance configured with SiblingLookup strategy.</returns>
    public static PropertyBinding FromSibling<T>() where T : IComponent
    {
        return new PropertyBinding(new SiblingLookup<T>());
    }

    /// <summary>
    /// Creates a binding that searches immediate children for the first component of type T.
    /// </summary>
    /// <typeparam name="T">The type of the child component to find.</typeparam>
    /// <returns>A new PropertyBinding instance configured with ChildLookup strategy.</returns>
    public static PropertyBinding FromChild<T>() where T : IComponent
    {
        return new PropertyBinding(new ChildLookup<T>());
    }

    /// <summary>
    /// Creates a binding that searches up the tree for the first ancestor of type T.
    /// </summary>
    /// <typeparam name="T">The type of the ancestor component to find.</typeparam>
    /// <returns>A new PropertyBinding instance configured with ContextLookup strategy.</returns>
    public static PropertyBinding FromContext<T>() where T : IComponent
    {
        return new PropertyBinding(new ContextLookup<T>());
    }

    #endregion

    #region Fluent API - Configuration Methods

    /// <summary>
    /// Specifies the source property to bind to.
    /// </summary>
    /// <param name="propertyName">The name of the property on the source component.</param>
    /// <returns>The current PropertyBinding instance for chaining.</returns>
    public PropertyBinding GetPropertyValue(string propertyName)
    {
        _sourcePropertyName = propertyName;
        return this;
    }

    /// <summary>
    /// Adds a value converter to transform values during binding updates.
    /// </summary>
    /// <param name="converter">The converter to use.</param>
    /// <returns>The current PropertyBinding instance for chaining.</returns>
    public PropertyBinding WithConverter(IValueConverter converter)
    {
        _converter = converter;
        return this;
    }

    /// <summary>
    /// Adds a format string converter for converting values to formatted strings.
    /// </summary>
    /// <param name="format">The format string (e.g., "{0:F1}").</param>
    /// <returns>The current PropertyBinding instance for chaining.</returns>
    public PropertyBinding AsFormattedString(string format)
    {
        _converter = new StringFormatConverter(format);
        return this;
    }

    /// <summary>
    /// Configures the binding for two-way synchronization (source ↔ target).
    /// </summary>
    /// <remarks>
    /// Requires IBidirectionalConverter if a converter is used.
    /// </remarks>
    /// <returns>The current PropertyBinding instance for chaining.</returns>
    public PropertyBinding TwoWay()
    {
        _mode = BindingMode.TwoWay;
        return this;
    }

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
    internal void Activate(IComponent targetComponent, string targetPropertyName)
    {
        if (_lookupStrategy == null) throw new InvalidOperationException("Lookup strategy not set.");
        if (_sourcePropertyName == null) throw new InvalidOperationException("Source property name not set.");

        _sourceComponent = _lookupStrategy.Resolve(targetComponent);
        if (_sourceComponent == null) return;

        var sourceType = _sourceComponent.GetType();
        var sourceProp = sourceType.GetProperty(_sourcePropertyName);
        if (sourceProp == null) return;

        var targetType = targetComponent.GetType();
        var targetProp = targetType.GetProperty(targetPropertyName);
        if (targetProp == null) return;

        // Initial sync
        var value = sourceProp.GetValue(_sourceComponent);
        if (_converter != null)
        {
            value = _converter.Convert(value);
            if (value == null) return;
        }
        targetProp.SetValue(targetComponent, value);

        // Subscribe
        var eventName = $"{_sourcePropertyName}Changed";
        _sourceEvent = sourceType.GetEvent(eventName);
        
        if (_sourceEvent != null)
        {
            var propType = sourceProp.PropertyType;
            var eventArgsType = typeof(PropertyChangedEventArgs<>).MakeGenericType(propType);
            var eventHandlerType = typeof(EventHandler<>).MakeGenericType(eventArgsType);

            var methodInfo = this.GetType().GetMethod(nameof(OnSourcePropertyChanged), BindingFlags.Instance | BindingFlags.NonPublic)
                ?.MakeGenericMethod(propType);

            if (methodInfo != null)
            {
                _targetComponent = targetComponent;
                _targetProperty = targetProp;
                _sourceProperty = sourceProp;
                
                _sourceHandler = Delegate.CreateDelegate(eventHandlerType, this, methodInfo);
                _sourceEvent.AddEventHandler(_sourceComponent, _sourceHandler);
            }
        }

        // Two-Way Subscription
        if (_mode == BindingMode.TwoWay)
        {
            var targetEventName = $"{targetPropertyName}Changed";
            _targetEvent = targetType.GetEvent(targetEventName);

            if (_targetEvent != null)
            {
                var propType = targetProp.PropertyType;
                var eventArgsType = typeof(PropertyChangedEventArgs<>).MakeGenericType(propType);
                var eventHandlerType = typeof(EventHandler<>).MakeGenericType(eventArgsType);

                var methodInfo = this.GetType().GetMethod(nameof(OnTargetPropertyChanged), BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.MakeGenericMethod(propType);

                if (methodInfo != null)
                {
                    _targetHandler = Delegate.CreateDelegate(eventHandlerType, this, methodInfo);
                    _targetEvent.AddEventHandler(_targetComponent, _targetHandler);
                }
            }
        }
    }

    private void OnSourcePropertyChanged<T>(object sender, PropertyChangedEventArgs<T> e)
    {
        if (_isUpdating) return;

        if (_targetComponent != null && _targetProperty != null)
        {
            try
            {
                _isUpdating = true;
                object? value = e.NewValue;
                if (_converter != null)
                {
                    value = _converter.Convert(value);
                    if (value == null) return;
                }
                _targetProperty.SetValue(_targetComponent, value);
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }

    private void OnTargetPropertyChanged<T>(object sender, PropertyChangedEventArgs<T> e)
    {
        if (_isUpdating) return;

        if (_sourceComponent != null && _sourceProperty != null)
        {
            try
            {
                _isUpdating = true;
                object? value = e.NewValue;
                if (_converter != null)
                {
                    if (_converter is IBidirectionalConverter bidirectional)
                    {
                        value = bidirectional.ConvertBack(value);
                    }
                    else
                    {
                        // Cannot convert back without bidirectional converter
                        return;
                    }
                    
                    if (value == null) return;
                }
                _sourceProperty.SetValue(_sourceComponent, value);
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }

    /// <summary>
    /// Deactivates the binding by unsubscribing from events and clearing references.
    /// </summary>
    /// <remarks>
    /// Called during Component.OnDeactivate() lifecycle phase.
    /// - Unsubscribes from source PropertyChanged event
    /// - Unsubscribes from target PropertyChanged event (TwoWay mode)
    /// - Clears cached component references
    /// </remarks>
    internal void Deactivate()
    {
        if (_sourceComponent != null && _sourceEvent != null && _sourceHandler != null)
        {
            _sourceEvent.RemoveEventHandler(_sourceComponent, _sourceHandler);
        }

        if (_targetComponent != null && _targetEvent != null && _targetHandler != null)
        {
            _targetEvent.RemoveEventHandler(_targetComponent, _targetHandler);
        }
        
        _sourceComponent = null;
        _sourceEvent = null;
        _sourceHandler = null;
        _sourceProperty = null;
        
        _targetComponent = null;
        _targetProperty = null;
        _targetEvent = null;
        _targetHandler = null;
    }

    #endregion
}
