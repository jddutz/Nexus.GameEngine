# Feature Specification: Property Binding System

**Feature Branch**: `013-property-binding`  
**Created**: December 6, 2025  
**Status**: Draft  
**Input**: User description: "Now we need to explore a new feature: how do we wire up properties between components?"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic Parent-to-Child Property Binding (Priority: P1)

A developer wants to create a health bar UI component that automatically updates when the player's health changes, without writing manual event handling code.

**Why this priority**: This is the most common use case for property bindings and delivers immediate value. It eliminates boilerplate event subscription code and enables declarative component composition.

**Independent Test**: Can be fully tested by creating a parent component with a `[ComponentProperty]` health value and a child HealthBar with a bound `CurrentHealth` property. Changing the parent's health should automatically update the child's display, verifiable through integration tests.

**Acceptance Scenarios**:

1. **Given** a parent component `PlayerCharacter` with a `Health` property, **When** a child `HealthBar` component defines a binding from parent's `Health` to its own `CurrentHealth` property, **Then** the HealthBar's CurrentHealth updates immediately when PlayerCharacter's Health changes
2. **Given** a HealthBar with a binding to PlayerCharacter's Health, **When** the PlayerCharacter component is activated, **Then** the HealthBar's CurrentHealth initializes with the current value from PlayerCharacter
3. **Given** an active HealthBar with bindings, **When** the component is deactivated, **Then** all event subscriptions are cleaned up and no memory leaks occur

---

### User Story 2 - Property Bindings with Type Conversion (Priority: P2)

A developer wants to display a numeric health value as formatted text (e.g., "Health: 75.5") without writing custom conversion logic.

**Why this priority**: Type conversion is essential for practical UI scenarios where data types don't match (float → string, bool → color, etc.). This story builds on P1 by adding transformation capabilities.

**Independent Test**: Can be tested by binding a float health value to a string text property with a format converter, verifying the formatted output appears correctly.

**Acceptance Scenarios**:

1. **Given** a PlayerCharacter with float `Health` property, **When** a TextDisplay binds to it with `.AsFormattedString("{0:F1}")`, **Then** the TextDisplay shows "75.5" when health is 75.5
2. **Given** a binding with a custom `IValueConverter`, **When** the source value changes, **Then** the converter transforms the value before setting the target property
3. **Given** a binding with a converter that returns null, **When** the source changes, **Then** the binding silently ignores the null result and doesn't update the target

---

### User Story 3 - Named Component Lookup (Priority: P2)

A developer wants to bind to a specific named component anywhere in the tree (not just parent/child relationships).

**Why this priority**: Enables flexible component relationships beyond parent/child hierarchy. Common for cross-cutting concerns like binding UI to a named "GameState" context.

**Independent Test**: Can be tested by creating two sibling components where one binds to the other by name, verifying the binding works despite no parent/child relationship.

**Acceptance Scenarios**:

1. **Given** a component named "PlayerCharacter" in the tree, **When** a HealthBar uses `.FromNamedObject("PlayerCharacter")`, **Then** the binding successfully resolves the source component
2. **Given** a binding to a non-existent named component, **When** the binding activates, **Then** it silently fails without throwing exceptions and the target property remains unchanged
3. **Given** a binding to a named component that's loaded after the binding activates, **When** the named component is later added, **Then** the binding remains inactive (doesn't auto-rebind)

---

### User Story 4 - Sibling and Context-Based Lookups (Priority: P3)

A developer wants to bind properties using flexible lookup strategies like finding siblings of a specific type or searching up the tree for context providers.

**Why this priority**: Provides architectural flexibility for advanced scenarios like theming (context providers) or coordinating between sibling components. Less common than parent/child but valuable for complex UIs.

**Independent Test**: Can be tested by creating sibling components and binding between them using `.FromSibling<T>()`, or creating a theme context provider and binding descendant colors using `.FromContext<T>()`.

**Acceptance Scenarios**:

1. **Given** sibling components A and B, **When** B binds to A using `.FromSibling<ComponentA>()`, **Then** the binding resolves A correctly
2. **Given** a `ThemeContext` component with a `PrimaryColor` property, **When** a descendant Button binds using `.FromContext<ThemeContext>()`, **Then** the binding searches up the tree and finds the context provider
3. **Given** multiple siblings of the same type, **When** using `.FromSibling<T>()`, **Then** the binding returns the first matching sibling

