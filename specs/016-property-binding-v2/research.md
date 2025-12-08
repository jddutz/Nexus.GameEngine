# Research: Property Binding Framework Revision

**Feature**: `016-property-binding-v2`  
**Date**: December 7, 2025  
**Purpose**: Resolve unknowns and establish best practices for property binding implementation

## Executive Summary

The Property Binding Framework is a **template-based event handler configuration system** that synchronizes component properties at runtime without coupling. PropertyBindings are essentially event handlers configured via fluent API in templates, managed by target components, with source components remaining passive.

**Core Principles**:
1. PropertyBinding = Event Handler (not a complex binding pipeline)
2. Template-based configuration (no source generators required)
3. Target-managed lifecycle (source is passive)
4. Type-safe without reflection in hot path
5. Minimal defaults with explicit setter requirement

## Research Areas

### 1. Do We Need Source Generators?

**Question**: Can the property binding framework function without source generators while maintaining type safety and performance?

**Decision**: **NO SOURCE GENERATORS REQUIRED** - Use explicit setter delegates provided in binding configuration

**Rationale**: 
- Templates already provide type information via generic parameters: `PropertyBinding<TSource, TOutput>()`
- Setters are component methods that already exist (e.g., `SetText`, `SetHealth`)
- No need to generate `{Component}PropertyBindings` classes - just use `IPropertyBinding[]`
- Simpler architecture, easier to debug, no build-time complexity
- Performance is equivalent - setter delegates are just as fast as generated code

**What can be removed**:
- ❌ `PropertyBindingsGenerator.cs` - entire generator not needed
- ❌ `{Component}PropertyBindings` classes - use simple arrays instead
- ❌ `IPropertyBindingDefinition` interface - just use `IPropertyBinding` directly
- ❌ Generated `GetEnumerator()` methods - standard collection enumeration

**Revised Template Pattern**:
```csharp
// OLD (complex, requires source generator):
Bindings = new HealthBarPropertyBindings
{
    CurrentHealth = PropertyBinding.From("Player", "Health")
};

// NEW (simple, no generators):
Bindings = [
    new PropertyBinding<PlayerComponent, string>()
        .FindParent()
        .On(p => p.HealthChanged)
        .ConvertToString(formatter)
        .Set(SetText)  // Explicit setter method
]
```

**Implementation notes**:
```csharp
// Component.PropertyBindings.cs - simple array
protected List<IPropertyBinding> PropertyBindings { get; } = new();

// Template.cs - simple array property
public IPropertyBinding[] Bindings { get; init; } = Array.Empty<IPropertyBinding>();

// Component.Load(Template template)
protected virtual void LoadPropertyBindings()
{
    foreach(var binding in template.Bindings)
    {
        PropertyBindings.Add(binding);
    }
}
```

**References**:
- Delegate performance: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/delegates/

---

### 2. Event Subscription Patterns

**Question**: How should PropertyBindings subscribe to events - reflection-based or compile-time delegates?

**Decision**: **Use Expression<> for compile-time event extraction, store as EventInfo, subscribe with typed delegates**

**Rationale**: 
- `.On(p => p.HealthChanged)` provides compile-time type safety
- Expression can be compiled once, EventInfo cached
- Event subscription still uses EventInfo.AddEventHandler for proper cleanup
- Balance between type safety (expression) and flexibility (reflection subscription)
- Hot path (event firing) uses compiled delegates - zero reflection overhead

**Why not full reflection**: 
- String-based event names are error-prone: `.On("HealthChanged")` vs `.On(p => p.HealthChanged)`
- Refactoring support - renaming events updates all bindings automatically
- IntelliSense support during template authoring

**Implementation notes**:
```csharp
// Configuration phase (in template)
public PropertyBinding<TSource, TValue> On(Expression<Func<TSource, EventHandler<PropertyChangedEventArgs<TValue>>>> eventSelector)
{
    if (eventSelector.Body is not MemberExpression member || member.Member is not EventInfo eventInfo)
        throw new ArgumentException("Expression must reference an event");
    
    _sourceEvent = eventInfo;
    return this;
}

// Activation phase (subscribe)
public void Activate()
{
    _sourceComponent = _lookupStrategy.Resolve(_targetComponent);
    if (_sourceComponent == null) return; // Skip gracefully
    
    // Create typed delegate for event handler
    _sourceHandler = CreateEventHandler();
    _sourceEvent.AddEventHandler(_sourceComponent, _sourceHandler);
}

// Deactivation phase (unsubscribe)  
public void Deactivate()
{
    if (_sourceComponent != null && _sourceEvent != null && _sourceHandler != null)
    {
        _sourceEvent.RemoveEventHandler(_sourceComponent, _sourceHandler);
    }
    _sourceComponent = null;
    _sourceHandler = null;
}
```

