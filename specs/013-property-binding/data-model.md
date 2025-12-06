# Data Model: Property Binding System

**Feature**: Property Binding System  
**Branch**: 013-property-binding  
**Date**: December 6, 2025

## Overview

This document defines the entities, relationships, and validation rules for the Property Binding System. The system enables declarative property synchronization between components using a composition-first approach with source-generated type-safe binding configuration.

## Core Entities

### PropertyBinding

**Purpose**: Encapsulates the configuration and runtime state for a single property binding between a source and target component.

**Fields**:

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `LookupStrategy` | `ILookupStrategy` | Yes | Strategy for resolving the source component (null until fluent API call) |
| `SourcePropertyName` | `string` | Yes | Name of the property on the source component (null until GetPropertyValue call) |
| `Converter` | `IValueConverter` | Yes | Optional converter for transforming values (null = no conversion) |
| `Mode` | `BindingMode` | No | OneWay or TwoWay binding mode (default: OneWay) |
| `_sourceComponent` | `IComponent` | Yes | Cached reference to resolved source component (null until Activate) |
| `_sourceEvent` | `EventInfo` | Yes | Reflected event info for source PropertyChanged (null until Activate) |
| `_sourceHandler` | `Delegate` | Yes | Event handler delegate subscribed to source (null until Activate) |
| `_targetEvent` | `EventInfo` | Yes | Reflected event info for target PropertyChanged (TwoWay only) |
| `_targetHandler` | `Delegate` | Yes | Event handler delegate subscribed to target (TwoWay only) |
| `_isUpdating` | `bool` | No | Re-entry guard for preventing infinite loops in TwoWay bindings |

**State Transitions**:
1. **Created** → `Activate()` → **Active** (event subscriptions established)
2. **Active** → `Deactivate()` → **Inactive** (event subscriptions removed)

**Validation Rules**:
- `LookupStrategy` MUST be set before calling `Activate()` (enforced by fluent API pattern)
- `SourcePropertyName` MUST be set before calling `Activate()` (enforced by fluent API pattern)
- Target component and property name provided during `Activate()` call
- If `Converter` is set and `Mode` is `TwoWay`, converter MUST implement `IBidirectionalConverter`

**Relationships**:
- Uses one `ILookupStrategy` implementation to resolve source component
- Optionally uses one `IValueConverter` for value transformation
- Subscribed to one source component's PropertyChanged event
- Invokes one target component's `SetCurrent{PropertyName}()` method
- In TwoWay mode, subscribed to target component's PropertyChanged event

### PropertyBindings (Base Class)

**Purpose**: Base class for source-generated `{ComponentName}PropertyBindings` classes. Provides enumeration capability for iterating over configured bindings.

**Fields**: None (base class only)

**Methods**:
- `GetEnumerator()`: Returns `IEnumerator<(string propertyName, PropertyBinding binding)>`
- Implements `IEnumerable<(string, PropertyBinding)>`

**Derived Classes** (generated):
- `HealthBarPropertyBindings` (example)
- `ButtonPropertyBindings` (example)
- One class per component with `[ComponentProperty]` attributes

**Validation Rules**:
- Generated classes MUST have one nullable `PropertyBinding?` property per `[ComponentProperty]` on the component
- Property names MUST match the public property name (not the backing field name)

**Relationships**:
- Contains 0..N `PropertyBinding` instances (one per component property that has a binding configured)
- Owned by one `Template` instance via `Bindings` property

### ILookupStrategy

**Purpose**: Interface defining how to resolve source components in the component tree.

**Methods**:
- `IComponent? Resolve(IComponent targetComponent)`: Returns the source component or null if not found

**Implementations**:

1. **ParentLookup<T>**
   - Searches up the tree for the first parent of type `T`
   - Returns `targetComponent.Parent as T`

