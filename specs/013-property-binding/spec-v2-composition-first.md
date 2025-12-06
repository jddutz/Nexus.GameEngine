# Property Binding Specification (Composition-First)

## Executive Summary

This specification defines a **composition-first property binding system** where component relationships are **declared in templates at composition time**, not hardcoded in component classes at design time. This maintains component reusability while enabling declarative property synchronization.

**Key Insight**: You were absolutely right - we cannot use attributes on component properties like `[SyncFromParent<T>]` because the relationship between components is unknown until composition time. This would violate composition-first principles and force component inheritance for wiring.

## Core Philosophy

### The Composition-First Constraint

**Problem**: In a composition-first architecture, components must be generic and reusable. A `HealthBar` component doesn't inherently know it will be used with a `PlayerCharacter` or `HealthSystem` or `Enemy`. Those relationships are established when composing the component tree via templates.

**Wrong Approach** (violates composition-first):
```csharp
// ❌ Component class hardcodes relationship
public partial class HealthBar : RuntimeComponent
{
    [SyncFromParent<HealthSystem>(nameof(HealthSystem.Health))]
    public float Health { get; set; }
}
```

This forces:
- Component knows about specific parent types at design time
- Can't reuse HealthBar with different parent types
- Would need inheritance to create variants
- Violates open/closed principle

**Right Approach** (composition-first):
```csharp
// ✅ Generic component, no hardcoded relationships
public partial class HealthBar : RuntimeComponent
{
    [ComponentProperty]
    public float Health { get; set; }
}

// ✅ Relationship defined at composition time in template
new HealthSystemTemplate()
{
    Subcomponents = 
    [
        new HealthBarTemplate()
        {
            PropertyBindings = 
            [
                new("../Health", "Health")  // Parent.Health → this.Health
            ]
        }
    ]
}
```

## Goals

1. **Composition-First**: Relationships defined in templates, not component classes
2. **Declarative**: Everything visible in template configuration
3. **Type-Safe**: Compile-time checking where possible, runtime validation otherwise
4. **Explicit**: Data flow is clear and understandable
5. **Consistent**: Matches existing Template/Subcomponents pattern
6. **Performant**: Event-based (not polling), acceptable reflection cost

## Design

### 1. Property Change Notifications (Foundation)

Extend ComponentProperty to support change events (unchanged from original spec).

```csharp
[ComponentProperty(NotifyChange = true)]
public float Health { get; set; }
```

Generated code includes `PropertyChanged` event.

### 2. PropertyBindingTemplate Record

Add property bindings to base Template record:

```csharp
public record Template
{
    public string? Name { get; set; }
    public Template[] Subcomponents { get; init; } = [];
    
    /// <summary>
    /// Property bindings established when this component is created.
    /// Bindings synchronize properties between this component and related components.
    /// </summary>
    public PropertyBindingTemplate[] PropertyBindings { get; init; } = [];
}

/// <summary>
/// Defines a property binding between a source and target property.
/// Bindings are established during component creation from templates.
/// </summary>
public record PropertyBindingTemplate
{
    /// <summary>
    /// Path to source property using relative syntax:
    /// - "PropertyName" = property on this component
    /// - "../PropertyName" = property on parent
    /// - "../../PropertyName" = property on grandparent
    /// - "$Context&lt;ThemeContext&gt;/PropertyName" = property on context provider
    /// - "ChildName/PropertyName" = property on named child
    /// </summary>
    public string SourcePath { get; init; } = string.Empty;
    
    /// <summary>
    /// Name of property on this component to update.
    /// </summary>
    public string TargetProperty { get; init; } = string.Empty;
    
    /// <summary>
    /// Binding mode: OneWay, TwoWay, OneTime, etc.
    /// </summary>
    public BindingMode Mode { get; init; } = BindingMode.OneWay;
    
    /// <summary>
    /// Optional converter for type conversions or transformations.
    /// </summary>
    public IValueConverter? Converter { get; init; }
    
    /// <summary>
    /// Optional validator for value validation.
    /// </summary>
    public IValueValidator? Validator { get; init; }
    
    // Convenience constructors
    public PropertyBindingTemplate() { }
    
    public PropertyBindingTemplate(string sourcePath, string targetProperty, BindingMode mode = BindingMode.OneWay)
    {
        SourcePath = sourcePath;
        TargetProperty = targetProperty;
        Mode = mode;
    }
}

public enum BindingMode
{
    OneWay,         // Source → Target
    TwoWay,         // Source ↔ Target  
    OneTime,        // Source → Target (once, then disconnect)
    OneWayToSource  // Source ← Target
}
```

