# Data Model: Component Base Class Consolidation

**Feature**: Component Base Class Consolidation  
**Branch**: `014-component-consolidation`  
**Date**: December 6, 2025

## Overview

This document defines the data model for the consolidated component architecture. The consolidation transforms a 4-level inheritance hierarchy into a single `Component` class organized via partial classes, with interface reorganization to improve naming clarity and separation of concerns.

---

## Class Structure

### Component (Consolidated)

**File Organization**: Partial classes with dot-separated naming in same directory

```csharp
// All partial files in: src/GameEngine/Components/

Component.Identity.cs         // Primary partial with all interface declarations
Component.Configuration.cs    // Configuration and validation functionality
Component.Hierarchy.cs        // Parent-child relationship management
Component.Lifecycle.cs        // Activation and update lifecycle
```

**Full Class Declaration**:

```csharp
// Component.Identity.cs - PRIMARY PARTIAL
namespace Nexus.GameEngine.Components;

/// <summary>
/// Unified component class consolidating Entity, Configurable, Component, and RuntimeComponent.
/// Organized into partial classes for logical separation of concerns.
/// </summary>
public partial class Component 
    : IComponent,             // NEW: Unified interface
      IEntity,                // Identity functionality
      ILoadable,              // Configuration loading
      IValidatable,           // Validation
      IComponentHierarchy,    // RENAMED from IComponent: Parent-child relationships
      IActivatable,           // NEW: Split from IRuntimeComponent - Activation lifecycle
      IUpdatable              // NEW: Split from IRuntimeComponent - Update lifecycle
{
    // Identity functionality
}
```

---

## Partial Class Breakdown

### Component.Identity (from Entity)

**Responsibility**: Unique identification and deferred property updates

**Members**:

| Member | Type | Description | Source |
|--------|------|-------------|--------|
| `Id` | `ComponentId` | Unique identifier for component instance | Entity |
| `Name` | `string` | Human-readable name (deferred updates) | Entity |
| `SetName` | `void SetName(string, InterpolationFunction<string>?)` | Deferred name update with optional interpolation | Entity |
| `SetCurrentName` | `void SetCurrentName(string)` | Immediate name update bypassing deferred system | Entity |
| `ApplyUpdates` | `virtual void ApplyUpdates(double)` | Applies deferred property updates (called every frame) | Entity |
| `OnNameChanged` | `partial void OnNameChanged(string)` | Hook for name change notifications | Entity |

**Implements**: `IEntity` interface

**State Diagram**:
```
[Created] ──SetName──> [Name Pending]
[Name Pending] ──ApplyUpdates──> [Name Applied]
[Any State] ──SetCurrentName──> [Name Applied] (immediate)
```

---

### Component.Configuration (from Configurable)

**Responsibility**: Template-based configuration and validation

**Members**:

| Member | Type | Description | Source |
|--------|------|-------------|--------|
| `IsLoaded` | `bool` | Whether component has been loaded from template | Configurable |
| `Load` | `void Load(Template)` | Configure component from template | Configurable |
| `Configure` | `virtual void Configure(Template)` | Apply template properties (extensible) | Configurable |
| `OnLoad` | `protected virtual void OnLoad(Template?)` | Override point for component-specific configuration | Configurable |
| `Validate` | `bool Validate()` | Validates component and updates cache | Configurable |
| `IsValid` | `bool IsValid()` | Checks cached validation state (validates if needed) | Configurable |
| `OnValidate` | `protected virtual IEnumerable<ValidationError> OnValidate()` | Override point for component-specific validation | Configurable |
| `ValidationErrors` | `IEnumerable<ValidationError>` | Current validation errors | Configurable |
| `ClearValidationResults` | `protected void ClearValidationResults()` | Invalidates validation cache | Configurable |
| `Loading` | `EventHandler<ConfigurationEventArgs>` | Fired before Load() processing | Configurable |
| `Loaded` | `EventHandler<ConfigurationEventArgs>` | Fired after Load() completes | Configurable |
| `Validating` | `EventHandler<EventArgs>` | Fired before validation | Configurable |
| `Validated` | `EventHandler<EventArgs>` | Fired after successful validation | Configurable |
| `ValidationFailed` | `EventHandler<EventArgs>` | Fired after validation failure | Configurable |