**Default behavior**: If `.On()` not called, try to find `{PropertyName}Changed` event, fall back to generic `PropertyChanged`

**References**:
- [Microsoft Docs: EventInfo Class](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.eventinfo)
- [Microsoft Docs: Expression Trees](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/)

---

### 3. Component Tree Navigation Strategies

**Question**: How should bindings efficiently resolve source components in a tree structure?

**Decision**: **Simple static helper methods, default to FindParent<TSource>(), store as delegate**

**Rationale**:
- No need for ILookupStrategy interface - just store `Func<IComponent, TSource?>` delegates
- Simple static helper methods are easier to understand and maintain
- Each helper optimizes for specific tree traversal patterns:
  - **FindParent<TSource>()**: Walk up tree until type match (default, 90%+ of bindings)
  - **FindSibling<TSource>()**: Check parent's children (sibling relationships)
  - **FindChild<TSource>()**: Search immediate children only (parent-to-child binding)
  - **FindNamed(name)**: Recursive search for named component (flexible but slower)
- Failed lookups silently skip activation (log warning) without throwing exceptions
- Lookup happens once during activation, then cached reference used for lifetime
- YAGNI principle - start simple, add complexity only when needed

**Simplified implementation**:
```csharp
// Simple static helper class (no interfaces)
public static class ComponentLookup
{
    public static TSource? FindParent<TSource>(IComponent target) where TSource : class, IComponent
    {
        var current = target.Parent;
        while (current != null)
        {
            if (current is TSource typed) return typed;
            current = current.Parent;
        }
        return null;
    }
    
    public static TSource? FindSibling<TSource>(IComponent target) where TSource : class, IComponent
    {
        if (target.Parent == null) return null;
        return target.Parent.Children.OfType<TSource>().FirstOrDefault(c => c != target);
    }
}

// PropertyBinding stores lookup as simple delegate
private Func<IComponent, TSource?>? _lookupFunc;

public PropertyBinding<TSource, TValue> FindParent()
{
    _lookupFunc = ComponentLookup.FindParent<TSource>;
    return this;
}
```

**Performance**: Parent lookup is O(tree depth), typically <10 iterations. Named lookup is O(tree size) but rarely needed.

---

### 4. Value Transformation and Conversion

**Question**: How should PropertyBinding extract values from source properties and convert types?

**Decision**: **Use compiled expressions for property extraction, simple converter pattern for formatting**

**Rationale**:
- `.GetPropertyValue(p => p.Health)` compiles to fast property accessor (no reflection in hot path)
- Optional `.ConvertToString(formatter)` for simple string formatting
- Formatters are just `Func<T, string>` delegates - no interface needed
- Type safety maintained through generics: `PropertyBinding<TSource, TOutput>`
- Simplest approach that maintains performance

**Implementation notes**:
```csharp
// Compiled property accessor (set during configuration)
private Func<TSource, TValue>? _propertyGetter;

public PropertyBinding<TSource, TProp> GetPropertyValue<TProp>(Expression<Func<TSource, TProp>> selector)
{
    var compiled = selector.Compile(); // Compile once
    return new PropertyBinding<TSource, TProp>(_lookupFunc, compiled, _eventInfo);
}

// String conversion helper
public PropertyBinding<TSource, string> ConvertToString(Func<TValue, string> formatter)
{
    Func<TSource, string> stringGetter = source => formatter(_propertyGetter!(source));
    return new PropertyBinding<TSource, string>(_lookupFunc, stringGetter, _eventInfo);
}

// Event handler (hot path) - zero reflection
private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs<TValue> e)
{
    if (_isUpdating) return;
    try
    {
        _isUpdating = true;
        var value = _propertyGetter!(_sourceComponent!);
        _targetSetter!(value);
    }
    finally { _isUpdating = false; }
}
```