### 3. Binding Resolution and Application

Bindings are resolved and established during component creation from template:

```csharp
// In RuntimeComponent or base Component class
public class RuntimeComponent : Component, IRuntimeComponent
{
    // Stores active property bindings for this component
    private readonly List<ActivePropertyBinding> _propertyBindings = new();
    
    protected override void OnLoad(Template? template)
    {
        base.OnLoad(template);
        
        // Establish property bindings from template
        if (template?.PropertyBindings != null)
        {
            foreach (var bindingTemplate in template.PropertyBindings)
            {
                EstablishBinding(bindingTemplate);
            }
        }
    }
    
    private void EstablishBinding(PropertyBindingTemplate template)
    {
        // Resolve source component and property
        var (sourceComponent, sourceProperty) = ResolveSourcePath(template.SourcePath);
        if (sourceComponent == null || sourceProperty == null)
        {
            Logger?.LogWarning($"Cannot resolve source path: {template.SourcePath}");
            return;
        }
        
        // Get target property on this component
        var targetProperty = this.GetType().GetProperty(template.TargetProperty);
        if (targetProperty == null)
        {
            Logger?.LogWarning($"Target property not found: {template.TargetProperty}");
            return;
        }
        
        // Create active binding
        var binding = new ActivePropertyBinding(
            sourceComponent, 
            sourceProperty,
            this,
            targetProperty,
            template.Mode,
            template.Converter,
            template.Validator
        );
        
        _propertyBindings.Add(binding);
    }
    
    private (IComponent? component, PropertyInfo? property) ResolveSourcePath(string path)
    {
        // Parse path syntax
        if (path.StartsWith("../"))
        {
            // Parent navigation
            var levels = path.Count(c => c == '/') - 1;
            var propertyName = path.Split('/').Last();
            
            var component = this.Parent;
            for (int i = 1; i < levels && component != null; i++)
                component = component.Parent;
                
            return (component, component?.GetType().GetProperty(propertyName));
        }
        else if (path.StartsWith("$Context<"))
        {
            // Context lookup: $Context<ThemeContext>/PropertyName
            var match = Regex.Match(path, @"\$Context<(.+?)>/(.+)");
            if (match.Success)
            {
                var contextTypeName = match.Groups[1].Value;
                var propertyName = match.Groups[2].Value;
                
                // Search up tree for context provider
                var contextType = Type.GetType(contextTypeName);
                var context = this.FindAncestor(c => contextType?.IsAssignableFrom(c.GetType()) ?? false);
                
                return (context, context?.GetType().GetProperty(propertyName));
            }
        }
        else if (path.Contains('/'))
        {
            // Child navigation: ChildName/PropertyName
            var parts = path.Split('/');
            var childName = parts[0];
            var propertyName = parts[1];
            
            var child = Children.FirstOrDefault(c => c.Name == childName);
            return (child, child?.GetType().GetProperty(propertyName));
        }
        else
        {
            // Property on this component
            return (this, this.GetType().GetProperty(path));
        }
        
        return (null, null);
    }
    
    protected override void OnActivate()
    {
        base.OnActivate();
        
        // Subscribe to source property changes
        foreach (var binding in _propertyBindings)
        {
            binding.Activate();
        }
    }
    
    protected override void OnDeactivate()
    {
        // Unsubscribe from property changes
        foreach (var binding in _propertyBindings)
        {
            binding.Deactivate();
        }
        
        base.OnDeactivate();
    }
}

/// <summary>
/// Represents an active property binding between source and target.
/// Handles subscription to PropertyChanged events and value synchronization.
/// </summary>
internal class ActivePropertyBinding
{
    private readonly IComponent _sourceComponent;
    private readonly PropertyInfo _sourceProperty;
    private readonly IComponent _targetComponent;
    private readonly PropertyInfo _targetProperty;
    private readonly BindingMode _mode;
    private readonly IValueConverter? _converter;
    private readonly IValueValidator? _validator;
    
    public ActivePropertyBinding(
        IComponent sourceComponent,
        PropertyInfo sourceProperty,
        IComponent targetComponent,
        PropertyInfo targetProperty,
        BindingMode mode,
        IValueConverter? converter,
        IValueValidator? validator)
    {
        _sourceComponent = sourceComponent;
        _sourceProperty = sourceProperty;
        _targetComponent = targetComponent;
        _targetProperty = targetProperty;
        _mode = mode;
        _converter = converter;
        _validator = validator;
    }
    
    public void Activate()
    {
        // Subscribe to source PropertyChanged event
        if (_sourceComponent is IRuntimeComponent source)
        {
            source.PropertyChanged += OnSourcePropertyChanged;
        }
        
        // Initial sync
        SyncSourceToTarget();
        
        // For TwoWay bindings, also subscribe to target changes
        if (_mode == BindingMode.TwoWay && _targetComponent is IRuntimeComponent target)
        {
            target.PropertyChanged += OnTargetPropertyChanged;
        }
    }
    
    public void Deactivate()
    {
        if (_sourceComponent is IRuntimeComponent source)
        {
            source.PropertyChanged -= OnSourcePropertyChanged;
        }
        
        if (_mode == BindingMode.TwoWay && _targetComponent is IRuntimeComponent target)
        {
            target.PropertyChanged -= OnTargetPropertyChanged;
        }
    }
    
    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _sourceProperty.Name)
        {
            SyncSourceToTarget();
            
            // For OneTime bindings, disconnect after first sync
            if (_mode == BindingMode.OneTime)
            {
                Deactivate();
            }
        }
    }
    
    private void OnTargetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == _targetProperty.Name && _mode == BindingMode.TwoWay)
        {
            SyncTargetToSource();
        }
    }
    
    private void SyncSourceToTarget()
    {
        var value = _sourceProperty.GetValue(_sourceComponent);
        
        // Apply converter if present
        if (_converter != null)
        {
            value = _converter.Convert(value, _targetProperty.PropertyType);
        }
        
        // Validate if validator present
        if (_validator != null && !_validator.Validate(value))
        {
            return; // Validation failed, don't update
        }
        
        _targetProperty.SetValue(_targetComponent, value);
    }
    
    private void SyncTargetToSource()
    {
        var value = _targetProperty.GetValue(_targetComponent);
        
        // Apply converter in reverse if present
        if (_converter != null)
        {
            value = _converter.ConvertBack(value, _sourceProperty.PropertyType);
        }
        
        _sourceProperty.SetValue(_sourceComponent, value);
    }
}
```

