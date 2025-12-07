# Migration Guide: Component Base Class Consolidation

**Feature**: Component Base Class Consolidation
**Branch**: `014-component-consolidation`
**Date**: December 6, 2025

## Overview

The component architecture has been consolidated from a 4-level inheritance hierarchy into a single `Component` class using partial classes. This is a **breaking change** that requires updates to all component definitions and usages.

## Breaking Changes

### 1. Inheritance Hierarchy Flattened

**Old Hierarchy**:
`Entity` -> `Configurable` -> `Component` -> `RuntimeComponent`

**New Hierarchy**:
`Component` (Single class, partial definitions)

**Action Required**:
Update all component classes to inherit from `Component` instead of `RuntimeComponent`, `Component`, `Configurable`, or `Entity`.

```csharp
// BEFORE
public class MyComponent : RuntimeComponent { }

// AFTER
public class MyComponent : Component { }
```

### 2. Interface Renaming and Splitting

**Old Interfaces**:
- `IComponent`: Handled hierarchy (Parent/Children)
- `IRuntimeComponent`: Handled lifecycle (Activate/Update)

**New Interfaces**:
- `IActivatable`: Split from `IRuntimeComponent` (Activation lifecycle)
- `IUpdatable`: Split from `IRuntimeComponent` (Per-frame updates)
- `IComponent`: **NEW** Unified interface combining all capabilities (hierarchy + lifecycle + configuration)

**Note**: `IComponentHierarchy` was planned but not implemented. The unified `IComponent` interface covers all functionality.

**Action Required**:
Update variable types and method signatures.

```csharp
// BEFORE
public void Process(IRuntimeComponent runtime) { }

// AFTER
public void Process(IActivatable activatable) { }
// OR (for update behavior)
public void Process(IUpdatable updatable) { }
// OR (if you need everything)
public void Process(IComponent component) { }
```

### 3. Property Binding System Refactored

**Major Change**: The property binding system has been completely refactored with generic type-safe bindings.

**Old API**:
```csharp
// Non-generic, string-based
var binding = new PropertyBinding(lookupStrategy)
    .GetPropertyValue("PropertyName");
```

**New API**:
```csharp
// Generic, type-safe with fluent API
var binding = Binding.FromParent<SourceType>()
    .GetPropertyValue(s => s.PropertyName)
    .AsFormattedString("Value: {0}")
    .TwoWay();
```

**Key Changes**:
- **Generic Types**: `PropertyBinding<TSource, TValue>` provides compile-time type safety
- **No Reflection**: Interface-based activation (`IPropertyBinding`) instead of reflection
- **Fluent API**: Chainable methods for transformations
- **Lambda Expressions**: Use `s => s.Property` instead of `nameof()` or strings

**Action Required**:
Update all property binding declarations in templates.

```csharp
// BEFORE
Bindings = new MyComponentPropertyBindings
{
    TargetProperty = Binding.FromParent<Source>(s => s.SourceProperty)
}

// AFTER - Same syntax! (Internal implementation changed, API preserved)
Bindings = new MyComponentPropertyBindings
{
    TargetProperty = Binding.FromParent<Source>()
        .GetPropertyValue(s => s.SourceProperty)
}
```

// AFTER
public void Process(IComponentHierarchy hierarchy, IActivatable activatable, IUpdatable updatable) { }
// OR (if you need everything)
public void Process(IComponent component) { }
```

### 3. Generic Constraints

**Action Required**:
Update generic constraints to use `Component` or specific interfaces.

```csharp
// BEFORE
public class System<T> where T : RuntimeComponent

// AFTER
public class System<T> where T : Component
// OR
public class System<T> where T : IActivatable
```

## Migration Steps

1. **Update Component Classes**: Change base class to `Component`.
2. **Update References**: Replace `IRuntimeComponent` with `IActivatable`, `IUpdatable`, or `IComponent`.
3. **Update IComponent Usage**: Rename old `IComponent` usages to `IComponentHierarchy` if they only use hierarchy features, or `IComponent` if they use the unified interface.
4. **Rebuild**: Fix compilation errors.

## Common Issues

### "RuntimeComponent not found"
Replace with `Component`.

### "IComponent does not contain definition for Parent"
The new `IComponent` **does** contain `Parent`. If you see this, you might be using an old reference or conflicting namespace. Ensure you are using `Nexus.GameEngine.Components`.

### "Ambiguous reference between IComponent and..."
If you have other `IComponent` interfaces (e.g., from other libraries), use the fully qualified name `Nexus.GameEngine.Components.IComponent` or alias it.
