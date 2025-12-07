using System;
using System.Linq.Expressions;
using System.Reflection;
using Nexus.GameEngine.Components.Lookups;
using Nexus.GameEngine.Data;
using Nexus.GameEngine.Events;

namespace Nexus.GameEngine.Components;

/// <summary>
/// Non-generic interface for property bindings to enable collection storage and lifecycle management.
/// </summary>
public interface IPropertyBinding
{
    /// <summary>
    /// Activates the binding by resolving source component and subscribing to property changes.
    /// </summary>
    void Activate(IComponent targetComponent, string targetPropertyName);
    
    /// <summary>
    /// Deactivates the binding by unsubscribing from events and clearing references.
    /// </summary>
    void Deactivate();
}

/// <summary>
/// Property binding transformation pipeline.
/// TSource: The source component type (constant through pipeline)
/// TValue: The current value type flowing through the pipeline (transforms at each step)
/// </summary>
/// <typeparam name="TSource">Source component type</typeparam>
/// <typeparam name="TValue">Current value type in the pipeline</typeparam>
public class PropertyBinding<TSource, TValue> : IPropertyBinding where TSource : class, IComponent
{
    private ILookupStrategy? _lookupStrategy;
    private string? _sourcePropertyName;
    private IValueConverter? _converter;
    private BindingMode _mode = BindingMode.OneWay;
    private Func<TSource, TValue>? _transform;

    // Runtime state
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
    
    // Private constructor for pipeline transformations
    private PropertyBinding(ILookupStrategy? strategy, string? sourceProp, Func<TSource, TValue>? transform, IValueConverter? converter = null, BindingMode mode = BindingMode.OneWay)
    {
        _lookupStrategy = strategy;
        _sourcePropertyName = sourceProp;
        _transform = transform;
        _converter = converter;
        _mode = mode;
    }

    #region Fluent API - Property Extraction and Transformation

    /// <summary>
    /// Extracts a property value from the source component.
    /// Type inference: compiler infers TProp from the lambda expression.
    /// </summary>
    /// <typeparam name="TProp">Property type (inferred from lambda)</typeparam>
    /// <param name="selector">Lambda expression selecting the property</param>
    /// <returns>New binding with TValue = TProp</returns>
    public PropertyBinding<TSource, TProp> GetPropertyValue<TProp>(Expression<Func<TSource, TProp>> selector)
    {
        string propName = ExtractPropertyName(selector);
        var compiled = selector.Compile();
        
        return new PropertyBinding<TSource, TProp>(
            _lookupStrategy,
            propName,
            compiled,
            _converter,
            _mode
        );
    }

    /// <summary>
    /// Converts the current value to a formatted string.
    /// </summary>
    /// <param name="format">Format string (e.g., "Health: {0:F0}")</param>
    /// <returns>New binding with TValue = string</returns>
    public PropertyBinding<TSource, string> AsFormattedString(string format)
    {
        var converter = new StringFormatConverter(format);
        
        return new PropertyBinding<TSource, string>(
            _lookupStrategy,
            _sourcePropertyName,
            source =>
            {
                var value = _transform != null ? _transform(source) : (object?)source;
                return converter.Convert(value) as string ?? "";
            },
            converter,
            _mode
        );
    }

    /// <summary>
    /// Applies a custom converter to transform the current value.
    /// </summary>
    /// <param name="converter">Converter to apply</param>
    /// <returns>Current binding instance</returns>
    public PropertyBinding<TSource, TValue> WithConverter(IValueConverter converter)
    {
        _converter = converter;
        return this;
    }

    /// <summary>
    /// Configures the binding for two-way synchronization (source ↔ target).
    /// </summary>
    /// <returns>Current binding instance</returns>
    public PropertyBinding<TSource, TValue> TwoWay()
    {
        _mode = BindingMode.TwoWay;
        return this;
    }

    /// <summary>
    /// Terminal method: registers the binding and returns default value for template property assignment.
    /// Type inference: compiler infers TValue? as return type.
    /// </summary>
    /// <returns>Default value of TValue (null for reference types, 0 for value types)</returns>
    public TValue? Bind()
    {
        // In real implementation, this would register with a binding manager
        // For now, return default to satisfy template property assignment
        return default;
    }

    #endregion

    #region Helper Methods

    private static string ExtractPropertyName<T, TProp>(Expression<Func<T, TProp>> expr)
    {
        if (expr.Body is MemberExpression member)
            return member.Member.Name;
        throw new ArgumentException("Expression must be a property access");
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
    public void Activate(IComponent targetComponent, string targetPropertyName)
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
    public void Deactivate()
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

/// <summary>
/// Static factory class for creating property bindings with various lookup strategies.
/// </summary>
public static class Binding
{
    /// <summary>
    /// Creates a binding that searches for the first parent component of type TSource.
    /// </summary>
    /// <typeparam name="TSource">The type of the parent component to find</typeparam>
    /// <returns>PropertyBinding configured with ParentLookup strategy</returns>
    public static PropertyBinding<TSource, TSource> FromParent<TSource>() where TSource : class, IComponent
    {
        return new PropertyBinding<TSource, TSource>(new ParentLookup<TSource>());
    }

    /// <summary>
    /// Creates a binding that searches the entire component tree for a component with the specified name.
    /// </summary>
    /// <typeparam name="TSource">The expected type of the named component</typeparam>
    /// <param name="name">The name of the component to find</param>
    /// <returns>PropertyBinding configured with NamedObjectLookup strategy</returns>
    public static PropertyBinding<TSource, TSource> FromNamedObject<TSource>(string name) where TSource : class, IComponent
    {
        return new PropertyBinding<TSource, TSource>(new NamedObjectLookup(name));
    }

    /// <summary>
    /// Creates a binding that searches siblings for the first component of type TSource.
    /// </summary>
    /// <typeparam name="TSource">The type of the sibling component to find</typeparam>
    /// <returns>PropertyBinding configured with SiblingLookup strategy</returns>
    public static PropertyBinding<TSource, TSource> FromSibling<TSource>() where TSource : class, IComponent
    {
        return new PropertyBinding<TSource, TSource>(new SiblingLookup<TSource>());
    }

    /// <summary>
    /// Creates a binding that searches immediate children for the first component of type TSource.
    /// </summary>
    /// <typeparam name="TSource">The type of the child component to find</typeparam>
    /// <returns>PropertyBinding configured with ChildLookup strategy</returns>
    public static PropertyBinding<TSource, TSource> FromChild<TSource>() where TSource : class, IComponent
    {
        return new PropertyBinding<TSource, TSource>(new ChildLookup<TSource>());
    }

    /// <summary>
    /// Creates a binding that searches up the tree for the first ancestor of type TSource.
    /// </summary>
    /// <typeparam name="TSource">The type of the ancestor component to find</typeparam>
    /// <returns>PropertyBinding configured with ContextLookup strategy</returns>
    public static PropertyBinding<TSource, TSource> FromContext<TSource>() where TSource : class, IComponent
    {
        return new PropertyBinding<TSource, TSource>(new ContextLookup<TSource>());
    }
}