### 4. Usage Examples

#### Example 1: Simple Parent-Child Sync

```csharp
// Generic components - no hardcoded relationships
public partial class HealthSystem : RuntimeComponent
{
    [ComponentProperty(NotifyChange = true)]
    public float Health { get; set; } = 100f;
}

public partial class HealthBar : RuntimeComponent  
{
    [ComponentProperty]
    public float Health { get; set; }
}

// Relationship defined in template
var template = new HealthSystemTemplate()
{
    Health = 100f,
    Subcomponents = 
    [
        new HealthBarTemplate()
        {
            PropertyBindings = 
            [
                new PropertyBindingTemplate("../Health", "Health")
            ]
        }
    ]
};
```

#### Example 2: Theme Context

```csharp
public partial class ThemeContext : RuntimeComponent, IContextProvider
{
    [ComponentProperty(NotifyChange = true)]
    public Color PrimaryColor { get; set; } = Colors.Blue;
}

public partial class Button : RuntimeComponent
{
    [ComponentProperty]
    public Color BackgroundColor { get; set; }
}

// Usage
new ThemeContextTemplate()
{
    PrimaryColor = Colors.DarkBlue,
    Subcomponents = 
    [
        new PanelTemplate()
        {
            Subcomponents = 
            [
                new ButtonTemplate()
                {
                    PropertyBindings = 
                    [
                        new("$Context<ThemeContext>/PrimaryColor", "BackgroundColor")
                    ]
                }
            ]
        }
    ]
}
```

#### Example 3: Sibling Property Binding

```csharp
new PanelTemplate()
{
    Name = "Container",
    Subcomponents = 
    [
        new SliderTemplate()
        {
            Name = "VolumeSlider",
            // Slider has Value property
        },
        new TextElementTemplate()
        {
            PropertyBindings = 
            [
                // Bind to sibling's property
                new("../VolumeSlider/Value", "Text", 
                    converter: new FloatToStringConverter())
            ]
        }
    ]
}
```

#### Example 4: Two-Way Binding