**Usage in template**:
```csharp
Bindings = [
    new PropertyBinding<PlayerComponent, float>()
        .FindParent()
        .On(p => p.HealthChanged)
        .GetPropertyValue(p => p.Health)
        .ConvertToString(h => $"Health: {h:F0}")
        .Set(SetText)
]
```

**Performance**: Property getter compiled once, zero reflection overhead during event handling.

---

### 5. Fluent API Method Chaining vs Immutability

**Question**: Should PropertyBinding be immutable with each method returning a new instance, or mutable with method chaining?

**Decision**: **Hybrid - mutable for same type, new instance when changing TValue type**

**Rationale**:
- Most fluent methods return `this` for efficiency (FindParent, On, Set)
- Type-transforming methods MUST create new instance due to generic constraint (GetPropertyValue, ConvertToString)
- Template definitions are one-time configuration during component load (not reused)
- Minimizes allocations while maintaining type safety
- Mutable pattern is simpler to implement and debug
- Type transformation requires new generic instances anyway (PropertyBinding<TSource, TProp>)
- No thread safety concerns (templates loaded on main thread during component initialization)

**Alternatives considered**:
- **Immutable builder pattern**: More allocations, no practical benefit for one-time template config
- **Separate builder and binding classes**: Adds complexity without clarity gains

**Implementation notes**:
```csharp
public PropertyBinding<TSource, TValue> WithConverter(IValueConverter converter)
{
    _converter = converter;
    return this; // Mutable chaining
}

// Type transformation creates new instance (different generic TValue)
public PropertyBinding<TSource, TProp> GetPropertyValue<TProp>(Expression<Func<TSource, TProp>> selector)
{
    return new PropertyBinding<TSource, TProp>(_lookupStrategy, propName, compiled, _converter, _mode);
}
```

---

### 6. Default Behavior vs Explicit Configuration

**Question**: What should be the sensible defaults when configuration methods are omitted?

**Decision**: 
- **Default source resolution**: FindParent<TSource>() (most common binding pattern - 90%+)
- **Default event**: Subscribe to `PropertyChanged` event (generic catch-all)
- **No default conversion**: Must explicitly call `.GetPropertyValue()` or value transformation methods
- **Required**: `.Set(setter)` method MUST be called (compiler enforces via return type)

**Rationale**:
- Parent-to-child property flow is the dominant UI pattern
- Generic PropertyChanged event is simplest - property-specific events are optimization
- Explicit transformation makes data flow visible in template
- Type system prevents forgetting `.Set()` - PropertyBinding<TSource, TValue> doesn't implement IPropertyBinding until `.Set()` called

**Implementation notes**:
```csharp
// Minimal binding (still requires explicit transformation)
new PropertyBinding<PlayerComponent, float>()
    // .FindParent() ← implicit (default)
    // .On(...) ← implicit (uses PropertyChanged)
    .GetPropertyValue(p => p.Health) // ← explicit transformation required
    .ConvertToString(h => $"Health: {h:F0}")
    .Set(SetText) // ← required (type system enforces)

// Without .Set(), type is PropertyBinding<Player, string> not IPropertyBinding
// Template.Bindings expects IPropertyBinding[] so compiler error if .Set() omitted
```

**Type-safe enforcement**:
```csharp
// PropertyBinding<TSource, TValue> is configuration builder
public class PropertyBinding<TSource, TValue> where TSource : class, IComponent
{
    // Returns IPropertyBinding when configuration is complete
    public IPropertyBinding Set(Action<TValue> setter) { ... }
}

// Template requires completed bindings
public IPropertyBinding[] Bindings { get; init; } = [];
```

---

### 7. Lifecycle Integration with Component System

**Question**: How should PropertyBinding lifecycle integrate with Component.OnLoad/OnActivate/OnDeactivate?

**Decision**: 
- **Template**: Define `IPropertyBinding[] Bindings` array
- **Component.Load**: Copy template bindings to component's `List<IPropertyBinding>` collection
- **Component.Activate**: Enumerate bindings, call `Activate(this)` on each
- **Component.Deactivate**: Enumerate bindings, call `Deactivate()` on each