---

### User Story 5 - Two-Way Property Bindings (Priority: P3)

A developer wants a slider control where moving the slider updates a source value AND changing the source value updates the slider position (bidirectional sync).

**Why this priority**: Required for interactive controls but less common than one-way display bindings. Deferred to P3 because it adds complexity (cycle prevention, bidirectional converters).

**Independent Test**: Can be tested by creating a slider bound two-way to a volume setting, verifying that dragging the slider updates the setting AND external changes to the setting update the slider.

**Acceptance Scenarios**:

1. **Given** a Slider with `.TwoWay()` binding mode, **When** the slider value changes, **Then** the source property updates
2. **Given** a two-way binding with a bidirectional converter, **When** the source changes, **Then** the converter's `Convert()` method is called; when the target changes, the `ConvertBack()` method is called
3. **Given** a two-way binding, **When** the source triggers an update, **Then** the system prevents infinite loops by tracking update state

---

### User Story 6 - Source Generator Integration (Priority: P1)

The source generator system automatically creates PropertyBindings configuration classes for each template, enabling type-safe binding declarations.

**Why this priority**: Core infrastructure requirement for the entire binding system. Without this, developers must use string-based APIs which are error-prone.

**Independent Test**: Can be tested by verifying that compiling a component with `[ComponentProperty]` attributes generates a corresponding `{ComponentName}PropertyBindings` class with matching property names.

**Acceptance Scenarios**:

1. **Given** a component `HealthBar` with `[ComponentProperty] float _currentHealth`, **When** the code is compiled, **Then** a `HealthBarPropertyBindings` class is generated with a `CurrentHealth` property
2. **Given** a `HealthBarTemplate`, **When** the template is compiled, **Then** it includes a `Bindings` property of type `HealthBarPropertyBindings`
3. **Given** a `HealthBarPropertyBindings` instance with configured bindings, **When** enumerated, **Then** it yields tuples of (propertyName, binding) for all non-null bindings

---

### Edge Cases

- What happens when a binding source component is deactivated before the target? The binding's event subscription is cleaned up when the source deactivates, preventing orphaned handlers.
- How does the system handle type mismatches without converters? The binding silently fails if types are incompatible and no converter is provided.
- What if multiple properties have the same name (shadowing)? Bindings use the declared property on the specific component type, following normal C# member resolution.
- How are circular bindings prevented in two-way mode? The `PropertyBinding` class tracks an `_isUpdating` flag to prevent re-entry during update propagation.
- What happens if a converter throws an exception? The exception is logged and the target property is not updated (current value preserved).
- How do bindings behave with interpolated properties? Bindings use `SetCurrent{PropertyName}()` which bypasses interpolation, providing immediate updates to avoid double-deferral.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a fluent API for defining property bindings in component templates
- **FR-002**: System MUST generate `{ComponentName}PropertyBindings` classes for all components with `[ComponentProperty]` attributes
- **FR-003**: System MUST generate `{PropertyName}Changed` events for all `[ComponentProperty]` fields to enable observable properties
- **FR-004**: System MUST support multiple lookup strategies: FromNamedObject, FromParent, FromSibling, FromChild, FromContext
- **FR-005**: System MUST activate bindings during `Component.OnActivate()` lifecycle phase
- **FR-006**: System MUST deactivate bindings during `Component.OnDeactivate()` lifecycle phase
- **FR-007**: System MUST perform initial synchronization when a binding is activated (set target to current source value)
- **FR-008**: System MUST update target properties immediately when source properties change (bypass interpolation)
- **FR-009**: System MUST support optional value converters implementing `IValueConverter` interface
- **FR-010**: System MUST support one-way binding mode (source → target) as default
- **FR-011**: System MUST support two-way binding mode (source ↔ target) when explicitly configured
- **FR-012**: System MUST prevent infinite loops in two-way bindings using re-entry detection
- **FR-013**: System MUST gracefully handle missing source components (silent failure, no exceptions)
- **FR-014**: System MUST clean up event subscriptions when bindings are deactivated
- **FR-015**: System MUST make `PropertyBindings` classes enumerable to support `ToList()` and iteration
- **FR-016**: System MUST call `SetCurrent{PropertyName}()` generated method for binding updates
- **FR-017**: Bidirectional converters MUST implement both `Convert()` and `ConvertBack()` methods for two-way bindings

