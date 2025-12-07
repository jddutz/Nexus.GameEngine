# Interface Contract: IActivatable (New)

**Feature**: Component Base Class Consolidation  
**Date**: December 6, 2025  
**Status**: NEW - Split from `IRuntimeComponent`

## Purpose

Represents the activation/deactivation lifecycle phase of components. Activation is the setup/initialization phase where property bindings are established, event subscriptions are created, and resources are prepared for use. Separated from `IUpdatable` to allow systems to depend only on the lifecycle phase they need.

## Definition

```csharp
namespace Nexus.GameEngine.Components;

/// <summary>
/// Represents components that can be activated and deactivated.
/// Activation is the setup/initialization phase (property bindings, event subscriptions, resource preparation).
/// Deactivation is the teardown/cleanup phase (unbind properties, unsubscribe events, release resources).
/// </summary>
public interface IActivatable
{
    // State
    bool IsActive();
    
    // Lifecycle
    void Activate();
    void Deactivate();
    
    // Extensibility
    void OnActivate();      // Protected in implementation, but part of contract
    void OnDeactivate();    // Protected in implementation, but part of contract
    
    // Child Management
    void ActivateChildren();
    void ActivateChildren<TChild>() where TChild : IActivatable;
    void DeactivateChildren();
    
    // Events
    event EventHandler<EventArgs>? Activating;
    event EventHandler<EventArgs>? Activated;
    event EventHandler<EventArgs>? Deactivating;
    event EventHandler<EventArgs>? Deactivated;
}
```

## Member Contracts

### State Query

#### IsActive
```csharp
bool IsActive()
```

**Purpose**: Determine if component is currently active and ready for use

**Contract**:
- Returns `true` if component is valid, loaded, and active
- Returns `false` otherwise
- Implementation: `IsValid() && IsLoaded && Active` (where `Active` is the deferred property)

**Example**:
```csharp
if (component.IsActive())
{
    // Component is ready for use
    component.Update(deltaTime);
}
```

---

### Lifecycle Methods

#### Activate
```csharp
void Activate()
```

**Purpose**: Activate component and all children

**Contract**:
1. **Prerequisite Check**: Skip if parent not active (`parent.IsActive() == false`)
2. **Validation**: Call `Validate()` if needed, skip if invalid
3. **Pre-Event**: Fire `Activating` event
4. **Component Logic**: Call `OnActivate()` for component-specific setup
5. **State Change**: Set `Active = true` (immediately, not deferred)
6. **Child Activation**: Call `ActivateChildren()` to activate all children
7. **Post-Event**: Fire `Activated` event

**Activation Rules**:
- Component must be `IsValid()` to activate
- Component must be `IsLoaded` to activate
- Parent must be active (`parent.IsActive()`) for child activation
- Invalid components are skipped (not an error)

**Example**:
```csharp
component.Activate();
// Component is now active if all prerequisites met
Debug.Assert(component.IsActive());
```

---

#### Deactivate
```csharp
void Deactivate()
```

**Purpose**: Deactivate component and all children

**Contract**:
1. **Pre-Event**: Fire `Deactivating` event
2. **Child Deactivation**: Call `DeactivateChildren()` to deactivate all children first
3. **Component Logic**: Call `OnDeactivate()` for component-specific cleanup
4. **State Change**: Set `Active = false`
5. **Post-Event**: Fire `Deactivated` event

**Deactivation Order**: Children deactivated BEFORE parent (reverse of activation)

**Example**:
```csharp
component.Deactivate();
// Component and all children now inactive
Debug.Assert(!component.IsActive());
```

---

#### OnActivate
```csharp
void OnActivate()
```

**Purpose**: Override point for component-specific activation logic

**Contract**:
- Called during `Activate()` AFTER validation, BEFORE setting `Active = true`
- Base implementation activates property bindings
- Derived classes should call `base.OnActivate()` to preserve binding activation
- Used for: event subscriptions, resource initialization, binding activation

**Implementation Pattern**:
```csharp
protected override void OnActivate()
{
    base.OnActivate();  // Activates property bindings
    
    // Component-specific activation
    _eventBus.Subscribe<MyEvent>(OnMyEvent);
    _resource = ResourceManager.Load("myresource");
}
```

---

#### OnDeactivate
```csharp
void OnDeactivate()
```

**Purpose**: Override point for component-specific deactivation logic

**Contract**:
- Called during `Deactivate()` AFTER children deactivated, BEFORE setting `Active = false`
- Base implementation deactivates property bindings
- Derived classes should call `base.OnDeactivate()` to preserve binding deactivation
- Used for: event unsubscription, resource cleanup, binding deactivation

**Implementation Pattern**:
```csharp
protected override void OnDeactivate()
{
    base.OnDeactivate();  // Deactivates property bindings
    
    // Component-specific deactivation
    _eventBus.Unsubscribe<MyEvent>(OnMyEvent);
    ResourceManager.Unload(_resource);
    _resource = null;
}
```

