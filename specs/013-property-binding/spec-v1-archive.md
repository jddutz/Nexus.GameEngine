# Property Binding Specification

## Executive Summary

This specification defines a **composition-first property binding system** for Nexus.GameEngine that enables components to wire up properties declaratively while maintaining the engine's zero-overhead, source-generation-based architecture.

The system provides multiple binding mechanisms optimized for different use cases:
- **SyncFromParent** - Simple parent→child value synchronization (most common)
- **FromContext** - Global/shared values accessible to descendant tree
- **PropertyBinding** - First-class component for complex scenarios (validation, conversion)
- **Manual Events** - Escape hatch for custom logic

## Goals

1. **Composition-First**: Properties flow naturally through component hierarchy
2. **Type-Safe**: Compile-time checking, no string-based reflection
3. **Zero-Overhead**: Source generation, no runtime cost beyond direct property access
4. **Explicit**: Data flow is visible and understandable
5. **Consistent**: Builds on existing ComponentProperty system
6. **Flexible**: Multiple tools for different complexity levels

## Non-Goals

- Full WPF-style binding infrastructure (too complex, doesn't align with composition)
- Reactive programming model (too much magic)
- Automatic dependency tracking (performance overhead)
- Bi-directional synchronization as default (prefer unidirectional flow)

## Current State

### ComponentProperty System

Properties marked with `[ComponentProperty]` are source-generated with:
- Backing fields for current and target values
- Deferred updates (applied during `ApplyUpdates()`)
- Optional animation/interpolation
- **Missing**: No change notification mechanism

### Gaps

- No way to connect child properties to parent values
- No PropertyChanged events
- Manual wiring required for related properties
- IDataBinding interface exists but is unimplemented

## Design

### 1. Property Change Notifications (Foundation)

Extend ComponentProperty source generator to optionally emit change notifications.

#### Attribute

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class ComponentPropertyAttribute : Attribute
{
    /// <summary>
    /// Whether to generate PropertyChanged notification for this property.
    /// Default: false (no notification overhead unless needed)
    /// </summary>
    public bool NotifyChange { get; set; } = false;
}
```

#### Generated Code

```csharp
// Source
public partial class HealthSystem : RuntimeComponent
{
    [ComponentProperty(NotifyChange = true)]
    public float Health { get; set; } = 100f;
}

// Generated
partial class HealthSystem
{
    private float _health = 100f;
    private float _targetHealth = 100f;
    
    public float Health
    {
        get => _health;
        set
        {
            if (_targetHealth == value) return;
            _targetHealth = value;
        }
    }
    
    partial void ApplyUpdates(double deltaTime)
    {
        if (_targetHealth != _health)
        {
            var oldValue = _health;
            _health = _targetHealth;
            OnPropertyChanged(nameof(Health), oldValue, _health);
        }
    }
    
    // Event generated only if ANY property has NotifyChange = true
    public event EventHandler<PropertyChangedEventArgs>? PropertyChanged;
    
    protected virtual void OnPropertyChanged(string propertyName, object? oldValue, object? newValue)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName, oldValue, newValue));
    }
}

