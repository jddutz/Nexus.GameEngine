# Data Model: Property Binding Framework

**Feature**: `016-property-binding-v2`  
**Date**: December 7, 2025  
**Purpose**: Define entities, relationships, and state transitions for property binding system

## Core Entities

### 1. PropertyBinding<TSource, TValue>

**Description**: Generic property binding class that configures and manages runtime synchronization between a source component's property and a target component's property.

**Type Parameters**:
- `TSource`: The source component type (constant throughout pipeline)
- `TValue`: The current value type flowing through the transformation pipeline

**Fields**:
```csharp
// Configuration (set during Load phase)
private ILookupStrategy? _lookupStrategy;           // How to find source component
private string? _sourcePropertyName;                 // Name of source property to watch
private IValueConverter? _converter;                 // Optional value transformation
private BindingMode _mode;                           // OneWay or TwoWay
private Func<TSource, TValue>? _transform;          // Compiled transformation function

// Runtime state (set during Activate phase)
private IComponent? _sourceComponent;                // Resolved source component reference
private EventInfo? _sourceEvent;                     // Source property change event
private Delegate? _sourceHandler;                    // Event handler delegate
private PropertyInfo? _sourceProperty;               // Source property reflection info

private IComponent? _targetComponent;                // Target component owning the binding
private Action<object?>? _targetSetter;             // Target property setter delegate
private EventInfo? _targetEvent;                     // Target property change event (two-way)
private Delegate? _targetHandler;                    // Target event handler (two-way)

private bool _isUpdating;                            // Recursion guard flag
```

**Methods**:
```csharp
// Fluent configuration API (Load phase)
PropertyBinding<TSource, TProp> GetPropertyValue<TProp>(Expression<Func<TSource, TProp>> selector)
PropertyBinding<TSource, string> AsFormattedString(string format)
PropertyBinding<TSource, TValue> WithConverter(IValueConverter converter)
PropertyBinding<TSource, TValue> TwoWay()

// Lifecycle methods (runtime)
void Activate(IComponent targetComponent, string targetPropertyName, Action<object?> setter)
void Deactivate()

// Event handlers (internal)
void OnSourcePropertyChanged<T>(object sender, PropertyChangedEventArgs<T> e)
void OnTargetPropertyChanged<T>(object sender, PropertyChangedEventArgs<T> e)
```

**Validation Rules**:
- `_lookupStrategy` must be set before activation
- `_sourcePropertyName` must be set before activation
- `_transform` must be set if property extraction was performed
- Target setter must be provided during activation
- Source component must be found during activation (or skip gracefully)

**State Transitions**:
```
[Created] --(FindParent/FromSibling/etc)--> [Strategy Set]
          --(GetPropertyValue)--> [Property Selected]
          --(AsFormattedString/WithConverter)--> [Converter Configured]
          --(Activate)--> [Active] or [Failed Resolution]
[Active] --(Deactivate)--> [Inactive]
[Inactive] --(Activate)--> [Active]
```

---

### 2. IPropertyBinding

**Description**: Non-generic marker interface enabling collection storage and polymorphic lifecycle management.

**Methods**:
```csharp
void Activate();
void Deactivate();
```

**Purpose**: Allows `Component.PropertyBindings` to store bindings with different generic type parameters in a single collection.

---

### 3. IPropertyBindingDefinition

**Description**: Marker interface for template binding definitions before they are converted to active PropertyBinding instances.

**Purpose**: Type-safe representation of binding configurations in template records. Currently a placeholder for future template validation.

**Implementation Note**: Currently empty interface. May be extended with metadata properties in future revisions.

---

### 4. ILookupStrategy

**Description**: Strategy interface for resolving source components in the component tree.

**Methods**:
```csharp
IComponent? Resolve(IComponent target);
```

**Implementations**:

#### ParentLookup<TSource>
Searches up the parent chain for the first component of type TSource.

```csharp
public class ParentLookup<TSource> : ILookupStrategy where TSource : class, IComponent
{
    public IComponent? Resolve(IComponent target)
    {
        var current = target.Parent;
        while (current != null)
        {
            if (current is TSource typed) return typed;
            current = current.Parent;
        }
        return null;
    }
}
```