**Implements**: `ILoadable`, `IValidatable` interfaces

**State Diagram**:
```
[Created] ──Load(template)──> [Loaded]
[Loaded] ──Validate()──> [Valid] or [Invalid]
[Valid/Invalid] ──ClearValidationResults()──> [Needs Validation]
[Needs Validation] ──IsValid()──> [Validates] ──> [Valid] or [Invalid]
```

---

### Component.Hierarchy (from Component)

**Responsibility**: Parent-child relationship management

**Members**:

| Member | Type | Description | Source |
|--------|------|-------------|--------|
| `ContentManager` | `IContentManager?` | Manager for creating and managing subcomponents | Component |
| `Parent` | `virtual IComponent?` | Parent component in hierarchy | Component |
| `Children` | `virtual IEnumerable<IComponent>` | Child components | Component |
| `AddChild` | `virtual void AddChild(IComponent)` | Add child to component | Component |
| `RemoveChild` | `virtual void RemoveChild(IComponent)` | Remove child from component | Component |
| `CreateChild` | `virtual IComponent? CreateChild(Type)` | Create child from type | Component |
| `CreateChild` | `virtual IComponent? CreateChild(Template)` | Create child from template | Component |
| `GetChildren<T>` | `virtual IEnumerable<T> GetChildren<T>(Func<T, bool>?, bool, bool)` | Query children with filtering and traversal options | Component |
| `GetParent<T>` | `virtual T? GetParent<T>(Func<T, bool>?)` | Find parent matching predicate | Component |
| `GetRoot` | `virtual IComponent GetRoot()` | Get root component | Component |
| `ChildCollectionChanged` | `EventHandler<ChildCollectionChangedEventArgs>` | Fired when children added/removed | Component |
| `Unloading` | `EventHandler<EventArgs>` | Fired before unload | Component |
| `Unloaded` | `EventHandler<EventArgs>` | Fired after unload | Component |

**Implements**: `IComponentHierarchy` interface (renamed from `IComponent`)

**State Diagram**:
```
[Component] ──AddChild──> [Parent with Children]
[Parent] ──CreateChild(template)──> [Calls ContentManager.CreateInstance] ──> [AddChild]
[Parent] ──RemoveChild──> [Child removed, Parent cleared]
```

**Relationship Constraints**:
- **Parent Assignment**: Child's Parent is set when added via `AddChild`
- **Parent Clearing**: Child's Parent is cleared when removed via `RemoveChild`
- **ContentManager Usage**: All child creation goes through `ContentManager` for lifecycle management
- **Non-Tree Structures**: Property bindings may create non-tree relationships (hence name `IComponentHierarchy` not `IComponentTree`)

---

### Component.Lifecycle (from RuntimeComponent)

**Responsibility**: Activation, deactivation, and frame-by-frame updates

**Members**:

| Member | Type | Description | Source |
|--------|------|-------------|--------|
| `Active` | `bool` | Whether component is currently active (deferred property) | RuntimeComponent |
| `SetActive` | `void SetActive(bool, InterpolationFunction<bool>?)` | Deferred active state update | RuntimeComponent (generated) |
| `IsActive` | `bool IsActive()` | Returns if component is valid, loaded, and active | RuntimeComponent |
| `Activate` | `void Activate()` | Activate component and children | RuntimeComponent |
| `OnActivate` | `protected virtual void OnActivate()` | Override point for activation logic | RuntimeComponent |
| `ActivateChildren` | `virtual void ActivateChildren()` | Activate all children | RuntimeComponent |
| `ActivateChildren<TChild>` | `virtual void ActivateChildren<TChild>()` | Activate children of specific type | RuntimeComponent |
| `Deactivate` | `void Deactivate()` | Deactivate component and children | RuntimeComponent |
| `OnDeactivate` | `protected virtual void OnDeactivate()` | Override point for deactivation logic | RuntimeComponent |
| `DeactivateChildren` | `virtual void DeactivateChildren()` | Deactivate all children | RuntimeComponent |
| `Update` | `virtual void Update(double)` | Frame update with deltaTime | RuntimeComponent |
| `OnUpdate` | `protected virtual void OnUpdate(double)` | Override point for update logic | RuntimeComponent |
| `UpdateChildren` | `virtual void UpdateChildren(double)` | Update all children | RuntimeComponent |
| `Activating` | `EventHandler<EventArgs>` | Fired before activation | RuntimeComponent |
| `Activated` | `EventHandler<EventArgs>` | Fired after activation | RuntimeComponent |
| `Deactivating` | `EventHandler<EventArgs>` | Fired before deactivation | RuntimeComponent |
| `Deactivated` | `EventHandler<EventArgs>` | Fired after deactivation | RuntimeComponent |
| `Updating` | `EventHandler<EventArgs>` | Fired before update | RuntimeComponent |
| `Updated` | `EventHandler<EventArgs>` | Fired after update | RuntimeComponent |

