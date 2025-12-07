# Interface Contract: IComponent (Unified)

**Feature**: Component Base Class Consolidation  
**Date**: December 6, 2025  
**Status**: NEW - Replaces old `IComponent` (renamed to `IComponentHierarchy`)

## Purpose

Provides a unified interface combining all component capabilities: identity, configuration, validation, hierarchy, activation, and update lifecycle. This is the primary interface for working with components when all capabilities are needed.

## Definition

```csharp
namespace Nexus.GameEngine.Components;

/// <summary>
/// Unified interface representing all component capabilities.
/// Combines identity, configuration, validation, hierarchy, activation, and update concerns.
/// Components can be resolved as this unified interface or any constituent interface.
/// </summary>
public interface IComponent 
    : IEntity,              // Identity (Id, Name, ApplyUpdates)
      ILoadable,            // Configuration (Load, IsLoaded, events)
      IValidatable,         // Validation (Validate, IsValid, ValidationErrors)
      IComponentHierarchy,  // Hierarchy (Parent, Children, navigation)
      IActivatable,         // Activation lifecycle (Activate, Deactivate, IsActive)
      IUpdatable            // Update lifecycle (Update)
{
    // Composition only - no additional members
    // All functionality inherited from constituent interfaces
}
```

## Constituent Interfaces

### IEntity
- `ComponentId Id { get; set; }`
- `string Name { get; }`
- `void SetName(string value, InterpolationFunction<string>? interpolator = null)`
- `void SetCurrentName(string value)`
- `void ApplyUpdates(double deltaTime)`

### ILoadable
- `bool IsLoaded { get; set; }`
- `void Load(Template template)`
- `event EventHandler<ConfigurationEventArgs>? Loading`
- `event EventHandler<ConfigurationEventArgs>? Loaded`

### IValidatable
- `bool Validate()`
- `bool IsValid()`
- `IEnumerable<ValidationError> ValidationErrors { get; }`
- `event EventHandler<EventArgs>? Validating`
- `event EventHandler<EventArgs>? Validated`
- `event EventHandler<EventArgs>? ValidationFailed`

### IComponentHierarchy
- `IContentManager? ContentManager { get; set; }`
- `IComponent? Parent { get; set; }`
- `IEnumerable<IComponent> Children { get; }`
- `void AddChild(IComponent child)`
- `void RemoveChild(IComponent child)`
- `IComponent? CreateChild(Type componentType)`
- `IComponent? CreateChild(Template template)`
- `IEnumerable<T> GetChildren<T>(Func<T, bool>? filter = null, bool recursive = false, bool depthFirst = false) where T : IComponent`
- `T? GetParent<T>(Func<T, bool>? filter = null) where T : IComponent`
- `IComponent GetRoot()`
- `event EventHandler<ChildCollectionChangedEventArgs>? ChildCollectionChanged`
- `event EventHandler<EventArgs>? Unloading`
- `event EventHandler<EventArgs>? Unloaded`

### IActivatable
- `bool IsActive()`
- `void Activate()`
- `void ActivateChildren()`
- `void ActivateChildren<TChild>() where TChild : IActivatable`
- `void Deactivate()`
- `void DeactivateChildren()`
- `event EventHandler<EventArgs>? Activating`
- `event EventHandler<EventArgs>? Activated`
- `event EventHandler<EventArgs>? Deactivating`
- `event EventHandler<EventArgs>? Deactivated`

### IUpdatable
- `void Update(double deltaTime)`
- `void UpdateChildren(double deltaTime)`
- `event EventHandler<EventArgs>? Updating`
- `event EventHandler<EventArgs>? Updated`

## Usage Patterns

### General Component Handling

```csharp
// ContentManager returns IComponent from factory
public class ContentManager : IContentManager
{
    public IComponent? CreateInstance(Template template)
    {
        var component = _factory.Create(template.ComponentType);
        
        // All lifecycle phases available
        component.Load(template);      // ILoadable
        component.Validate();          // IValidatable
        component.Activate();          // IActivatable
        
        return component;
    }
}
```

### Casting to Constituent Interfaces

```csharp
IComponent component = GetComponent();

// Cast to specific interface for granular operations
if (component is IActivatable activatable)
{
    activatable.Activate();
}

if (component is IUpdatable updatable)
{
    updatable.Update(deltaTime);
}
```

### Generic Constraints

```csharp
// Require full component capabilities
public T CreateAndActivate<T>(Template template) 
    where T : IComponent
{
    var component = ContentManager.CreateInstance(template) as T;
    component?.Load(template);
    component?.Activate();
    return component;
}
```

## Migration from Old Interfaces

### Old IComponent → New IComponentHierarchy

```csharp
// BEFORE: Using old IComponent for hierarchy operations
IComponent parent = GetParentComponent();
foreach (IComponent child in parent.Children)
{
    parent.RemoveChild(child);
}

// AFTER: Using renamed IComponentHierarchy
IComponentHierarchy parent = GetParentComponent();
foreach (IComponent child in parent.Children)
{
    parent.RemoveChild(child);
}
```

### Old IRuntimeComponent → New IComponent

```csharp
// BEFORE: Using old IRuntimeComponent
IRuntimeComponent component = GetRuntimeComponent();
component.Activate();
component.Update(deltaTime);

// AFTER: Using new unified IComponent
IComponent component = GetComponent();
component.Activate();  // IActivatable
component.Update(deltaTime);  // IUpdatable
```

### Granular Interface Usage

```csharp
// BEFORE: Forced to depend on full IRuntimeComponent
public class ActivationManager
{
    public ActivationManager(IEnumerable<IRuntimeComponent> components)
    {
        // Only needs activation, but gets update lifecycle too
    }
}

// AFTER: Depend only on what's needed
public class ActivationManager
{
    public ActivationManager(IEnumerable<IActivatable> activatables)
    {
        // Clear dependency: only activation lifecycle
    }
}
```

## Design Decisions

### Why Interface Composition?

**Zero Overhead**: Interface inheritance has no runtime cost - compiler merges all members at compile time.

**Flexibility**: Code can depend on unified interface or constituent interfaces based on needs:
- Full component handling → `IComponent`
- Only activation → `IActivatable`
- Only updates → `IUpdatable`
- Only hierarchy → `IComponentHierarchy`

**Interface Segregation**: Maintains ISP compliance while providing convenience unified interface.

### Why No Additional Members?

**Composition Pattern**: Unified interface is pure composition - all functionality comes from constituent interfaces.

**Single Responsibility**: Each constituent interface has clear, focused responsibility.

**No Duplication**: Members defined once in constituent interfaces, inherited by unified interface.

## Validation Rules

1. **Implementation Requirement**: Any class implementing `IComponent` must implement ALL constituent interfaces
2. **Behavioral Contract**: Implementations must honor lifecycle flow defined in data-model.md
3. **Activation Prerequisite**: Component must `IsValid() && IsLoaded` before activation allowed
4. **Parent Constraint**: Child activation requires `parent.IsActive() == true`

## Testing Contract

### Unit Tests
- Verify `Component` class implements `IComponent`
- Verify all constituent interface members accessible through `IComponent`
- Verify casting to constituent interfaces succeeds

### Integration Tests
- Verify lifecycle flow through unified interface
- Verify granular interface usage in DI scenarios
- Verify behavioral equivalence to old interface structure

## References

- **Data Model**: See `data-model.md` for complete member definitions
- **Research**: See `research.md` Question 2 for interface consolidation rationale
- **Spec**: See `spec.md` FR-006 for unified interface requirement