#### SiblingLookup<TSource>
Searches siblings (parent's children) for the first component of type TSource.

```csharp
public class SiblingLookup<TSource> : ILookupStrategy where TSource : class, IComponent
{
    public IComponent? Resolve(IComponent target)
    {
        var parent = target.Parent;
        if (parent == null) return null;
        
        foreach (var child in parent.Children)
        {
            if (child != target && child is TSource typed) 
                return typed;
        }
        return null;
    }
}
```

#### ChildLookup<TSource>
Searches immediate children for the first component of type TSource.

```csharp
public class ChildLookup<TSource> : ILookupStrategy where TSource : class, IComponent
{
    public IComponent? Resolve(IComponent target)
    {
        foreach (var child in target.Children)
        {
            if (child is TSource typed) return typed;
        }
        return null;
    }
}
```

#### NamedObjectLookup
Recursively searches the entire component tree for a component with a specific name.

```csharp
public class NamedObjectLookup : ILookupStrategy
{
    private readonly string _name;
    
    public NamedObjectLookup(string name)
    {
        _name = name;
    }
    
    public IComponent? Resolve(IComponent target)
    {
        // Find root
        var root = target;
        while (root.Parent != null) root = root.Parent;
        
        // Recursive search
        return SearchTree(root);
    }
    
    private IComponent? SearchTree(IComponent node)
    {
        if (node.Name == _name) return node;
        
        foreach (var child in node.Children)
        {
            var result = SearchTree(child);
            if (result != null) return result;
        }
        
        return null;
    }
}
```

#### ContextLookup<TSource>
Searches up the ancestor chain (similar to ParentLookup but emphasized for architectural clarity).

```csharp
public class ContextLookup<TSource> : ILookupStrategy where TSource : class, IComponent
{
    public IComponent? Resolve(IComponent target)
    {
        var current = target.Parent;
        while (current != null)
        {
            if (current is TSource typed) return typed;
            current = current.Parent;
        }
        return null;
    }
}
```

**Performance Characteristics**:
- `ParentLookup`: O(tree depth) - typically <10 iterations
- `SiblingLookup`: O(sibling count) - typically <20 iterations
- `ChildLookup`: O(child count) - typically <10 iterations
- `NamedObjectLookup`: O(tree size) - potentially hundreds, use sparingly
- `ContextLookup`: O(tree depth) - same as ParentLookup

---

### 5. IValueConverter

**Description**: Interface for custom value transformation during binding updates.

**Methods**:
```csharp
object? Convert(object? value);
```

**Implementations**:

#### StringFormatConverter
Built-in converter for string formatting using .NET format strings.

```csharp
public class StringFormatConverter : IValueConverter
{
    private readonly string _format;
    
    public StringFormatConverter(string format)
    {
        _format = format;
    }
    
    public object? Convert(object? value)
    {
        try
        {
            return string.Format(_format, value);
        }
        catch (FormatException)
        {
            // Log error and return fallback
            return value?.ToString() ?? "";
        }
    }
}
```

**Usage Examples**:
```csharp
new StringFormatConverter("Health: {0:F0}")      // Integer formatting
new StringFormatConverter("Score: {0:N0}")       // Thousand separators
new StringFormatConverter("{0:P0}")              // Percentage
new StringFormatConverter("{0:C2}")              // Currency
```

---

### 6. IBidirectionalConverter

**Description**: Extension of IValueConverter for two-way bindings (future enhancement).

**Methods**:
```csharp
object? Convert(object? value);       // Source → Target
object? ConvertBack(object? value);   // Target → Source
```

**Note**: Out of scope for this revision but interface exists for future two-way binding support.

---

### 7. BindingMode

**Description**: Enumeration defining binding synchronization mode.

```csharp
public enum BindingMode
{
    OneWay = 0,      // Source → Target only (default)
    TwoWay = 1       // Source ↔ Target bidirectional (future)
}
```

---

### 8. PropertyBindingsCollection

**Description**: Base class for source-generated PropertyBindings classes (existing, may be deprecated).

**Methods**:
```csharp
IEnumerator<(string propertyName, IPropertyBinding binding)> GetEnumerator();
```

**Status**: Existing class. Evaluate during implementation whether this abstraction is still needed or if `List<IPropertyBinding>` in Component.PropertyBindings.cs is sufficient.

---

### 9. Binding (Static Factory)

**Description**: Static factory class providing fluent entry points for creating property bindings.

**Methods**:
```csharp
public static class Binding
{
    public static PropertyBinding<TSource, TSource> FromParent<TSource>() 
        where TSource : class, IComponent;
    
    public static PropertyBinding<TSource, TSource> FromNamedObject<TSource>(string name) 
        where TSource : class, IComponent;
    
    public static PropertyBinding<TSource, TSource> FromSibling<TSource>() 
        where TSource : class, IComponent;
    
    public static PropertyBinding<TSource, TSource> FromChild<TSource>() 
        where TSource : class, IComponent;
    
    public static PropertyBinding<TSource, TSource> FromContext<TSource>() 
        where TSource : class, IComponent;
}
```

**Usage in Templates**:
```csharp
new TextRendererTemplate
{
    Bindings = new[]
    {
        Binding.FromParent<PlayerComponent>()
            .GetPropertyValue(p => p.Health)
            .AsFormattedString("Health: {0:F0}")
            // .Set(SetText) ← How to specify target?
    }
}
```

**Open Question**: How does the binding definition specify the target property setter? Current implementation seems incomplete. Needs research/clarification.

---

## Relationships

```
Component (1) ----has-many----> (N) IPropertyBinding
    |
    +-- OnLoad() creates PropertyBinding instances
    +-- OnActivate() calls Activate() on each
    +-- OnDeactivate() calls Deactivate() on each

PropertyBinding<TSource, TValue> (1) ----uses----> (1) ILookupStrategy
    |                                                    |
    |                                                    +-- ParentLookup
    |                                                    +-- SiblingLookup
    |                                                    +-- ChildLookup
    |                                                    +-- NamedObjectLookup
    |                                                    +-- ContextLookup
    |
    +--uses--> (0..1) IValueConverter
    |                    |
    |                    +-- StringFormatConverter
    |                    +-- CustomConverter (user-defined)
    |
    +--references--> (0..1) IComponent (source)
    +--references--> (0..1) IComponent (target)

Binding (static factory) ----creates----> PropertyBinding<TSource, TValue>
```

---

## State Machine: PropertyBinding Lifecycle

```
┌─────────────────────────────────────────────────────────────────┐
│                      CONFIGURATION PHASE                         │
│                         (Template Load)                          │
└─────────────────────────────────────────────────────────────────┘
                                ↓
                         [Uninitialized]
                                ↓
               (Binding.FromParent/FromSibling/etc)
                                ↓
                        [Strategy Set]
                                ↓
                      (GetPropertyValue)
                                ↓
                     [Property Selected]
                                ↓
              (AsFormattedString/WithConverter) [Optional]
                                ↓
                      [Converter Configured]
                                ↓
                  (Added to Component.PropertyBindings)
                                ↓
┌─────────────────────────────────────────────────────────────────┐
│                       ACTIVATION PHASE                           │
│                      (Component.OnActivate)                      │
└─────────────────────────────────────────────────────────────────┘
                                ↓
                          (Activate())
                                ↓
                   (Resolve source component)
                        ↙              ↘
                   Found            Not Found
                     ↓                  ↓
              [Resolving Event]   [Failed Resolution]
                     ↓                  (Skip activation,
         (Find property change event)   log warning)
                ↙        ↘
           Found      Not Found
             ↓            ↓
    [Subscribing]   [Event Missing]
             ↓       (Log warning,
    (Subscribe via   binding won't
     EventInfo)      update)
             ↓
    [Initial Sync]
             ↓
    (Read source value, convert, set target)
             ↓
        [ACTIVE]
             ↓
    (Property change events trigger updates)
             ↓
┌─────────────────────────────────────────────────────────────────┐
│                      DEACTIVATION PHASE                          │
│                     (Component.OnDeactivate)                     │
└─────────────────────────────────────────────────────────────────┘
             ↓
       (Deactivate())
             ↓
    (Unsubscribe from events)
             ↓
    (Clear component references)
             ↓
        [INACTIVE]
             ↓
    (Can be reactivated later)
```

---

## Data Flow: Property Change Propagation

```
1. Source Property Changes
   ↓
2. Source Component raises {PropertyName}Changed event
   ↓
3. PropertyBinding.OnSourcePropertyChanged<T>() handler invoked
   ↓
4. Guard check: if (_isUpdating) return;
   ↓
5. Set _isUpdating = true
   ↓
6. Extract e.NewValue
   ↓
7. If converter exists: value = _converter.Convert(value)
   ↓
8. If conversion fails: log error, return
   ↓
9. Invoke _targetSetter(value)
   ↓
10. Target component's Set{PropertyName}() method executes
   ↓
11. Target property updated
   ↓
12. Set _isUpdating = false
   ↓
13. Target may raise {PropertyName}Changed (ignored due to guard)
```

**Performance**: Steps 1-13 typically complete in <0.1ms for simple conversions.

---

## Validation Rules Summary

### Configuration Phase (Load)
- ✅ Lookup strategy must be set (via Binding.FromParent/etc)
- ✅ Property selector must extract valid property name
- ✅ Format string must be valid (validated at first conversion)
- ❌ **MISSING**: How is target setter specified? Needs clarification.

### Activation Phase
- ✅ Source component must exist (or skip gracefully)
- ✅ Source property must exist (or skip gracefully)
- ✅ Source event should exist (warn if missing)
- ✅ Target component must be provided
- ✅ Target setter must be provided

### Runtime Phase
- ✅ No circular update loops (_isUpdating guard)
- ✅ Converter errors handled gracefully (log and skip)
- ✅ Null values handled safely

---

## Open Questions / Clarifications Needed

### 1. Target Setter Specification (CRITICAL)

**Current Problem**: The fluent API doesn't show how the target property setter is specified in the template.

**Possible Approaches**:

**Option A**: Terminal `.Set()` method
```csharp
Binding.FromParent<PlayerComponent>()
    .GetPropertyValue(p => p.Health)
    .AsFormattedString("Health: {0:F0}")
    .Set(SetText)  // Pass setter method directly
```

**Option B**: Property name string
```csharp
Binding.FromParent<PlayerComponent>()
    .GetPropertyValue(p => p.Health)
    .AsFormattedString("Health: {0:F0}")
    .To("Text")  // Resolve SetText via reflection
```

**Option C**: Expression-based
```csharp
Binding.FromParent<PlayerComponent>()
    .GetPropertyValue(p => p.Health)
    .AsFormattedString("Health: {0:F0}")
    .To(t => t.Text)  // Extract property, find setter
```

**Recommendation**: Option A (explicit setter delegate) based on FR-009 stating "PropertyBinding MUST require an explicit target setter method". This is most explicit and avoids reflection overhead during updates.

**Action Required**: Confirm approach and update PropertyBinding.cs implementation.

---

### 2. PropertyBindingsCollection Necessity

**Question**: Is the `PropertyBindingsCollection` abstract base class still needed, or can we use `List<IPropertyBinding>` directly?

**Current State**: Component.PropertyBindings.cs uses `List<IPropertyBinding>` directly.

**Recommendation**: Deprecate `PropertyBindingsCollection` unless source generators for property-specific bindings are planned (which contradicts FR-011).

**Action Required**: Confirm deprecation or identify use case.

---

### 3. IPropertyBindingDefinition Purpose

**Question**: What is the intended purpose of `IPropertyBindingDefinition` marker interface?

**Current State**: Empty interface with no members.

**Possible Uses**:
- Type marker for template binding configurations
- Future metadata properties (priority, validation rules)
- Reflection-based binding registration

**Recommendation**: If no immediate use, consider removing to reduce complexity.

**Action Required**: Confirm purpose or remove.

---

## Summary

Data model defines 9 core entities with clear relationships and lifecycle:
1. **PropertyBinding<TSource, TValue>**: Core binding logic with fluent configuration
2. **IPropertyBinding**: Non-generic lifecycle interface
3. **IPropertyBindingDefinition**: Template marker (purpose unclear)
4. **ILookupStrategy**: Source resolution strategy with 5 implementations
5. **IValueConverter**: Value transformation with StringFormatConverter built-in
6. **IBidirectionalConverter**: Future two-way binding support
7. **BindingMode**: OneWay vs TwoWay enumeration
8. **PropertyBindingsCollection**: Base class (may be deprecated)
9. **Binding**: Static factory for fluent API entry points

**Critical open question**: How is the target setter specified in the fluent API? Recommend explicit `.Set(setter)` method per FR-009.

Ready to proceed to contracts generation after resolving target setter specification.
