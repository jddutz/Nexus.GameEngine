# Interface Contract: IComponentHierarchy (Renamed)

**Feature**: Component Base Class Consolidation  
**Date**: December 6, 2025  
**Status**: RENAMED from `IComponent`

## Purpose

Represents parent-child relationship management capabilities. Renamed from `IComponent` to `IComponentHierarchy` to better reflect that components can form hierarchical structures beyond simple trees (e.g., property bindings creating non-tree relationships).

## Definition

```csharp
namespace Nexus.GameEngine.Components;

/// <summary>
/// Represents components capable of parent-child relationship management.
/// Named "Hierarchy" rather than "Tree" because property bindings and other features
/// can create non-tree structures (cycles, multiple parents via bindings).
/// </summary>
public interface IComponentHierarchy
{
    // Management
    IContentManager? ContentManager { get; set; }
    
    // Relationships
    IComponent? Parent { get; set; }
    IEnumerable<IComponent> Children { get; }
    
    // Modification
    void AddChild(IComponent child);
    void RemoveChild(IComponent child);
    IComponent? CreateChild(Type componentType);
    IComponent? CreateChild(Template template);
    
    // Navigation
    IEnumerable<T> GetChildren<T>(Func<T, bool>? filter = null, bool recursive = false, bool depthFirst = false) 
        where T : IComponent;
    T? GetParent<T>(Func<T, bool>? filter = null) 
        where T : IComponent;
    IComponent GetRoot();
    
    // Events
    event EventHandler<ChildCollectionChangedEventArgs>? ChildCollectionChanged;
    event EventHandler<EventArgs>? Unloading;
    event EventHandler<EventArgs>? Unloaded;
}
```

## Member Contracts

### Properties

#### ContentManager
```csharp
IContentManager? ContentManager { get; set; }
```

**Purpose**: Manager used to create and manage subcomponents

**Contract**:
- Set by dependency injection or parent during child creation
- Used by `CreateChild` methods to instantiate subcomponents
- May be `null` if component doesn't create children

**Example**:
```csharp
public IComponent? CreateChild(Template template)
{
    var child = ContentManager?.CreateInstance(template);
    if (child != null) AddChild(child);
    return child;
}
```

---

#### Parent
```csharp
IComponent? Parent { get; set; }
```

**Purpose**: Parent component in the hierarchy

**Contract**:
- `null` for root components
- Set automatically by `AddChild`
- Cleared automatically by `RemoveChild`
- May be set manually for advanced scenarios (use with caution)

**Relationship Rules**:
- Child's `Parent` set when added via `AddChild`
- Child's `Parent` cleared when removed via `RemoveChild`
- Circular references possible with property bindings (hence "Hierarchy" not "Tree")

**Example**:
```csharp
var parent = new Component();
var child = new Component();

parent.AddChild(child);
Debug.Assert(child.Parent == parent);  // Automatically set

parent.RemoveChild(child);
Debug.Assert(child.Parent == null);    // Automatically cleared
```

---

#### Children
```csharp
IEnumerable<IComponent> Children { get; }
```

**Purpose**: Collection of immediate child components

**Contract**:
- Read-only enumerable (modification via `AddChild`/`RemoveChild`)
- Returns only immediate children (not grandchildren)
- May be empty for leaf components

**Example**:
```csharp
foreach (var child in parent.Children)
{
    Console.WriteLine(child.Name);
}
```

---

### Modification Methods

#### AddChild
```csharp
void AddChild(IComponent child)
```

**Purpose**: Add a child component to this component's children

**Contract**:
- Sets `child.Parent = this`
- Adds child to `Children` collection
- Fires `ChildCollectionChanged` event with `Added` collection
- Idempotent: adding same child twice has no effect (no duplicate children)

**Example**:
```csharp
var parent = new Component();
var child = new Component();

parent.AddChild(child);
// child.Parent == parent
// parent.Children contains child
// ChildCollectionChanged fired with Added = [child]
```

---

#### RemoveChild
```csharp
void RemoveChild(IComponent child)
```

