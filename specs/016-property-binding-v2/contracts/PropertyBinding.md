# API Contract: PropertyBinding<TSource, TValue>

**Namespace**: `Nexus.GameEngine.Components`  
**Purpose**: Generic property binding class with fluent configuration API

## Class Definition

```csharp
namespace Nexus.GameEngine.Components;

/// <summary>
/// Property binding transformation pipeline.
/// TSource: The source component type (constant through pipeline)
/// TValue: The current value type flowing through the pipeline (transforms at each step)
/// </summary>
/// <typeparam name="TSource">Source component type (must implement IComponent)</typeparam>
/// <typeparam name="TValue">Current value type in the pipeline</typeparam>
public class PropertyBinding<TSource, TValue> : IPropertyBinding 
    where TSource : class, IComponent
{
    // Public API - Fluent configuration methods
    
    /// <summary>
    /// Extracts a property value from the source component.
    /// Type inference: compiler infers TProp from the lambda expression.
    /// </summary>
    /// <typeparam name="TProp">Property type (inferred from lambda)</typeparam>
    /// <param name="selector">Lambda expression selecting the property (e.g., p => p.Health)</param>
    /// <returns>New PropertyBinding with TValue = TProp</returns>
    /// <exception cref="ArgumentException">Expression is not a property access</exception>
    public PropertyBinding<TSource, TProp> GetPropertyValue<TProp>(
        Expression<Func<TSource, TProp>> selector);
    
    /// <summary>
    /// Converts the current value to a formatted string.
    /// </summary>
    /// <param name="format">Format string (e.g., "Health: {0:F0}")</param>
    /// <returns>New PropertyBinding with TValue = string</returns>
    public PropertyBinding<TSource, string> AsFormattedString(string format);
    
    /// <summary>
    /// Applies a custom converter to transform the current value.
    /// </summary>
    /// <param name="converter">Converter to apply</param>
    /// <returns>Current PropertyBinding instance (mutable chaining)</returns>
    public PropertyBinding<TSource, TValue> WithConverter(IValueConverter converter);
    
    /// <summary>
    /// Configures the binding for two-way synchronization (source ↔ target).
    /// </summary>
    /// <returns>Current PropertyBinding instance (mutable chaining)</returns>
    /// <remarks>Out of scope for v2 revision, included for future compatibility</remarks>
    public PropertyBinding<TSource, TValue> TwoWay();
    
    /// <summary>
    /// Specifies the target property setter method.
    /// This is a REQUIRED terminal method for binding configuration.
    /// </summary>
    /// <param name="setter">Action that sets the target property (e.g., SetText)</param>
    /// <returns>Current PropertyBinding instance</returns>
    public PropertyBinding<TSource, TValue> Set(Action<TValue> setter);
    
    // IPropertyBinding implementation
    
    /// <summary>
    /// Activates the binding by resolving source component and subscribing to events.
    /// </summary>
    public void Activate();
    
    /// <summary>
    /// Deactivates the binding by unsubscribing from events and clearing references.
    /// </summary>
    public void Deactivate();
}
```

## Fluent API Usage Patterns

### Basic Parent-to-Child Binding

```csharp
// Template configuration
new TextRendererTemplate
{
    Bindings = new[]
    {
        Binding.FromParent<PlayerComponent>()
            .GetPropertyValue(p => p.Health)
            .AsFormattedString("Health: {0:F0}")
            .Set(SetText)
    }
}
```

### Sibling Binding with Custom Converter

```csharp
Binding.FromSibling<ScoreComponent>()
    .GetPropertyValue(s => s.Points)
    .WithConverter(new PointsToRankConverter())
    .Set(SetRank)
```

### Named Component Binding

```csharp
Binding.FromNamedObject<GameStateComponent>("GameState")
    .GetPropertyValue(g => g.Level)
    .AsFormattedString("Level {0}")
    .Set(SetText)
```

### Minimal Binding (Type Matching)

```csharp
// When source and target types match, no converter needed
Binding.FromParent<ColorProvider>()
    .GetPropertyValue(c => c.PrimaryColor)
    .Set(SetColor)  // Assumes target has SetColor(Color color) method
```

## Type Transformation Pipeline

The generic type parameter `TValue` changes as the binding flows through transformations:

```csharp
// Initial: PropertyBinding<PlayerComponent, PlayerComponent>
Binding.FromParent<PlayerComponent>()

// After GetPropertyValue: PropertyBinding<PlayerComponent, float>
    .GetPropertyValue(p => p.Health)

// After AsFormattedString: PropertyBinding<PlayerComponent, string>
    .AsFormattedString("Health: {0:F0}")

// Terminal: Set() returns PropertyBinding<PlayerComponent, string>
    .Set(SetText)
```

## Configuration Constraints

### Required Methods

1. **Source resolution**: Must call one of:
   - `Binding.FromParent<TSource>()` (default if omitted)
   - `Binding.FromSibling<TSource>()`
   - `Binding.FromChild<TSource>()`
   - `Binding.FromNamedObject<TSource>(name)`
   - `Binding.FromContext<TSource>()`

2. **Property selection**: Must call:
   - `GetPropertyValue(selector)` to specify which property to bind

3. **Target setter**: MUST call:
   - `Set(setter)` to specify the target property setter method
   - Binding is invalid without `.Set()` and will not be added to PropertyBindings collection

### Optional Methods

- `AsFormattedString(format)` - for string conversion with formatting
- `WithConverter(converter)` - for custom type conversion
- `TwoWay()` - for bidirectional binding (future feature)

## Method Chaining Behavior

- **Immutable transformations**: `GetPropertyValue()` and `AsFormattedString()` create new generic instances with different `TValue`
- **Mutable configuration**: `WithConverter()`, `TwoWay()`, `Set()` modify and return the same instance
- **Terminal method**: `Set()` is the final configuration step, returns the binding instance for collection assignment

## Validation Rules

### Compile-Time

- ✅ `TSource` must implement `IComponent`
- ✅ Property selector expression must be a property access (enforced by method signature)
- ✅ Setter delegate type must match `TValue` (enforced by generic constraint)

### Runtime (Configuration Phase)

- ✅ Property selector must be a MemberExpression (throws ArgumentException if not)
- ✅ Binding without `.Set()` is considered invalid (not added to PropertyBindings)

### Runtime (Activation Phase)

- ⚠️ Source component resolution may fail (logs warning, skips activation)
- ⚠️ Property event may not exist (logs warning, binding won't update)
- ⚠️ Property may not exist on source (logs warning, skips activation)

## Performance Characteristics

- **Configuration**: Zero overhead at template definition time (no allocations until Load phase)
- **Activation**: O(tree depth) for parent/context lookup, O(sibling count) for sibling lookup, O(tree size) for named lookup
- **Property update**: <0.1ms for simple converters, <1ms for complex formatters
- **Memory per active binding**: ~1KB (event handler delegates + cached references)

## Thread Safety

- ❌ **NOT thread-safe**: All operations must occur on main thread
- Configuration methods (GetPropertyValue, Set, etc.) called during template load (main thread)
- Activation/deactivation called during component lifecycle (main thread)
- Event handlers execute on event source thread (typically main thread)

## Error Handling Strategy

### Configuration Errors (Fail Fast)

- Invalid property selector → `ArgumentException`
- Missing `.Set()` → Binding not added to collection (silent)

### Runtime Errors (Log and Skip)

- Source component not found → Warning logged, activation skipped
- Property not found → Warning logged, activation skipped
- Event not found → Warning logged, binding won't update
- Conversion error → Error logged, update skipped

## Extension Points

### Custom Converters

```csharp
public class HealthPercentageConverter : IValueConverter
{
    public object? Convert(object? value)
    {
        if (value is not float health) return null;
        return health / 100.0f;  // Convert to percentage
    }
}

// Usage:
Binding.FromParent<PlayerComponent>()
    .GetPropertyValue(p => p.Health)
    .WithConverter(new HealthPercentageConverter())
    .Set(SetPercentage)
```

### Custom Lookup Strategies

```csharp
public class NearestByNameLookup : ILookupStrategy
{
    private readonly string _name;
    
    public NearestByNameLookup(string name) => _name = name;
    
    public IComponent? Resolve(IComponent target)
    {
        // Custom tree traversal logic
        // ...
    }
}
```

## Known Limitations (v2)

1. **No two-way binding**: `TwoWay()` method exists but functionality not implemented
2. **No multi-source bindings**: Cannot combine values from multiple sources
3. **No binding expressions**: Cannot bind to computed values like `Health / MaxHealth`
4. **No automatic type coercion**: Must explicitly convert incompatible types
5. **No binding validation**: Invalid configurations only detected at runtime activation