**Implements**: `IActivatable`, `IUpdatable` interfaces (split from `IRuntimeComponent`)

**State Diagram**:
```
[Loaded & Valid] ──Activate()──> [Active]
[Active] ──Update(deltaTime)──> [Active] (continuous)
[Active] ──Deactivate()──> [Inactive]
[Inactive] ──Activate()──> [Active]

Property Bindings Lifecycle:
[OnActivate] ──> [Bindings.Activate()] ──> [Subscriptions Created]
[OnDeactivate] ──> [Bindings.Deactivate()] ──> [Subscriptions Removed]
```

**Activation Rules**:
- Component must be `IsValid()` to activate
- Component must be `IsLoaded` to activate
- Parent must be `IsActive()` for child activation
- Activation cascades to children automatically
- Deactivation cascades to children automatically
- Updates cascade to children automatically

---

## Interface Structure

### IComponent (NEW - Unified Interface)

**Purpose**: Unified interface combining all component capabilities

**Definition**:
```csharp
namespace Nexus.GameEngine.Components;

/// <summary>
/// Unified interface representing all component capabilities.
/// Combines identity, configuration, validation, hierarchy, activation, and update concerns.
/// </summary>
public interface IComponent 
    : IEntity,              // Identity
      ILoadable,            // Configuration
      IValidatable,         // Validation
      IComponentHierarchy,  // Hierarchy
      IActivatable,         // Activation lifecycle
      IUpdatable            // Update lifecycle
{
    // Composition only - no additional members
}
```

**Usage Patterns**:
- **ContentManager**: Returns `IComponent` from factory methods
- **General Component Handling**: Accept `IComponent` when all capabilities needed
- **Casting**: Can cast to constituent interfaces for specific concerns

---

### IComponentHierarchy (RENAMED from IComponent)

**Purpose**: Parent-child relationship management (non-tree structures allowed)

**Rename Rationale**: 
- Property bindings create non-tree relationships (circular references possible)
- "Hierarchy" better reflects general parent-child relationships
- Distinguishes from new unified `IComponent` interface

**Members**: All hierarchy-related members from `Component.Hierarchy`

---

### IActivatable (NEW - Split from IRuntimeComponent)

**Purpose**: Activation and deactivation lifecycle phase

**Definition**:
```csharp
namespace Nexus.GameEngine.Components;

/// <summary>
/// Represents components that can be activated and deactivated.
/// Activation is the setup/initialization phase (property bindings, event subscriptions).
/// </summary>
public interface IActivatable
{
    bool IsActive();
    void Activate();
    void OnActivate();
    void ActivateChildren();
    void ActivateChildren<TChild>() where TChild : IActivatable;
    void Deactivate();
    void OnDeactivate();
    void DeactivateChildren();
    
    event EventHandler<EventArgs>? Activating;
    event EventHandler<EventArgs>? Activated;
    event EventHandler<EventArgs>? Deactivating;
    event EventHandler<EventArgs>? Deactivated;
}
```

**Usage Patterns**:
- Systems managing component lifecycle (activation order, dependency resolution)
- Property binding activation/deactivation
- Event subscription management

---