**Purpose**: Remove a child component from this component's children

**Contract**:
- Sets `child.Parent = null`
- Removes child from `Children` collection
- Fires `ChildCollectionChanged` event with `Removed` collection
- No-op if child not in collection (safe to call multiple times)

**Example**:
```csharp
parent.RemoveChild(child);
// child.Parent == null
// parent.Children does not contain child
// ChildCollectionChanged fired with Removed = [child]
```

---

#### CreateChild (Type)
```csharp
IComponent? CreateChild(Type componentType)
```

**Purpose**: Create a child component from a type

**Contract**:
- Uses `ContentManager.Create(componentType)` to instantiate
- Calls `AddChild` to establish parent-child relationship
- Returns created component or `null` if creation fails
- Does NOT call `Load` (component not yet configured)

**Example**:
```csharp
var child = parent.CreateChild(typeof(MyComponent));
// child is instance of MyComponent
// child.Parent == parent
// child is NOT loaded (template not applied)
```

---

#### CreateChild (Template)
```csharp
IComponent? CreateChild(Template template)
```

**Purpose**: Create a child component from a template

**Contract**:
- Uses `ContentManager.CreateInstance(template)` to instantiate
- ContentManager handles loading from template
- Calls `AddChild` to establish parent-child relationship
- Returns created component or `null` if creation fails

**Example**:
```csharp
var template = new MyComponentTemplate { Name = "Child" };
var child = parent.CreateChild(template);
// child is instance of MyComponent
// child.Parent == parent
// child IS loaded (template applied by ContentManager)
```

---

### Navigation Methods

#### GetChildren<T>
```csharp
IEnumerable<T> GetChildren<T>(
    Func<T, bool>? filter = null, 
    bool recursive = false, 
    bool depthFirst = false
) where T : IComponent
```

**Purpose**: Query children with optional filtering and traversal strategy

**Parameters**:
- `filter`: Predicate to filter results (null = all)
- `recursive`: If true, search entire subtree; if false, immediate children only
- `depthFirst`: If true, depth-first traversal; if false, breadth-first

**Contract**:
- Returns only children of type `T` (compile-time type safety)
- Non-recursive: returns immediate children matching filter
- Recursive: returns all descendants matching filter using specified traversal
- Empty enumerable if no matches

**Example**:
```csharp
// Get all immediate Button children
var buttons = parent.GetChildren<Button>();

// Get all Label descendants recursively (depth-first)
var labels = parent.GetChildren<Label>(recursive: true, depthFirst: true);

// Get enabled Checkbox children with filter
var enabledCheckboxes = parent.GetChildren<Checkbox>(
    filter: cb => cb.Enabled, 
    recursive: false
);
```

---

#### GetParent<T>
```csharp
T? GetParent<T>(Func<T, bool>? filter = null) where T : IComponent
```

**Purpose**: Find ancestor component of specified type matching optional predicate

**Contract**:
- Walks up `Parent` chain until match found or root reached
- Returns first matching ancestor or `null` if none found
- Filter applied only to ancestors of type `T`

**Example**:
```csharp
// Find nearest Panel ancestor
var panel = child.GetParent<Panel>();

// Find nearest enabled Container ancestor
var container = child.GetParent<Container>(c => c.Enabled);
```

---

#### GetRoot
```csharp
IComponent GetRoot()
```

**Purpose**: Get the root component of the hierarchy

**Contract**:
- Walks up `Parent` chain until `Parent == null`
- Returns topmost component in hierarchy
- Returns `this` if component has no parent (already root)

**Example**:
```csharp
var root = child.GetRoot();
// root.Parent == null
```

---

### Events

#### ChildCollectionChanged
```csharp
event EventHandler<ChildCollectionChangedEventArgs>? ChildCollectionChanged
```

**Purpose**: Fired when children are added or removed

**Contract**:
- Fired AFTER child added/removed
- `Added` collection populated by `AddChild`
- `Removed` collection populated by `RemoveChild`
- May contain multiple children if batch operations implemented