**Rationale**:
- Separation of configuration (template/load) and execution (activate) follows existing patterns
- Lazy activation enables components to load before all dependencies are available
- Explicit deactivation ensures deterministic cleanup (no GC finalization dependency)
- Component tree structure is stable during activation (parents loaded before children)
- Simple array/list - no complex generated PropertyBindings classes

**Implementation notes**:
```csharp
// Template.cs - array of completed bindings
public IPropertyBinding[] Bindings { get; init; } = [];

// Component.PropertyBindings.cs - instance collection
protected List<IPropertyBinding> PropertyBindings { get; } = new();

protected virtual void LoadPropertyBindings()
{
    if (Template?.Bindings != null)
    {
        PropertyBindings.AddRange(Template.Bindings);
    }
}

// Component.Lifecycle.cs - Activate phase
protected virtual void ActivatePropertyBindings()
{
    foreach (var binding in PropertyBindings)
    {
        binding.Activate(this); // Pass target component
    }
}

// Component.Lifecycle.cs - Deactivate phase
protected virtual void DeactivatePropertyBindings()
{
    foreach (var binding in PropertyBindings)
    {
        binding.Deactivate(); // Unsubscribe events, clear refs
    }
}
```

**Activation flow**:
1. Template binding configured at compile-time (fluent API)
2. Component.Load() copies bindings to instance collection
3. Component.Activate() iterates collection:
   - Binding resolves source component via lookup delegate
   - Binding subscribes to source event
   - Binding performs initial value sync
4. Events fire → binding updates target property
5. Component.Deactivate() iterates collection:
   - Binding unsubscribes from source event
---

### 8. Preventing Recursive Updates

**Question**: How to prevent infinite update loops when both source and target properties notify changes?

**Decision**: Use `_isUpdating` flag to suppress notifications during programmatic updates

**Rationale**:
- Simple boolean guard prevents re-entry into update handlers
- Works for both one-way and two-way bindings
- Thread-safe for single-threaded component updates (main thread only)
- Minimal overhead (single boolean check per update)

**Alternatives considered**:
- **Transaction-based updates**: Overly complex for simple binding scenarios
- **Dependency graph analysis**: Runtime overhead, hard to debug cycles
- **Disable events during update**: Requires event disable/enable API on components

**Implementation notes**:
```csharp
private bool _isUpdating;

private void OnSourcePropertyChanged<T>(object sender, PropertyChangedEventArgs<T> e)
{
    if (_isUpdating) return; // Guard against recursion
    
    try
    {
        _isUpdating = true;
        var value = e.NewValue;
        if (_converter != null) value = _converter.Convert(value);
        _targetSetter(value);
    }
    finally
    {
        _isUpdating = false;
    }
}
```

**Note**: Two-way bindings are deferred to future revision, but foundation is laid.

---

### 9. Error Handling Strategy

**Question**: How should the framework handle errors during binding activation and updates?

**Decision**: **Fail fast on configuration errors, log and skip runtime resolution failures**