### Key Entities *(include if feature involves data)*

- **PropertyBinding**: Encapsulates binding configuration (source lookup strategy, source property name, converter, binding mode). Stateful class that manages event subscription lifecycle.
- **ILookupStrategy**: Interface defining how to resolve source components (NamedObjectLookup, ParentLookup, SiblingLookup, ChildLookup, ContextLookup implementations)
- **IValueConverter**: Interface for one-way value transformation with `Convert(object? value)` method
- **IBidirectionalConverter**: Interface extending IValueConverter with `ConvertBack(object? value)` for two-way bindings
- **PropertyBindings**: Base class for generated `{ComponentName}PropertyBindings` classes, implements `IEnumerable<(string propertyName, PropertyBinding binding)>`
- **PropertyChangedEventArgs<T>**: Generic event args containing `OldValue` and `NewValue` for property change notifications

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can define a basic parent-to-child binding in under 5 lines of code using the fluent API
- **SC-002**: Property bindings reduce event subscription boilerplate by at least 80% compared to manual implementation (measured by lines of code)
- **SC-003**: Source generators produce compilation errors if binding configuration references non-existent properties (compile-time safety)
- **SC-004**: Integration tests verify zero memory leaks after 1000 activate/deactivate cycles of components with bindings
- **SC-005**: Bindings introduce less than 5% performance overhead compared to direct property assignment (measured via benchmarks)
- **SC-006**: 90% of common UI binding scenarios (health bars, progress indicators, text displays) require no custom converter code
- **SC-007**: Generated code compiles without warnings and passes static analysis
- **SC-008**: Property bindings work correctly with the existing deferred property update system (no conflicts with interpolation)

## Technical Design

### Template API

```csharp
new HealthBarTemplate()
{
    Bindings = 
    {
        // Simple parent binding
        CurrentHealth = PropertyBinding
            .FromParent<PlayerCharacter>()
            .GetPropertyValue(nameof(PlayerCharacter.Health)),
            
        // Named component binding with converter
        HealthText = PropertyBinding
            .FromNamedObject("PlayerCharacter")
            .GetPropertyValue(nameof(PlayerCharacter.Health))
            .AsFormattedString("{0:F1}"),
            
        // Two-way binding with bidirectional converter
        Volume = PropertyBinding
            .FromParent<AudioSettings>()
            .GetPropertyValue(nameof(AudioSettings.MasterVolume))
            .WithConverter(new PercentageConverter())
            .TwoWay()
    }
}
```

### PropertyBinding Class

```csharp
public class PropertyBinding
{
    // Configuration
    internal ILookupStrategy? LookupStrategy { get; set; }
    internal string? SourcePropertyName { get; set; }
    internal IValueConverter? Converter { get; set; }
    internal BindingMode Mode { get; set; } = BindingMode.OneWay;
    
    // Runtime state (managed internally)
    private IComponent? _sourceComponent;
    private EventInfo? _sourceEvent;
    private Delegate? _sourceHandler;
    private EventInfo? _targetEvent;
    private Delegate? _targetHandler;
    private bool _isUpdating; // Cycle prevention for TwoWay
    
    // Fluent API
    public static PropertyBinding FromParent<T>() where T : IComponent;
    public static PropertyBinding FromSibling<T>() where T : IComponent;
    public static PropertyBinding FromChild<T>() where T : IComponent;
    public static PropertyBinding FromContext<T>() where T : IComponent;
    public static PropertyBinding FromNamedObject(string name);
    
    public PropertyBinding GetPropertyValue(string propertyName);
    public PropertyBinding WithConverter(IValueConverter converter);
    public PropertyBinding AsFormattedString(string format);
    public PropertyBinding TwoWay();
    
    // Lifecycle (called by Component)
    internal void Activate(IComponent targetComponent, string targetPropertyName);
    internal void Deactivate();
}

public enum BindingMode
{
    OneWay,      // Source → Target (default)
    TwoWay       // Source ↔ Target
}
```

### Generated Code Structure