**Example**:
```csharp
parent.ChildCollectionChanged += (sender, e) =>
{
    foreach (var added in e.Added)
        Console.WriteLine($"Added: {added.Name}");
    
    foreach (var removed in e.Removed)
        Console.WriteLine($"Removed: {removed.Name}");
};
```

---

#### Unloading / Unloaded
```csharp
event EventHandler<EventArgs>? Unloading
event EventHandler<EventArgs>? Unloaded
```

**Purpose**: Fired before/after component unload (cleanup phase)

**Contract**:
- `Unloading` fired before cleanup logic
- `Unloaded` fired after cleanup complete
- Used for resource cleanup, event unsubscription

---

## Migration from IComponent

### Interface Rename

```csharp
// BEFORE: Old interface name
public interface IComponent
{
    IComponent? Parent { get; set; }
    IEnumerable<IComponent> Children { get; }
    // ...
}

// AFTER: Renamed to IComponentHierarchy
public interface IComponentHierarchy
{
    IComponent? Parent { get; set; }  // Return type changed to new IComponent (unified)
    IEnumerable<IComponent> Children { get; }  // Element type changed to new IComponent (unified)
    // ...
}
```

### Usage Updates

```csharp
// BEFORE: Using old IComponent
public void ProcessHierarchy(IComponent component)
{
    foreach (IComponent child in component.Children)
    {
        ProcessHierarchy(child);
    }
}

// AFTER: Using renamed IComponentHierarchy
public void ProcessHierarchy(IComponentHierarchy component)
{
    foreach (IComponent child in component.Children)  // IComponent is now unified interface
    {
        ProcessHierarchy(child);
    }
}
```

---

## Rationale for Rename

### Why "Hierarchy" instead of "Component"?

**Problem with Old Name**:
- "IComponent" is too generic - doesn't indicate parent-child capability
- Conflicts with new unified interface concept
- Ambiguous in contexts like "IComponent.Parent" (component of what?)

**Benefits of "Hierarchy"**:
- Clear intent: this interface is about parent-child relationships
- Distinguishes from unified `IComponent` interface
- Accurate: reflects non-tree structures (property bindings create cycles)

### Why "Hierarchy" instead of "Tree"?

**Non-Tree Structures**:
- Property bindings can create circular references
- Component may participate in multiple hierarchies via bindings
- "Tree" implies strict parent-child without cycles

**Example of Non-Tree Structure**:
```csharp
// Component A's property bound to Component B's property
// Creates bidirectional dependency beyond simple tree structure
var componentA = new Component();
var componentB = new Component();

componentA.Bindings = new()
{
    ["Value"] = Binding.TwoWay<Component>(c => c.SomeProperty)
};

// A observes B, B observes A - not a tree!
```

---

## Design Decisions

### Why Not Rename to IParentChild?

**Rejected**: "Parent-child" is implementation detail, "hierarchy" is conceptual abstraction.

### Why Keep Parent Settable?

**Rationale**: Allows advanced scenarios (reparenting, binding-based relationships) while maintaining safety through `AddChild`/`RemoveChild` for common cases.

### Why Return IComponent Instead of IComponentHierarchy?

**Rationale**: Children have ALL component capabilities (unified `IComponent`), not just hierarchy. Allows accessing activation, updates, etc. without casting.

---

## Testing Contract

### Unit Tests
- Verify `AddChild` sets `child.Parent` and fires event
- Verify `RemoveChild` clears `child.Parent` and fires event
- Verify `CreateChild` instantiates via ContentManager and calls AddChild
- Verify navigation methods traverse hierarchy correctly
- Verify idempotent behavior (add same child twice = no duplicates)

### Integration Tests
- Verify property binding relationships work with hierarchy
- Verify lifecycle propagation (activate parent → children activate)
- Verify disposal propagation (dispose parent → children dispose)

---

## References

- **Data Model**: See `data-model.md` Component.Hierarchy section
- **Research**: See `research.md` Question 2 for interface naming rationale
- **Spec**: See `spec.md` FR-004 for rename requirement