---

### Child Management

#### ActivateChildren
```csharp
void ActivateChildren()
```

**Purpose**: Activate all immediate children

**Contract**:
- Only activates immediate children (not grandchildren)
- Each child's `Activate()` will handle its own children recursively
- Children of type `IActivatable` are activated
- Non-`IActivatable` children are skipped

**Example**:
```csharp
public override void ActivateChildren()
{
    foreach (var child in Children.OfType<IActivatable>())
    {
        child.Activate();  // Child handles its own children
    }
}
```

---

#### ActivateChildren<TChild>
```csharp
void ActivateChildren<TChild>() where TChild : IActivatable
```

**Purpose**: Activate only children of specific type

**Contract**:
- Only activates immediate children of type `TChild`
- Type must implement `IActivatable`
- Useful for selective activation scenarios

**Example**:
```csharp
// Activate only Button children
component.ActivateChildren<Button>();
```

---

#### DeactivateChildren
```csharp
void DeactivateChildren()
```

**Purpose**: Deactivate all immediate children

**Contract**:
- Only deactivates immediate children (not grandchildren)
- Each child's `Deactivate()` will handle its own children recursively
- Children of type `IActivatable` are deactivated
- Non-`IActivatable` children are skipped
- Called BEFORE parent deactivation (reverse order from activation)

**Example**:
```csharp
public override void DeactivateChildren()
{
    foreach (var child in Children.OfType<IActivatable>())
    {
        child.Deactivate();  // Child handles its own children
    }
}
```

---

### Events

#### Activating
```csharp
event EventHandler<EventArgs>? Activating
```

**Purpose**: Fired immediately before component activation logic

**Contract**:
- Fired BEFORE `OnActivate()` called
- Fired BEFORE `Active` set to `true`
- Fired BEFORE children activated
- Allows observers to prepare for activation

**Example**:
```csharp
component.Activating += (sender, e) =>
{
    Console.WriteLine("Component about to activate");
};
```

---

#### Activated
```csharp
event EventHandler<EventArgs>? Activated
```

**Purpose**: Fired immediately after component activation completes

**Contract**:
- Fired AFTER `OnActivate()` called
- Fired AFTER `Active` set to `true`
- Fired AFTER children activated
- Component fully active when event fires

**Example**:
```csharp
component.Activated += (sender, e) =>
{
    Debug.Assert(component.IsActive());
    Console.WriteLine("Component activated");
};
```

---

#### Deactivating
```csharp
event EventHandler<EventArgs>? Deactivating
```

**Purpose**: Fired immediately before component deactivation logic

**Contract**:
- Fired BEFORE children deactivated
- Fired BEFORE `OnDeactivate()` called
- Fired BEFORE `Active` set to `false`
- Component still active when event fires

**Example**:
```csharp
component.Deactivating += (sender, e) =>
{
    Debug.Assert(component.IsActive());
    Console.WriteLine("Component about to deactivate");
};
```

---

#### Deactivated
```csharp
event EventHandler<EventArgs>? Deactivated
```

**Purpose**: Fired immediately after component deactivation completes

**Contract**:
- Fired AFTER children deactivated
- Fired AFTER `OnDeactivate()` called
- Fired AFTER `Active` set to `false`
- Component fully inactive when event fires

**Example**:
```csharp
component.Deactivated += (sender, e) =>
{
    Debug.Assert(!component.IsActive());
    Console.WriteLine("Component deactivated");
};
```

---

## Lifecycle Flow

### Activation Sequence

```
Activate() called
│
├─> Check parent.IsActive() ──[false]──> SKIP (do nothing)
│                          └─[true]─┐
│                                   │
├─> Validate if needed ──[invalid]──> SKIP (do nothing)
│                     └─[valid]─┐
│                               │
├─> Fire Activating event       │
├─> Call OnActivate()           │
│   └─> Activate property bindings
│   └─> Component-specific setup
├─> Set Active = true (immediate)
├─> Call ActivateChildren()     │
│   └─> Each child.Activate() recursively
├─> Fire Activated event        │
└─> COMPLETE
```

### Deactivation Sequence

```
Deactivate() called
│
├─> Fire Deactivating event
├─> Call DeactivateChildren() (children FIRST)
│   └─> Each child.Deactivate() recursively
├─> Call OnDeactivate()
│   └─> Deactivate property bindings
│   └─> Component-specific cleanup
├─> Set Active = false
├─> Fire Deactivated event
└─> COMPLETE
```

---

## Property Binding Integration

### Activation Phase

```csharp
protected override void OnActivate()
{
    base.OnActivate();  // CRITICAL: Activates property bindings
    
    // Property bindings are now subscribed to source changes
    // Two-way bindings will propagate changes in both directions
}
```

