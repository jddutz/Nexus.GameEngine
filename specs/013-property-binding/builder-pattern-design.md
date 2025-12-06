# Property Binding: Builder Pattern Design

## Problem with Static Initialization

You've identified the key constraint: **Template initialization must be static/compile-time compatible**.

```csharp
// ❌ Won't work - can't call instance methods in object initializer
new HealthBarTemplate()
{
    PropertyBindings = PropertyBindings
        .Add(nameof(Health))
        .FromParent<HealthSystem>()  // Can't call methods here!
}
```

**Why?**: Object initializers only allow property/field assignments, not method calls.

## Solution: Builder Creates Immutable Binding Records

The builder creates immutable `PropertyBinding` records that are assigned to the array:

```csharp
// ✅ Works - builder methods return binding records
new HealthBarTemplate()
{
    PropertyBindings = 
    [
        PropertyBinding.For(nameof(Health))
            .FromParent<HealthSystem>()
            .Property(nameof(HealthSystem.Health))
            .Build(),
            
        PropertyBinding.For(nameof(MaxHealth))
            .FromParent<HealthSystem>()
            .Property(nameof(HealthSystem.MaxHealth))
            .Build()
    ]
}
```

## Design: Fluent Builder with Immutable Output

### Step 1: PropertyBinding Record (Output)

```csharp
namespace Nexus.GameEngine.Components;

/// <summary>
/// Immutable record representing a property binding configuration.
/// Created via PropertyBinding.For() builder API.
/// </summary>
public record PropertyBinding
{
    /// <summary>Target property name on this component</summary>
    public required string TargetProperty { get; init; }
    
    /// <summary>Source property name on source component</summary>
    public required string SourceProperty { get; init; }
    
    /// <summary>Lookup strategy for finding source component</summary>
    public required BindingLookup Lookup { get; init; }
    
    /// <summary>Binding mode (OneWay, TwoWay, etc.)</summary>
    public BindingMode Mode { get; init; } = BindingMode.OneWay;
    
    /// <summary>Optional value converter</summary>
    public IValueConverter? Converter { get; init; }
    
    /// <summary>Optional value validator</summary>
    public IValueValidator? Validator { get; init; }
    
    /// <summary>Null handling strategy</summary>
    public NullBehavior NullHandling { get; init; } = NullBehavior.Propagate;
    
    /// <summary>Fallback value when source is null</summary>
    public object? FallbackValue { get; init; }
    
    /// <summary>Update timing strategy</summary>
    public UpdateTiming Timing { get; init; } = UpdateTiming.Immediate;
    
    /// <summary>Throttle interval in milliseconds</summary>
    public double ThrottleMs { get; init; } = 0;
    
    // Static factory method - entry point for builder
    public static PropertyBindingBuilder For(string targetProperty) 
        => new PropertyBindingBuilder(targetProperty);
}

/// <summary>
/// Discriminated union for different lookup strategies
/// </summary>
public abstract record BindingLookup
{
    /// <summary>Find parent by type</summary>
    public sealed record ParentType(Type Type) : BindingLookup;
    
    /// <summary>Find context provider by type</summary>
    public sealed record ContextType(Type Type) : BindingLookup;
    
    /// <summary>Find sibling by name</summary>
    public sealed record SiblingName(string Name) : BindingLookup;
    
    /// <summary>Parse path syntax (fallback)</summary>
    public sealed record Path(string PathString) : BindingLookup;
}
```

### Step 2: Fluent Builder (Intermediate State)