**ComponentPropertyGenerator** modifications:
```csharp
// For each [ComponentProperty]:
partial class HealthBar
{
    // Existing
    public float CurrentHealth => _currentHealth;
    public void SetCurrentHealth(float value, InterpolationFunction<float>? interpolator = null) { ... }
    
    // NEW: Immediate setter (uses existing SetCurrent pattern)
    // (SetCurrentHealth already exists and bypasses interpolation when called without interpolator)
    
    // NEW: Change event
    public event EventHandler<PropertyChangedEventArgs<float>>? CurrentHealthChanged;
    
    partial void OnCurrentHealthChanged(float oldValue)
    {
        CurrentHealthChanged?.Invoke(this, new(oldValue, CurrentHealth));
    }
}
```

**TemplateGenerator** modifications:
```csharp
// Add to each template:
partial record HealthBarTemplate
{
    public HealthBarPropertyBindings Bindings { get; init; } = new();
}
```

**PropertyBindingsGenerator** (new):
```csharp
// Generated for each component with [ComponentProperty] attributes:
public class HealthBarPropertyBindings : IEnumerable<(string propertyName, PropertyBinding binding)>
{
    public PropertyBinding? CurrentHealth { get; set; }
    public PropertyBinding? MaxHealth { get; set; }
    
    public IEnumerator<(string propertyName, PropertyBinding binding)> GetEnumerator()
    {
        if (CurrentHealth != null)
            yield return (nameof(CurrentHealth), CurrentHealth);
        if (MaxHealth != null)
            yield return (nameof(MaxHealth), MaxHealth);
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

### Component Lifecycle Integration

```csharp
public partial class Component
{
    private List<(string propertyName, PropertyBinding binding)> _bindings = [];
    
    protected override void OnLoad(Template? template)
    {
        base.OnLoad(template);
        
        // Store bindings from template (don't activate yet)
        if (template?.Bindings != null)
        {
            _bindings = template.Bindings.ToList();
        }
    }
    
    protected override void OnActivate()
    {
        base.OnActivate();
        
        // Activate all bindings
        foreach (var (propertyName, binding) in _bindings)
        {
            binding.Activate(this, propertyName);
        }
    }
    
    protected override void OnDeactivate()
    {
        // Deactivate bindings BEFORE calling base
        foreach (var (_, binding) in _bindings)
        {
            binding.Deactivate();
        }
        
        base.OnDeactivate();
    }
}
```

### Converter Interfaces

```csharp
public interface IValueConverter
{
    object? Convert(object? value);
}

public interface IBidirectionalConverter : IValueConverter
{
    object? ConvertBack(object? value);
}

// Built-in converters
public class StringFormatConverter : IValueConverter
{
    public required string Format { get; init; }
    public object? Convert(object? value) => value != null ? string.Format(Format, value) : null;
}

public class MultiplyConverter : IValueConverter
{
    public required float Factor { get; init; }
    public object? Convert(object? value) => value is float f ? f * Factor : null;
}

public class PercentageConverter : IBidirectionalConverter
{
    public object? Convert(object? value) => value is float f ? f * 100 : null;
    public object? ConvertBack(object? value) => value is float f ? f / 100 : null;
}
```

## Implementation Notes

1. **SetCurrent vs SetImmediate**: Use existing `SetCurrent{PropertyName}()` method for binding updates, as it already bypasses interpolation when called directly
2. **Validation**: No explicit validation phase. Invalid bindings (missing source, type mismatch) fail silently at runtime and are logged
3. **Cleanup**: Source components automatically clean up their event subscribers when deactivated. Target components call `binding.Deactivate()` which removes event handlers stored in PropertyBinding's internal state
4. **Lazy Lookup**: Source component resolution happens during `Activate()`, not `Load()`, allowing flexible component creation order
5. **Type Safety**: Generic lookup methods (`FromParent<T>()`) provide compile-time type checking for source component types
6. **Performance**: Bindings use direct event subscription (no polling), minimal reflection (cached during Activate), and struct-based property updaters
7. **PropertyBinding State**: The _bindings field in Component stores the configured bindings from the template. Each PropertyBinding instance manages its own runtime state (source component reference, event handlers) internally

## Future Enhancements (Out of Scope)

- Collection bindings (binding to array elements, list synchronization)
- Binding expressions (e.g., `binding.Expression(() => source.Health * 0.5f)`)
- Throttling/debouncing for high-frequency updates
- Binding validation during `Component.Validate()` phase
- Multi-source bindings (combining multiple sources into one target)
- Binding priorities/ordering when multiple bindings target the same property
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