2. **SiblingLookup<T>**
   - Searches siblings (parent's children) for first match of type `T`
   - Returns first `targetComponent.Parent?.Children.OfType<T>().FirstOrDefault()`

3. **ChildLookup<T>**
   - Searches immediate children for first match of type `T`
   - Returns `targetComponent.Children.OfType<T>().FirstOrDefault()`

4. **ContextLookup<T>**
   - Searches up the tree recursively for first ancestor of type `T`
   - Returns first matching parent, grandparent, etc.

5. **NamedObjectLookup**
   - Searches tree for component with matching `Name` property
   - Returns first `FindComponentByName(name)` (searches entire tree)

**Validation Rules**:
- Generic type `T` MUST extend `IComponent`
- `Resolve()` MUST return null if source not found (no exceptions thrown)

**Relationships**:
- Used by one `PropertyBinding` instance
- Resolves to one `IComponent` instance (or null)

### IValueConverter

**Purpose**: Interface for one-way value transformation during binding updates.

**Methods**:
- `object? Convert(object? value)`: Transforms source value to target value format

**Implementations**:

1. **StringFormatConverter**
   - Properties: `string Format` (required)
   - Example: `new StringFormatConverter { Format = "Health: {0:F1}" }`

2. **MultiplyConverter**
   - Properties: `float Factor` (required)
   - Example: `new MultiplyConverter { Factor = 0.01f }` (convert to percentage)

3. **PercentageConverter** (implements `IBidirectionalConverter`)
   - `Convert`: multiplies by 100 (0.75 → 75)
   - `ConvertBack`: divides by 100 (75 → 0.75)

**Validation Rules**:
- `Convert()` MAY return null (binding update skipped)
- `Convert()` MUST NOT throw exceptions (caught and logged, update skipped)
- Converter implementations SHOULD handle type mismatches gracefully

**Relationships**:
- Used by 0..1 `PropertyBinding` instances per converter instance
- Operates on values of any type (object-based)

### IBidirectionalConverter

**Purpose**: Interface extending `IValueConverter` for two-way bindings that need reverse transformation.

**Methods**:
- `object? Convert(object? value)`: Source → Target transformation (inherited)
- `object? ConvertBack(object? value)`: Target → Source transformation

**Validation Rules**:
- MUST implement both `Convert()` and `ConvertBack()` methods
- `ConvertBack()` MUST be the mathematical/logical inverse of `Convert()`
- Example: If `Convert(0.5) = 50`, then `ConvertBack(50) = 0.5`

**Relationships**:
- Used by `PropertyBinding` instances with `Mode = BindingMode.TwoWay`

### PropertyChangedEventArgs<T>

**Purpose**: Generic event arguments for property change notifications.

**Fields**:

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `OldValue` | `T` | No | Previous value before change |
| `NewValue` | `T` | No | Current value after change |

**Validation Rules**:
- Type parameter `T` MUST match the property's type
- Both values captured at time of change

**Relationships**:
- Used by generated `{PropertyName}Changed` events on components
- Consumed by `PropertyBinding` event handlers

### BindingMode (Enum)

**Purpose**: Defines the direction of data flow in a binding.

**Values**:

| Value | Description |
|-------|-------------|
| `OneWay` | Source → Target only (default) |
| `TwoWay` | Source ↔ Target (bidirectional) |

**Validation Rules**:
- `TwoWay` mode requires bidirectional converter if converter is used
- `TwoWay` mode requires target property to have PropertyChanged event

## Generated Code Entities

### Generated PropertyChanged Events

**Generated for**: Each `[ComponentProperty]` field with `NotifyChange = true`

**Pattern**:
```csharp
public event EventHandler<PropertyChangedEventArgs<T>>? {PropertyName}Changed;
```

**Invocation Point**: Inside generated `partial void On{PropertyName}Changed(T oldValue)` method

**Example**:
```csharp
// For [ComponentProperty(NotifyChange = true)] float _health
public event EventHandler<PropertyChangedEventArgs<float>>? HealthChanged;

partial void OnHealthChanged(float oldValue)
{
    HealthChanged?.Invoke(this, new PropertyChangedEventArgs<float>(oldValue, Health));
}
```

### Generated PropertyBindings Classes

**Generated for**: Each component with at least one `[ComponentProperty]` attribute

**Pattern**:
```csharp
public class {ComponentName}PropertyBindings : PropertyBindings
{
    public PropertyBinding? {PropertyName1} { get; set; }
    public PropertyBinding? {PropertyName2} { get; set; }
    // ... one per component property
    
    public override IEnumerator<(string, PropertyBinding)> GetEnumerator()
    {
        if ({PropertyName1} != null) yield return (nameof({PropertyName1}), {PropertyName1});
        if ({PropertyName2} != null) yield return (nameof({PropertyName2}), {PropertyName2});
        // ... one per property
    }
}
```

**Example**:
```csharp
public class HealthBarPropertyBindings : PropertyBindings
{
    public PropertyBinding? CurrentHealth { get; set; }
    public PropertyBinding? MaxHealth { get; set; }
    
    public override IEnumerator<(string, PropertyBinding)> GetEnumerator()
    {
        if (CurrentHealth != null) yield return (nameof(CurrentHealth), CurrentHealth);
        if (MaxHealth != null) yield return (nameof(MaxHealth), MaxHealth);
    }
}
```

### Generated Template Bindings Property

**Generated for**: Each template record (modification to existing TemplateGenerator)

**Pattern**:
```csharp
public partial record {ComponentName}Template
{
    public {ComponentName}PropertyBindings Bindings { get; init; } = new();
}
```

**Example**:
```csharp
public partial record HealthBarTemplate
{
    public HealthBarPropertyBindings Bindings { get; init; } = new();
}
```

## Component Lifecycle Integration

### Component.OnLoad()

**Responsibility**: Store binding configurations from template for later activation

**Pattern**:
```csharp
private List<(string propertyName, PropertyBinding binding)> _bindings = [];

protected override void OnLoad(Template? template)
{
    base.OnLoad(template);
    
    if (template?.Bindings != null)
    {
        _bindings = template.Bindings.ToList();
    }
}
```

### Component.OnActivate()

**Responsibility**: Activate all bindings (resolve sources, subscribe to events, perform initial sync)

**Pattern**:
```csharp
protected override void OnActivate()
{
    base.OnActivate();
    
    foreach (var (propertyName, binding) in _bindings)
    {
        binding.Activate(this, propertyName);
    }
}
```

### Component.OnDeactivate()

**Responsibility**: Deactivate all bindings (unsubscribe events, clear references)

**Pattern**:
```csharp
protected override void OnDeactivate()
{
    foreach (var (_, binding) in _bindings)
    {
        binding.Deactivate();
    }
    
    base.OnDeactivate();
}
```

## Entity Relationship Diagram

```
Template
  └─► PropertyBindings (1:1)
       └─► PropertyBinding[] (1:N)
            ├─► ILookupStrategy (1:1)
            │    ├─ ParentLookup<T>
            │    ├─ SiblingLookup<T>
            │    ├─ ChildLookup<T>
            │    ├─ ContextLookup<T>
            │    └─ NamedObjectLookup
            │
            ├─► IValueConverter? (0:1)
            │    ├─ StringFormatConverter
            │    ├─ MultiplyConverter
            │    └─ PercentageConverter : IBidirectionalConverter
            │
            └─► BindingMode (1:1 enum value)

Component (runtime)
  ├─► _bindings[] (0:N) - stored during OnLoad
  └─► {PropertyName}Changed events (0:N) - generated
       └─► PropertyChangedEventArgs<T>
```

## Type Safety Guarantees

1. **Compile-Time Safety**:
   - Fluent API methods (`FromParent<T>()`) enforce `T : IComponent` constraint
   - PropertyBindings classes have properties matching component's actual properties
   - Generated events use strongly-typed `PropertyChangedEventArgs<T>`

2. **Runtime Safety**:
   - Missing source components result in silent failure (null check, no exception)
   - Type mismatches during conversion return null (update skipped)
   - Reflection failures during event subscription are logged but don't crash

3. **Memory Safety**:
   - Event subscriptions stored as weak references or cleaned up in `Deactivate()`
   - No circular references between components via bindings
   - TwoWay mode uses `_isUpdating` flag to prevent infinite recursion

## Validation Summary

| Entity | Validation Rules |
|--------|------------------|
| PropertyBinding | LookupStrategy and SourcePropertyName must be non-null before Activate |
| ILookupStrategy | Resolve() returns null for not-found (no exceptions) |
| IValueConverter | Convert() may return null, must not throw |
| IBidirectionalConverter | ConvertBack() must be inverse of Convert() |
| PropertyBindings | Generated properties match component's ComponentProperty names |
| Component | Bindings activated in OnActivate(), deactivated in OnDeactivate() |

## Extensibility Points

1. **Custom Lookup Strategies**: Implement `ILookupStrategy` for domain-specific component resolution
2. **Custom Converters**: Implement `IValueConverter` or `IBidirectionalConverter` for specialized transformations
3. **Property Attributes**: Future enhancement could add validation attributes to `[ComponentProperty]`
4. **Binding Middleware**: Future enhancement could support intercepting binding updates for logging/debugging