```csharp
namespace Nexus.GameEngine.Components;

/// <summary>
/// Fluent builder for creating PropertyBinding records.
/// Provides type-safe, IntelliSense-friendly API for binding configuration.
/// </summary>
public class PropertyBindingBuilder
{
    private readonly string _targetProperty;
    private string? _sourceProperty;
    private BindingLookup? _lookup;
    private BindingMode _mode = BindingMode.OneWay;
    private IValueConverter? _converter;
    private IValueValidator? _validator;
    private NullBehavior _nullHandling = NullBehavior.Propagate;
    private object? _fallbackValue;
    private UpdateTiming _timing = UpdateTiming.Immediate;
    private double _throttleMs = 0;
    
    internal PropertyBindingBuilder(string targetProperty)
    {
        _targetProperty = targetProperty;
    }
    
    // === LOOKUP METHODS (Step 1: Where to find source?) ===
    
    /// <summary>
    /// Bind to a property on the nearest parent of specified type.
    /// Type-safe alternative to path-based lookup.
    /// </summary>
    public PropertyBindingBuilder FromParent<T>() where T : IComponent
    {
        _lookup = new BindingLookup.ParentType(typeof(T));
        return this;
    }
    
    /// <summary>
    /// Bind to a property on a context provider of specified type.
    /// Searches up the component tree for IContextProvider.
    /// </summary>
    public PropertyBindingBuilder FromContext<T>() where T : IComponent, IContextProvider
    {
        _lookup = new BindingLookup.ContextType(typeof(T));
        return this;
    }
    
    /// <summary>
    /// Bind to a property on a sibling component with the specified name.
    /// </summary>
    public PropertyBindingBuilder FromSibling(string name)
    {
        _lookup = new BindingLookup.SiblingName(name);
        return this;
    }
    
    /// <summary>
    /// Bind using path syntax (fallback for complex scenarios).
    /// Examples: "../", "../../", "ChildName/"
    /// </summary>
    public PropertyBindingBuilder FromPath(string path)
    {
        _lookup = new BindingLookup.Path(path);
        return this;
    }
    
    // === PROPERTY METHODS (Step 2: Which property on source?) ===
    
    /// <summary>
    /// Specify the source property name.
    /// Use nameof() for type safety.
    /// </summary>
    public PropertyBindingBuilder Property(string propertyName)
    {
        _sourceProperty = propertyName;
        return this;
    }
    
    // === CONFIGURATION METHODS (Optional) ===
    
    /// <summary>Enable two-way binding</summary>
    public PropertyBindingBuilder TwoWay()
    {
        _mode = BindingMode.TwoWay;
        return this;
    }
    
    /// <summary>One-time binding (disconnect after first update)</summary>
    public PropertyBindingBuilder OneTime()
    {
        _mode = BindingMode.OneTime;
        return this;
    }
    
    /// <summary>Add value converter</summary>
    public PropertyBindingBuilder WithConverter(IValueConverter converter)
    {
        _converter = converter;
        return this;
    }
    
    /// <summary>Add value validator</summary>
    public PropertyBindingBuilder WithValidator(IValueValidator validator)
    {
        _validator = validator;
        return this;
    }
    
    /// <summary>Use fallback value when source is null</summary>
    public PropertyBindingBuilder WithFallback(object fallbackValue)
    {
        _nullHandling = NullBehavior.UseFallback;
        _fallbackValue = fallbackValue;
        return this;
    }
    
    /// <summary>Skip updates when source is null</summary>
    public PropertyBindingBuilder SkipNulls()
    {
        _nullHandling = NullBehavior.SkipUpdate;
        return this;
    }
    
    /// <summary>Defer updates to next frame</summary>
    public PropertyBindingBuilder Deferred()
    {
        _timing = UpdateTiming.Deferred;
        return this;
    }
    
    /// <summary>Throttle updates to maximum frequency</summary>
    public PropertyBindingBuilder Throttled(double milliseconds)
    {
        _timing = UpdateTiming.Throttled;
        _throttleMs = milliseconds;
        return this;
    }
    
    // === BUILD METHOD (Final step: Create immutable record) ===
    
    /// <summary>
    /// Build the immutable PropertyBinding record.
    /// Validates that all required fields are set.
    /// </summary>
    public PropertyBinding Build()
    {
        if (_lookup == null)
            throw new InvalidOperationException("Lookup strategy not specified. Call FromParent(), FromContext(), FromSibling(), or FromPath().");
        
        if (string.IsNullOrEmpty(_sourceProperty))
            throw new InvalidOperationException("Source property not specified. Call Property(name).");
        
        return new PropertyBinding
        {
            TargetProperty = _targetProperty,
            SourceProperty = _sourceProperty,
            Lookup = _lookup,
            Mode = _mode,
            Converter = _converter,
            Validator = _validator,
            NullHandling = _nullHandling,
            FallbackValue = _fallbackValue,
            Timing = _timing,
            ThrottleMs = _throttleMs
        };
    }
}
```

### Step 3: Supporting Types