**Rationale**:
- **Configuration errors** (missing .Set(), invalid expression, wrong types): Developer mistakes → throw exceptions during template creation
- **Runtime resolution failures** (source component not found): Expected in dynamic UI → log warning and skip activation gracefully
- **Event subscription errors** (event doesn't exist): Log warning, binding won't update but component still functions
- Type safety from generics prevents most conversion errors at compile time

**Implementation notes**:
```csharp
// Configuration error - fail fast during template creation
public PropertyBinding<TSource, TProp> GetPropertyValue<TProp>(Expression<Func<TSource, TProp>> selector)
{
    if (selector.Body is not MemberExpression)
        throw new ArgumentException("Expression must be a property access");
    var compiled = selector.Compile();
    return new PropertyBinding<TSource, TProp>(_lookupFunc, compiled, _eventInfo);
}

// Runtime resolution failure - log and skip
public void Activate(IComponent targetComponent)
{
    _targetComponent = targetComponent;
    _sourceComponent = _lookupFunc?.Invoke(targetComponent) ?? ComponentLookup.FindParent<TSource>(targetComponent);
    
    if (_sourceComponent == null)
    {
        // TODO: Logging system
        // Log.Warning($"PropertyBinding: Source {typeof(TSource).Name} not found");
        return; // Skip activation gracefully
    }
    
    // Subscribe to event and perform initial sync
    SubscribeToEvent();
    SyncInitialValue();
}
```

---

### 10. Classes to Remove After Implementation

**Question**: What existing code can be removed once the simplified framework is in place?

**Decision**: Audit and remove these classes/generators after new implementation is verified:

**Source Generators**:
- ❌ `PropertyBindingsGenerator.cs` - entire generator not needed
- ❌ Generated `{Component}PropertyBindings.g.cs` files - replaced with simple arrays

**Abstract Base Classes**:
- ❓ `PropertyBindings.cs` - abstract base class for generated bindings (likely removable)
- ❓ `IPropertyBindingDefinition` - marker interface (assess if needed)

**Lookup Strategy Classes** (if implementing ILookupStrategy pattern):
- ❌ `ParentLookup<T>`, `SiblingLookup<T>`, etc. - replaced with static helpers
- ❌ `ILookupStrategy` interface - replaced with `Func<>` delegates

**Converter Classes** (if using IValueConverter pattern):
- ❓ `IValueConverter`, `StringFormatConverter` - assess if simpler `Func<>` delegates suffice
- ❓ Keep converter pattern if reusability across bindings is valuable

**Action Items**:
1. Scan `src/GameEngine/Components/` for PropertyBinding-related classes
2. Scan `src/SourceGenerators/` for PropertyBindingsGenerator
3. Scan `specs/013-property-binding/` for previous implementation artifacts
4. Create removal checklist after new implementation is complete

---

## Technology Stack Validation

**Confirmed Dependencies**:
- ✅ .NET 9.0 with C# 9.0+ language features (Expression<>, Func<> delegates, records)
- ✅ System.Linq.Expressions for property extraction (built-in, compile once)
- ✅ System.Reflection for event subscription (EventInfo.AddEventHandler, minimal usage)
- ✅ Existing component infrastructure (IComponent, Parent/Children tree navigation)
- ✅ Existing event infrastructure (PropertyChangedEventArgs<T>, component events)

**No New External Dependencies Required**
**No Source Generator Dependencies**

---

## Performance Considerations

**Optimization Strategy**:
- ✅ Compile expressions once during template creation (zero reflection in hot path)
- ✅ Cache event delegates during activation (reused for binding lifetime)
- ✅ Minimal allocations - most methods return `this` or reuse delegates
- ✅ Event handlers are simple delegate invocations (fast)

**Benchmarking Strategy**:
1. **Binding activation time**: Target <1ms per component with 10 bindings
2. **Event handling overhead**: Target <0.01ms per property change (pure delegate call)
3. **Memory footprint**: Target ~500 bytes per active binding (delegates + cached references)
4. **Stress test**: 100 components with 10 bindings each, all updating simultaneously

**Current design characteristics**:
- ✅ Expression compilation happens once during template creation (not in hot path)
- ✅ Event subscription uses reflection ONLY during activation (not per event)
- ✅ Event handlers are compiled delegates (equivalent to manual event subscription)
- ✅ Lookup delegates cached, executed once per activation

---

## Summary

All research areas resolved. **Key architectural decisions**:

1. **No Source Generators**: Use explicit setter delegates (`Set(SetText)`), template `IPropertyBinding[]` arrays
2. **Simple Lookups**: Static helper methods + `Func<>` delegates, default to FindParent
3. **Event Subscription**: Expression<> for type safety, EventInfo for subscription, compiled delegates for handlers
4. **Type Transformation**: Compiled property getters + converter delegates (no IValueConverter interface needed)
5. **Fluent API**: Mutable for efficiency, new instance only when generics change
6. **Default Behavior**: FindParent implicit, PropertyChanged event implicit, `.Set()` required (type-enforced)
7. **Lifecycle**: Template → Load → Activate → Deactivate with explicit cleanup
8. **Recursion Prevention**: Boolean `_isUpdating` guard flag
9. **Error Handling**: Fail fast on config errors, log and skip runtime failures

**Classes to Remove**:
- ❌ PropertyBindingsGenerator.cs and all generated PropertyBindings classes
- ❌ ILookupStrategy and concrete lookup strategy classes (use static helpers)
- ❓ IPropertyBindingDefinition (assess if needed)
- ❓ IValueConverter pattern (assess if `Func<>` suffices)

**Ready to proceed** to Phase 1 (Design & Contracts) with simplified architecture.