### IUpdatable (NEW - Split from IRuntimeComponent)

**Purpose**: Frame-by-frame update lifecycle phase

**Definition**:
```csharp
namespace Nexus.GameEngine.Components;

/// <summary>
/// Represents components that participate in frame-by-frame updates.
/// Updates are temporal changes driven by elapsed time (deltaTime).
/// </summary>
public interface IUpdatable
{
    void Update(double deltaTime);
    void OnUpdate(double deltaTime);
    void UpdateChildren(double deltaTime);
    
    event EventHandler<EventArgs>? Updating;
    event EventHandler<EventArgs>? Updated;
}
```

**Usage Patterns**:
- Rendering pipeline (update before render)
- Animation systems (interpolation updates)
- Game loop integration

**Separation Rationale**:
- Some components need activation but not updates (static UI elements)
- Some systems only care about updates (rendering)
- Clearer interface segregation (ISP compliance)

---

## Deprecated Classes (To Be Removed)

### Entity (REMOVED)
- **Consolidation Target**: `Component.Identity`
- **Migration**: All references change to `Component`
- **Interfaces Preserved**: `IEntity` (unchanged)

### Configurable (REMOVED)
- **Consolidation Target**: `Component.Configuration`
- **Migration**: All references change to `Component`
- **Interfaces Preserved**: `ILoadable`, `IValidatable` (unchanged)

### Component (REMOVED - name reused)
- **Consolidation Target**: `Component.Hierarchy`
- **Migration**: References to old `Component` class → new `Component` class
- **Interface Renamed**: `IComponent` → `IComponentHierarchy`

### RuntimeComponent (REMOVED)
- **Consolidation Target**: `Component.Lifecycle`
- **Migration**: All references change to `Component`
- **Interface Split**: `IRuntimeComponent` → `IActivatable` + `IUpdatable`
- **Unified Interface**: New `IComponent` combines both

---

## Validation Rules

### Component.Configuration Validation

**Rule**: Component is valid if `OnValidate()` returns no errors

**Validation Caching**:
- First call to `IsValid()` or `Validate()` performs validation
- Results cached in `_validationState` (bool?)
- `null` = not validated, `true` = valid, `false` = invalid
- `ClearValidationResults()` sets cache to `null`

**Validation Triggers**:
- Explicit `Validate()` call
- `IsValid()` when cache is `null`
- Activation attempts (validates before activating)

**Child Validation**:
- Parent validation does NOT validate children
- Children validated independently during their activation
- Invalid children are not activated (skipped silently)

---

## Lifecycle Flow

### Complete Component Lifecycle

```
1. Creation
   └─> [Component instantiated via DI]

2. Configuration
   └─> Load(template)
       ├─> Loading event
       ├─> Configure(template) - apply template properties
       ├─> OnLoad(template) - component-specific config
       ├─> ApplyUpdates(0) - apply deferred properties immediately
       └─> Loaded event

3. Validation
   └─> Validate() or IsValid()
       ├─> Validating event
       ├─> OnValidate() - component-specific validation
       ├─> Cache results
       └─> Validated or ValidationFailed event

4. Activation
   └─> Activate()
       ├─> Check parent.IsActive() (skip if false)
       ├─> Validate if needed (skip if invalid)
       ├─> Activating event
       ├─> OnActivate() - component-specific setup
       │   └─> Property bindings activated
       ├─> Set Active = true (immediate, not deferred)
       ├─> ActivateChildren() - recursive
       └─> Activated event

5. Runtime Loop
   └─> Update(deltaTime)
       ├─> Updating event
       ├─> ApplyUpdates(deltaTime) - deferred properties
       ├─> OnUpdate(deltaTime) - component-specific logic
       ├─> UpdateChildren(deltaTime) - recursive
       └─> Updated event

6. Deactivation
   └─> Deactivate()
       ├─> Deactivating event
       ├─> DeactivateChildren() - recursive
       ├─> OnDeactivate() - component-specific cleanup
       │   └─> Property bindings deactivated
       ├─> Set Active = false
       └─> Deactivated event

7. Disposal
   └─> Dispose()
       └─> [Resource cleanup via IDisposable]
```