```csharp
public enum BindingMode
{
    OneWay,         // Source → Target
    TwoWay,         // Source ↔ Target
    OneTime,        // Source → Target (once)
    OneWayToSource  // Source ← Target
}

public enum NullBehavior
{
    Propagate,   // Pass null to target
    UseFallback, // Use FallbackValue
    SkipUpdate   // Don't update target
}

public enum UpdateTiming
{
    Immediate,  // Update immediately
    Deferred,   // Update next frame
    Throttled   // Limit frequency
}

/// <summary>Marker interface for context providers</summary>
public interface IContextProvider { }

/// <summary>Converts values between types</summary>
public interface IValueConverter
{
    object? Convert(object? value, Type targetType);
    object? ConvertBack(object? value, Type sourceType);
}

/// <summary>Validates values before applying</summary>
public interface IValueValidator
{
    bool Validate(object? value);
    string? GetErrorMessage(object? value);
}
```

## Usage Examples

### Example 1: Simple Parent Binding

```csharp
new HealthBarTemplate()
{
    PropertyBindings = 
    [
        // ✅ Type-safe, fluent, readable
        PropertyBinding.For(nameof(Health))
            .FromParent<HealthSystem>()
            .Property(nameof(HealthSystem.Health))
            .Build()
    ]
}
```

### Example 2: Formatted Display with Conversion

```csharp
new TextElementTemplate()
{
    PropertyBindings = 
    [
        PropertyBinding.For(nameof(Text))
            .FromParent<HealthSystem>()
            .Property(nameof(HealthSystem.Health))
            .WithConverter(new FloatToStringConverter { Format = "0.0" })
            .WithFallback("N/A")
            .Build()
    ]
}
```

### Example 3: Context Binding with Throttling

```csharp
new ButtonTemplate()
{
    PropertyBindings = 
    [
        PropertyBinding.For(nameof(BackgroundColor))
            .FromContext<ThemeContext>()
            .Property(nameof(ThemeContext.PrimaryColor))
            .Throttled(100)  // Max 10 updates/sec
            .Build()
    ]
}
```

### Example 4: Two-Way Validated Binding

```csharp
new InputFieldTemplate()
{
    PropertyBindings = 
    [
        PropertyBinding.For(nameof(Text))
            .FromSibling("VolumeSlider")
            .Property(nameof(Value))
            .TwoWay()
            .WithConverter(new FloatToStringConverter())
            .WithValidator(new RangeValidator { Min = 0, Max = 100 })
            .Build()
    ]
}
```

### Example 5: Multiple Bindings on Same Component

```csharp
new HealthBarTemplate()
{
    PropertyBindings = 
    [
        PropertyBinding.For(nameof(Health))
            .FromParent<HealthSystem>()
            .Property(nameof(HealthSystem.Health))
            .Build(),
            
        PropertyBinding.For(nameof(MaxHealth))
            .FromParent<HealthSystem>()
            .Property(nameof(HealthSystem.MaxHealth))
            .Build(),
            
        PropertyBinding.For(nameof(BackgroundColor))
            .FromContext<ThemeContext>()
            .Property(nameof(ThemeContext.HealthBarColor))
            .Build()
    ]
}
```

### Example 6: Chained Property Navigation (Advanced)

```csharp
// For deep property access like: Parent.Weapon.Damage
new DamageDisplayTemplate()
{
    PropertyBindings = 
    [
        PropertyBinding.For(nameof(DamageValue))
            .FromParent<PlayerCharacter>()
            .Property("PrimaryWeapon.Damage")  // Dot notation for nested properties
            .Build()
    ]
}
```

## Advantages of This Approach

### ✅ Type-Safe Where Possible
```csharp
.FromParent<HealthSystem>()  // Generic constraint ensures IComponent
.Property(nameof(HealthSystem.Health))  // nameof() prevents typos
```

### ✅ IntelliSense-Friendly
Each builder method returns `PropertyBindingBuilder`, so IntelliSense shows next available options.

### ✅ Compile-Time Compatible
Builder methods return values (records), which are assignable in collection initializers.

### ✅ Self-Documenting
```csharp
// Very clear what this binding does:
PropertyBinding.For(nameof(Health))
    .FromParent<HealthSystem>()
    .Property(nameof(HealthSystem.Health))
    .TwoWay()
    .WithValidator(new RangeValidator { Min = 0, Max = 100 })
    .Build()
```

### ✅ Optional Configuration
Simple case is concise, complex cases add methods as needed:

```csharp
// Minimal
PropertyBinding.For(nameof(Health))
    .FromParent<HealthSystem>()
    .Property(nameof(HealthSystem.Health))
    .Build()

// Full-featured
PropertyBinding.For(nameof(Health))
    .FromParent<HealthSystem>()
    .Property(nameof(HealthSystem.Health))
    .TwoWay()
    .WithConverter(converter)
    .WithValidator(validator)
    .WithFallback(0.0f)
    .Throttled(100)
    .Build()
```

### ✅ Validation at Build Time
```csharp
// ❌ Throws exception - missing Property()
PropertyBinding.For(nameof(Health))
    .FromParent<HealthSystem>()
    .Build()  // InvalidOperationException
```

## Alternative: Collection-Based API

If you prefer list-style syntax:

```csharp
public class PropertyBindingCollection : List<PropertyBinding>
{
    public PropertyBindingBuilder Add(string targetProperty)
    {
        var builder = PropertyBinding.For(targetProperty);
        // Note: Can't auto-add to list until Build() is called
        return builder;
    }
}

// Usage (doesn't quite work - can't auto-add)
new HealthBarTemplate()
{
    PropertyBindings = new PropertyBindingCollection
    {
        { nameof(Health), b => b.FromParent<HealthSystem>().Property("Health") }
    }
}
```

**Problem**: Collection initializers don't work well with builder pattern because we need `Build()` to finalize.

**Conclusion**: Stick with array initializer + explicit `Build()` calls.

## Comparison to Your Proposals

### Your Idea 1
```csharp
PropertyBindings.Add(nameof(myProperty))
    .FromParent()
    .PropertyValue(nameof(PrimaryWeapon))
    .PropertyValue(nameof(Damage));
```

**Analysis**:
- ❌ `PropertyBindings.Add()` requires mutable collection
- ❌ Can't call methods in object initializer
- ✅ Chaining property navigation is interesting (added as dot notation)

### Your Idea 2
```csharp
PropertyBindings.Add(nameof(myProperty))
    .FromNamedObject("PlayerCharacter")
    .PropertyValue("CurrentHealth");
```

**Analysis**:
- ❌ Same Add() problem
- ✅ Named lookup is good (included as `FromSibling("name")`)

### Your Idea 3
```csharp
MyComponentProperty.PropertyBindings.Add(
    new PropertyBinding()
        .FromParent()
        .PropertyValue(nameof(SomeProperty))
);
```

**Analysis**:
- ❌ Would need per-property binding collections
- ❌ More complex structure
- ✅ Builder pattern is good

## Recommended Final API

```csharp
public record Template
{
    // ... existing properties
    
    /// <summary>
    /// Property bindings for this component.
    /// Use PropertyBinding.For() builder to create bindings.
    /// </summary>
    public PropertyBinding[] PropertyBindings { get; init; } = [];
}

// Usage
new HealthBarTemplate()
{
    PropertyBindings = 
    [
        PropertyBinding.For(nameof(Health))
            .FromParent<HealthSystem>()
            .Property(nameof(HealthSystem.Health))
            .Build(),
            
        PropertyBinding.For(nameof(BackgroundColor))
            .FromContext<ThemeContext>()
            .Property(nameof(ThemeContext.HealthBarColor))
            .WithFallback(Colors.Red)
            .Build()
    ]
}
```

## Implementation Notes

### Discriminated Union for Lookup
Using C# 9.0 record inheritance for type-safe lookup strategies:

```csharp
BindingLookup lookup = binding.Lookup switch
{
    BindingLookup.ParentType(var type) => ResolveParent(component, type),
    BindingLookup.ContextType(var type) => ResolveContext(component, type),
    BindingLookup.SiblingName(var name) => ResolveSibling(component, name),
    BindingLookup.Path(var path) => ResolvePath(component, path),
    _ => throw new InvalidOperationException("Unknown lookup type")
};
```

### Extension Methods for Converters

```csharp
public static class BindingConverterExtensions
{
    public static PropertyBindingBuilder ToPercentage(this PropertyBindingBuilder builder)
        => builder.WithConverter(new PercentageConverter());
    
    public static PropertyBindingBuilder ToUpperCase(this PropertyBindingBuilder builder)
        => builder.WithConverter(new UpperCaseConverter());
}

// Usage
PropertyBinding.For(nameof(DisplayText))
    .FromParent<HealthSystem>()
    .Property(nameof(Health))
    .ToPercentage()  // Extension method
    .Build()
```

This design gives you the fluent, type-safe API you want while working within C#'s static initialization constraints!