**Property Binding Activation**:
- Subscriptions created to source properties
- Change notifications established
- Initial values synchronized (if configured)

### Deactivation Phase

```csharp
protected override void OnDeactivate()
{
    base.OnDeactivate();  // CRITICAL: Deactivates property bindings
    
    // Property bindings are now unsubscribed from source changes
    // Memory leak prevention: no lingering event handlers
}
```

**Property Binding Deactivation**:
- Subscriptions removed from source properties
- Change notifications disconnected
- Memory leaks prevented

---

## Separation from IUpdatable

### Why Separate Activation from Updates?

**Different Concerns**:
- **Activation**: Setup/teardown lifecycle (one-time operations)
- **Updates**: Continuous frame-by-frame processing (every frame)

**Different Dependencies**:
- **Activation Systems**: Dependency injection, lifecycle management
- **Update Systems**: Game loop, rendering pipeline

**Example Use Cases**:

```csharp
// System only needing activation management
public class ActivationManager
{
    private readonly IEnumerable<IActivatable> _activatables;
    
    public void ActivateAll()
    {
        foreach (var activatable in _activatables)
            activatable.Activate();
    }
}

// System only needing update management
public class RenderSystem
{
    private readonly IEnumerable<IUpdatable> _updatables;
    
    public void UpdateFrame(double deltaTime)
    {
        foreach (var updatable in _updatables)
            updatable.Update(deltaTime);
    }
}

// Static UI element: needs activation, NOT updates
public class StaticLabel : Component, IActivatable
{
    // Implements IActivatable for setup/teardown
    // Does NOT implement IUpdatable - no frame-by-frame changes
}

// Animated UI element: needs both
public class AnimatedSprite : Component, IActivatable, IUpdatable
{
    // Implements IActivatable for resource loading
    // Implements IUpdatable for animation frames
}
```

---

## Migration from IRuntimeComponent

### Old IRuntimeComponent Usage

```csharp
// BEFORE: Forced to depend on both activation and updates
public class ActivationManager
{
    public ActivationManager(IEnumerable<IRuntimeComponent> components)
    {
        // Gets both activation AND update methods, even if only using activation
    }
}
```

### New IActivatable Usage

```csharp
// AFTER: Depend only on activation lifecycle
public class ActivationManager
{
    public ActivationManager(IEnumerable<IActivatable> activatables)
    {
        // Clear dependency: only activation lifecycle
        // No update methods in interface
    }
}
```

### Unified Interface Still Available

```csharp
// Components implementing both: use unified IComponent
public class ContentManager
{
    public IComponent CreateAndActivate(Template template)
    {
        var component = CreateInstance(template);  // Returns IComponent
        component.Load(template);    // ILoadable
        component.Activate();        // IActivatable
        component.Update(0);         // IUpdatable
        return component;
    }
}
```

---

## Validation Rules

1. **Activation Prerequisite**: `IsValid() && IsLoaded` must be `true`
2. **Parent Constraint**: Parent must be active (`parent.IsActive()`) for child activation
3. **Idempotency**: Multiple `Activate()` calls have no additional effect (already active)
4. **Deactivation Safety**: `Deactivate()` can be called multiple times safely
5. **Event Order**: Events fire in documented order (Activating → OnActivate → Activated)

---

## Testing Contract

### Unit Tests
- Verify `Activate()` sets `Active = true` when prerequisites met
- Verify `Activate()` skips when parent not active
- Verify `Activate()` skips when component invalid
- Verify `Deactivate()` sets `Active = false`
- Verify child activation order (parent → children)
- Verify child deactivation order (children → parent)
- Verify property binding activation/deactivation
- Verify event firing order

### Integration Tests
- Verify activation cascades through hierarchy
- Verify deactivation cascades through hierarchy
- Verify memory leaks prevented (bindings deactivated)
- Verify activation with property bindings works correctly

---

## Design Decisions

### Why Include OnActivate/OnDeactivate in Interface?

**Rationale**: These are part of the contract - derived classes MUST call base implementation to ensure property bindings work. Interface makes this explicit.

### Why Immediate Active State Change?

**Rationale**: Child activation checks `parent.IsActive()` - must see parent as active immediately for validation to work.

### Why Children Last in Activation?

**Rationale**: Parent must be fully initialized before children can safely access parent state.

### Why Children First in Deactivation?

**Rationale**: Children may depend on parent state - deactivate children while parent still valid.

---

## References

- **Data Model**: See `data-model.md` Component.Lifecycle section
- **Research**: See `research.md` Question 2 for interface splitting rationale
- **Spec**: See `spec.md` FR-005 for `IActivatable` creation requirement
- **Property Bindings**: See `.docs/Deferred Property Generation System.md` for binding lifecycle