---

## Source Generator Impact

### Generators Requiring Updates

1. **TemplateGenerator.cs**
   - **Change**: Update base class check from `RuntimeComponent` to `Component`
   - **Location**: Predicate function determining template generation targets
   - **Impact**: All component template records

2. **ComponentPropertyGenerator.cs**
   - **Change**: Update base class check from `Entity` to `Component`
   - **Location**: Predicate function for `[ComponentProperty]` targets
   - **Impact**: Deferred property implementations

3. **AnimatedPropertyGenerator.cs**
   - **Change**: Update base class check (if any) to `Component`
   - **Location**: Interpolation code generation targets
   - **Impact**: Type-specific interpolation methods

### Generated Code Changes

**No changes to generated code structure** - only target class name changes:

```csharp
// BEFORE (generated for class deriving from RuntimeComponent)
public void SetPosition(Vector2 value, InterpolationFunction<Vector2>? interpolator = null)
{
    _positionUpdater.Set(ref _position, value, interpolator);
}

// AFTER (generated for class deriving from Component) - IDENTICAL OUTPUT
public void SetPosition(Vector2 value, InterpolationFunction<Vector2>? interpolator = null)
{
    _positionUpdater.Set(ref _position, value, interpolator);
}
```

---

## Migration Impact Analysis

### Files Requiring Updates

**Category 1: Component Implementations** (~50 files)
- All classes inheriting from `RuntimeComponent` → inherit from `Component`
- No functionality changes, just base class reference

**Category 2: Interface References** (~30 files)
- Code using `IComponent` → use `IComponentHierarchy` (hierarchy operations)
- Code using `IRuntimeComponent` → use `IComponent` (unified) or `IActivatable + IUpdatable` (specific)
- Generic constraints: `where T : RuntimeComponent` → `where T : Component`

**Category 3: Type Checks** (~10 files)
- `is RuntimeComponent` → `is Component`
- `as RuntimeComponent` → `as Component`
- Type comparison: `GetType() == typeof(RuntimeComponent)` (rare, likely none)

**Category 4: Source Generators** (3 files)
- `TemplateGenerator.cs`
- `ComponentPropertyGenerator.cs`
- `AnimatedPropertyGenerator.cs`

**Category 5: Tests** (~40 files)
- Unit tests for component base classes
- Integration tests verifying behavior unchanged
- Source generator tests

---

## Breaking Changes

### Accepted Breaking Changes

1. **Class Name Changes**
   - `Entity` → removed
   - `Configurable` → removed
   - `Component` → removed (name reused for consolidated class)
   - `RuntimeComponent` → removed
   
2. **Interface Renames**
   - `IComponent` → `IComponentHierarchy`
   - `IRuntimeComponent` → split into `IActivatable` + `IUpdatable`, unified in new `IComponent`

3. **No Backward Compatibility**
   - Old class names will not compile
   - Old interface names will not compile
   - Migration required for all dependent code

### Non-Breaking Changes

1. **Constituent Interfaces Unchanged**
   - `IEntity` - same
   - `ILoadable` - same
   - `IValidatable` - same

2. **Functionality Preserved**
   - All methods have identical signatures
   - All events have identical signatures
   - All properties have identical behavior
   - Lifecycle flow unchanged

3. **Source Generator Output**
   - Generated code structure identical
   - Only target class names change

---

## Summary

The component consolidation transforms a deep inheritance hierarchy into a flat, organized structure using partial classes. Interface reorganization improves naming clarity and enables better separation of concerns through `IActivatable` and `IUpdatable` splitting. All existing functionality is preserved with zero behavioral changes, achieved through systematic migration and compiler-assisted error discovery.

**Key Benefits**:
- ✅ Reduced complexity (4 classes → 1 class with 4 partials)
- ✅ Improved navigability (logical partial file organization)
- ✅ Better interface segregation (activation vs. update separation)
- ✅ Clearer naming (`IComponentHierarchy` vs. ambiguous `IComponent`)
- ✅ Maintained functionality (100% behavioral equivalence)
- ✅ Zero runtime overhead (compile-time only changes)