public class PropertyChangedEventArgs : EventArgs
{
    public string PropertyName { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
}
```

**Key Points**:
- Opt-in: Only generates events when `NotifyChange = true`
- Fires AFTER value is applied (during `ApplyUpdates()`)
- Includes old/new values in event args
- Virtual `OnPropertyChanged` for subclass extension

### 2. SyncFromParent Attribute (Primary Mechanism)

Simple parent→child synchronization for most common use case.

#### Attribute

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class SyncFromParentAttribute<TParent> : Attribute 
    where TParent : IRuntimeComponent
{
    public string SourceProperty { get; }
    
    public SyncFromParentAttribute(string sourceProperty)
    {
        SourceProperty = sourceProperty;
    }
}
```

#### Usage

```csharp
public partial class HealthSystem : RuntimeComponent
{
    [ComponentProperty(NotifyChange = true)]  // Required for sync
    public float Health { get; set; } = 100f;
}

public partial class HealthBar : RuntimeComponent
{
    [ComponentProperty]
    [SyncFromParent<HealthSystem>(nameof(HealthSystem.Health))]
    public float Health { get; set; }
}
```

#### Generated Code

```csharp
partial class HealthBar
{
    private HealthSystem? _healthSystemParent;
    
    protected override void OnActivate()
    {
        base.OnActivate();
        
        // Find parent of specified type
        _healthSystemParent = this.FindAncestor<HealthSystem>();
        
        if (_healthSystemParent != null)
        {
            // Subscribe to parent's property changes
            _healthSystemParent.PropertyChanged += OnHealthSystemParentPropertyChanged;
            
            // Initial sync
            Health = _healthSystemParent.Health;
        }
    }
    
    private void OnHealthSystemParentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HealthSystem.Health))
        {
            Health = _healthSystemParent!.Health;
        }
    }
    
    protected override void OnDeactivate()
    {
        // Auto cleanup
        if (_healthSystemParent != null)
        {
            _healthSystemParent.PropertyChanged -= OnHealthSystemParentPropertyChanged;
            _healthSystemParent = null;
        }
        
        base.OnDeactivate();
    }
}
```

**Key Points**:
- Finds parent during `OnActivate()`
- Automatic subscription/unsubscription (no memory leaks)
- Compile-time checking of property names via `nameof()`
- Initial value sync on activation
- Analyzer warning if source property doesn't have `NotifyChange = true`

#### Analyzer Rules

**NX1003**: Source property must have `NotifyChange = true`
```csharp
[ComponentProperty]  // ❌ Error: Missing NotifyChange = true
public float Health { get; set; }

[ComponentProperty(NotifyChange = true)]  // ✅ OK
public float Health { get; set; }
```

**NX1004**: Parent type must exist in ancestor chain (runtime check, warning only)

### 3. FromContext Attribute (Global State)

Access values from context components higher in the tree.

#### Context Component

```csharp
/// <summary>
/// Marker interface for components that provide context values to descendants
/// </summary>
public interface IContextProvider { }

public partial class ThemeContext : RuntimeComponent, IContextProvider
{
    [ComponentProperty(NotifyChange = true)]
    public Theme Theme { get; set; } = Theme.Dark;
    
    [ComponentProperty(NotifyChange = true)]
    public Color AccentColor { get; set; } = Color.Blue;
}
```

#### Attribute

```csharp
[AttributeUsage(AttributeTargets.Property)]
public class FromContextAttribute<TContext> : Attribute
    where TContext : IRuntimeComponent, IContextProvider
{
    public string SourceProperty { get; }
    
    public FromContextAttribute(string sourceProperty)
    {
        SourceProperty = sourceProperty;
    }
}
```

#### Usage

```csharp
public partial class Button : RuntimeComponent
{
    [ComponentProperty]
    [FromContext<ThemeContext>(nameof(ThemeContext.AccentColor))]
    public Color AccentColor { get; set; }
}

// Component tree
new ThemeContext()
{
    Theme = Theme.Dark,
    Subcomponents =
    [
        new Panel()
        {
            Subcomponents = 
            [
                new Button()  // Gets AccentColor from ThemeContext
            ]
        }
    ]
}
```

#### Generated Code

Similar to `SyncFromParent`, but searches upward for `IContextProvider`:

```csharp
partial class Button
{
    private ThemeContext? _themeContext;
    
    protected override void OnActivate()
    {
        base.OnActivate();
        
        // Search up tree for context provider
        _themeContext = this.FindAncestor<ThemeContext>();
        
        if (_themeContext != null)
        {
            _themeContext.PropertyChanged += OnThemeContextPropertyChanged;
            AccentColor = _themeContext.AccentColor;
        }
    }
    
    // ... similar to SyncFromParent
}
```

**Key Points**:
- Same mechanism as `SyncFromParent` but with `IContextProvider` marker
- Can traverse multiple levels up the tree
- Multiple contexts of different types can coexist
- Nearest context wins (if multiple of same type)

### 4. PropertyBinding Component (Complex Scenarios)

First-class component for scenarios requiring validation, conversion, or conditional logic.

#### Component

```csharp
public partial class PropertyBinding : RuntimeComponent
{
    [TemplateProperty]
    protected string _sourceProperty = string.Empty;
    
    [TemplateProperty]
    protected string _targetProperty = string.Empty;
    
    [TemplateProperty]
    protected BindingMode _mode = BindingMode.OneWay;
    
    [TemplateProperty]
    protected IValueConverter? _converter = null;
    
    [TemplateProperty]
    protected IValueValidator? _validator = null;
    
    protected override void OnActivate()
    {
        // Reflection-based binding (only when needed for complex scenarios)
        // Uses parent as source, next sibling as target
    }
}

public enum BindingMode
{
    OneWay,      // Source → Target
    TwoWay,      // Source ↔ Target
    OneTime,     // Source → Target (once)
    OneWayToSource  // Source ← Target
}
```

#### Usage

```csharp
new HealthSystem()
{
    Subcomponents =
    [
        new PropertyBinding()
        {
            SourceProperty = nameof(HealthSystem.Health),
            TargetProperty = "Health",
            Mode = BindingMode.OneWay,
            Converter = new PercentageConverter()
        },
        new HealthBar()  // Target of binding
    ]
}
```

**Key Points**:
- Only used when simple attributes aren't sufficient
- Visible in component tree (explicit)
- Supports runtime configuration
- Can use reflection (acceptable overhead for complex scenarios)
- First-class component can be added/removed dynamically

### 5. Helper Methods (Component Extensions)

```csharp
public static class ComponentExtensions
{
    /// <summary>
    /// Find ancestor component of specified type
    /// </summary>
    public static T? FindAncestor<T>(this IComponent component) 
        where T : IComponent
    {
        var parent = component.Parent;
        while (parent != null)
        {
            if (parent is T typed) return typed;
            parent = parent.Parent;
        }
        return default;
    }
    
    /// <summary>
    /// Find descendant component of specified type (breadth-first)
    /// </summary>
    public static T? FindDescendant<T>(this IComponent component)
        where T : IComponent
    {
        // BFS through Children
    }
}
```

## Implementation Phases

### Phase 1: Foundation (Week 1)
- [ ] Extend ComponentPropertyGenerator to support `NotifyChange` parameter
- [ ] Generate `PropertyChanged` event and `OnPropertyChanged` method
- [ ] Add `PropertyChangedEventArgs` class
- [ ] Add analyzer to ensure `NotifyChange = true` for sync sources
- [ ] Unit tests for notification system

### Phase 2: SyncFromParent (Week 2)
- [ ] Create `SyncFromParentAttribute<T>`
- [ ] Extend generator to detect and generate sync code
- [ ] Implement `FindAncestor<T>` extension method
- [ ] Auto-subscribe/unsubscribe in OnActivate/OnDeactivate
- [ ] Analyzer for missing parent types
- [ ] Integration tests with component hierarchies

### Phase 3: FromContext (Week 3)
- [ ] Create `IContextProvider` marker interface
- [ ] Create `FromContextAttribute<T>`
- [ ] Extend generator to handle context lookup
- [ ] Tests for multi-level context resolution
- [ ] Documentation and examples

### Phase 4: PropertyBinding Component (Week 4)
- [ ] Implement `PropertyBinding` component
- [ ] Support `IValueConverter` and `IValueValidator`
- [ ] Handle all `BindingMode` variations
- [ ] Reflection-based property access
- [ ] Error handling and validation
- [ ] Examples for complex scenarios

### Phase 5: Documentation & Optimization (Week 5)
- [ ] Comprehensive documentation
- [ ] Performance benchmarks
- [ ] Examples for common patterns
- [ ] Migration guide from manual approaches
- [ ] Best practices guide

## Examples

### Example 1: Simple Parent-Child Sync

```csharp
// Health system manages health value
public partial class HealthSystem : RuntimeComponent
{
    [ComponentProperty(NotifyChange = true)]
    public float Health { get; set; } = 100f;
    
    [ComponentProperty(NotifyChange = true)]
    public float MaxHealth { get; set; } = 100f;
}

// Health bar displays health value
public partial class HealthBar : RuntimeComponent
{
    [ComponentProperty]
    [SyncFromParent<HealthSystem>(nameof(HealthSystem.Health))]
    public float Health { get; set; }
    
    [ComponentProperty]
    [SyncFromParent<HealthSystem>(nameof(HealthSystem.MaxHealth))]
    public float MaxHealth { get; set; }
    
    // Bar width calculated in OnUpdate
    protected override void OnUpdate(double deltaTime)
    {
        var percentage = Health / MaxHealth;
        Width = (uint)(MaxWidth * percentage);
    }
}

// Component tree
new HealthSystem()
{
    Health = 75f,
    MaxHealth = 100f,
    Subcomponents = 
    [
        new HealthBar()  // Automatically syncs Health and MaxHealth
    ]
}
```

### Example 2: Theme Context

```csharp
// Global theme provider
public partial class ThemeContext : RuntimeComponent, IContextProvider
{
    [ComponentProperty(NotifyChange = true)]
    public Color PrimaryColor { get; set; } = Color.Blue;
    
    [ComponentProperty(NotifyChange = true)]
    public Color BackgroundColor { get; set; } = Color.White;
    
    [ComponentProperty(NotifyChange = true)]
    public float FontSize { get; set; } = 14f;
}

// Buttons consume theme
public partial class Button : RuntimeComponent
{
    [ComponentProperty]
    [FromContext<ThemeContext>(nameof(ThemeContext.PrimaryColor))]
    public Color BackgroundColor { get; set; }
    
    [ComponentProperty]
    [FromContext<ThemeContext>(nameof(ThemeContext.FontSize))]
    public float FontSize { get; set; }
}

// App structure
new ThemeContext()
{
    PrimaryColor = Color.DarkBlue,
    Subcomponents =
    [
        new MainMenu()
        {
            Subcomponents =
            [
                new Button(),  // Gets theme from context
                new Button()
            ]
        },
        new SettingsPanel()
        {
            Subcomponents =
            [
                new Button()  // Also gets theme from same context
            ]
        }
    ]
}
```

### Example 3: Complex Binding with Validation

```csharp
new InputField()
{
    Subcomponents =
    [
        new PropertyBinding()
        {
            SourceProperty = nameof(InputField.Text),
            TargetProperty = "Value",
            Mode = BindingMode.TwoWay,
            Validator = new RangeValidator(0, 100),
            Converter = new StringToIntConverter()
        },
        new NumericDisplay()
    ]
}
```

### Example 4: Performance Monitor (Real Use Case)

```csharp
public partial class PerformanceMonitor : RuntimeComponent
{
    [ComponentProperty(NotifyChange = true)]
    protected double _currentFps;
    
    [ComponentProperty(NotifyChange = true)]
    protected string _performanceSummary = string.Empty;
    
    protected override void OnUpdate(double deltaTime)
    {
        _currentFps = 1.0 / deltaTime;
        _performanceSummary = $"FPS: {_currentFps:F1}";
    }
}

public partial class PerformanceDisplay : RuntimeComponent
{
    [ComponentProperty]
    [SyncFromParent<PerformanceMonitor>(nameof(PerformanceMonitor.CurrentFps))]
    public double CurrentFps { get; set; }
    
    [ComponentProperty]
    [SyncFromParent<PerformanceMonitor>(nameof(PerformanceMonitor.PerformanceSummary))]
    public string PerformanceSummary { get; set; }
}

// Tree
new PerformanceMonitor()
{
    Subcomponents = 
    [
        new PerformanceDisplay()  // Auto-syncs from monitor
    ]
}
```

## Performance Considerations

### Zero-Overhead (Generated Code)
- `SyncFromParent`: Single event subscription per property
- Direct property access (no reflection)
- Cleanup handled automatically in OnDeactivate
- No dictionary lookups or string comparisons

### Acceptable Overhead (PropertyBinding)
- Reflection-based property access
- Only used when validation/conversion needed
- Opt-in for complex scenarios

### Memory
- Event subscriptions: 1 delegate per sync relationship
- Parent references: 1 field per sync attribute
- Auto-cleanup prevents memory leaks

### Benchmarks (To Be Measured)
- Direct property access (baseline)
- SyncFromParent overhead (expected: ~5-10% vs baseline)
- FromContext overhead (expected: ~10-15% vs baseline due to tree traversal)
- PropertyBinding overhead (expected: ~50-100% vs baseline, acceptable for complex scenarios)

## Migration Path

Existing manual approaches can coexist with new system:

```csharp
// Old approach (still works)
public class HealthBar : RuntimeComponent
{
    protected override void OnActivate()
    {
        var healthSystem = this.FindAncestor<HealthSystem>();
        // Manual event subscription
    }
}

// New approach (simpler)
public partial class HealthBar : RuntimeComponent
{
    [SyncFromParent<HealthSystem>(nameof(HealthSystem.Health))]
    public float Health { get; set; }
}
```

## Open Questions

1. **Should SyncFromParent support two-way sync?**
   - Leaning No - prefer unidirectional flow, use PropertyBinding for two-way

2. **Should there be a [SyncToChild] attribute?**
   - Leaning No - parent shouldn't depend on child structure

3. **How to handle sync when parent changes dynamically?**
   - Re-subscribe during tree restructure events

4. **Should context providers be cached globally?**
   - No - keep lookup simple, optimize if profiling shows issues

5. **Support for sibling property sync?**
   - Phase 2 feature - use shared parent as intermediary

## References

- [Deferred Property Generation System](../../.docs/Deferred%20Property%20Generation%20System.md)
- [Component Lifecycle](../../src/GameEngine/Components/README.md)
- Research on other platforms: `research.md`