```csharp
new InputFieldTemplate()
{
    PropertyBindings = 
    [
        new("../PlayerName", "Text", BindingMode.TwoWay)
    ]
}
```

## Advantages of This Approach

### ✅ Composition-First
- Components remain generic and reusable
- No hardcoded relationships in component classes
- Same component can be used in different contexts with different bindings

### ✅ Declarative
- All bindings visible in template configuration
- Easy to understand data flow from template structure
- Matches existing Subcomponents pattern

### ✅ Consistent with Architecture
- Follows existing Template pattern
- Uses familiar path syntax (like file systems)
- Leverages existing ComponentProperty system

### ✅ Flexible
- Supports parent, child, sibling, context bindings
- Can add validation and conversion
- Supports all binding modes (OneWay, TwoWay, etc.)

### ✅ Runtime Performance
- Event-based (no polling)
- Automatic cleanup on deactivation
- Reflection only during binding setup, not during updates

### ✅ No Code Generation Required
- Pure runtime solution
- No source generator complexity
- Easy to debug and understand

## Performance Considerations

### Reflection Cost
- **One-time cost**: During binding setup in OnLoad()
- **Ongoing cost**: Property.GetValue/SetValue in event handlers
- **Mitigation**: Can cache delegates or use expression trees if profiling shows issues

### Memory
- Each binding: ~200 bytes (2 component refs, 2 PropertyInfo, delegates)
- Typical UI: 10-50 bindings = 2-10KB total
- Auto-cleanup prevents leaks

### Event Overhead
- Each PropertyChanged notification: ~1μs
- Only fired when properties actually change
- Acceptable for UI updates (not per-frame graphics)

## Migration from Existing Code

Existing manual approaches continue to work:

```csharp
// Old approach (manual events)
protected override void OnActivate()
{
    var healthSystem = this.FindParent<HealthSystem>();
    healthSystem.PropertyChanged += OnHealthChanged;
}

// New approach (declarative binding in template)
new HealthBarTemplate()
{
    PropertyBindings = [new("../Health", "Health")]
}
```

## Open Questions

1. **Should bindings be validated at template creation time?**
   - Pro: Catch errors early
   - Con: Requires type information at template construction
   - **Decision**: Runtime validation with helpful error messages

2. **Should we support expression-based bindings for computed values?**
   ```csharp
   new("../Health / ../MaxHealth", "HealthPercentage", 
       evaluator: new ExpressionEvaluator())
   ```
   - **Decision**: Phase 2 feature, use manual OnUpdate for now

3. **Should bindings be cached/pooled?**
   - **Decision**: Implement if profiling shows allocation issues

4. **Path syntax - use "/" or "."?**
   - **Decision**: "/" matches file systems, more familiar

## Implementation Phases

### Phase 1: Foundation
- Add PropertyBindingTemplate to base Template record
- Implement path resolution (parent, child, context)
- Basic OneWay binding support
- Unit tests for path resolution

### Phase 2: Full Features  
- TwoWay, OneTime, OneWayToSource modes
- IValueConverter and IValueValidator support
- Error handling and validation
- Integration tests

### Phase 3: Optimization
- Expression tree compilation for property access
- Binding caching/pooling if needed
- Performance benchmarks

### Phase 4: Developer Experience
- Analyzer warnings for invalid paths
- IntelliSense support for path syntax (if possible)
- Documentation and examples

## Comparison to Original Spec

| Aspect | Original (Attributes) | Revised (Templates) |
|--------|---------------------|-------------------|
| Component Classes | Modified with attributes | Unchanged, generic |
| Relationships | Design-time (hardcoded) | Composition-time (declarative) |
| Reusability | Limited (inheritance needed) | High (same component, different bindings) |
| Code Generation | Required | Not required |
| Type Safety | Compile-time | Runtime-validated |
| Composition-First | ❌ Violated | ✅ Preserved |
| Matches Unity/Godot | No | Yes (like inspector bindings) |

## Conclusion

This revised approach **fully embraces composition-first principles** by moving all relationship definitions from component class design time to component tree composition time via templates. This matches how other engines (Unity, Unreal, Godot) handle property bindings - they're configured in the scene/prefab/blueprint, not hardcoded in scripts.

The path-based syntax provides a clear, declarative way to express component relationships while maintaining component reusability. The Template-based approach is consistent with existing engine patterns and requires no source generation, making it simpler to implement and debug.
