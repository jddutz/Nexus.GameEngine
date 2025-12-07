# Quickstart: Component Base Class Consolidation

**Feature**: Component Base Class Consolidation  
**Branch**: `014-component-consolidation`  
**Date**: December 6, 2025

## Overview

This guide helps developers understand and migrate to the new consolidated component architecture. The consolidation transforms a 4-level inheritance hierarchy into a single `Component` class organized with partial classes, and reorganizes interfaces for better clarity and separation of concerns.

---

## What Changed?

### Before (Old Architecture)

```csharp
// 4-level inheritance hierarchy
Entity                  // Identity (Id, Name, ApplyUpdates)
  ↓
Configurable           // Configuration (Load, Validate)
  ↓
Component              // Hierarchy (Parent, Children)
  ↓
RuntimeComponent       // Lifecycle (Activate, Update)

// Old interfaces
IComponent             // Hierarchy operations
IRuntimeComponent      // Activation + Update combined
```

### After (New Architecture)

```csharp
// Single class with 4 partial files
Component
  ├─ Component.Identity.cs        // Identity functionality
  ├─ Component.Configuration.cs   // Configuration/validation
  ├─ Component.Hierarchy.cs       // Parent-child relationships
  └─ Component.Lifecycle.cs       // Activation/update lifecycle

// New interfaces
IComponent             // NEW: Unified interface combining all concerns
IComponentHierarchy    // RENAMED from IComponent
IActivatable           // NEW: Split from IRuntimeComponent
IUpdatable             // NEW: Split from IRuntimeComponent
```

---

## Quick Migration Guide

### 1. Update Component Class References

```csharp
// BEFORE: Inheriting from RuntimeComponent
public class MyComponent : RuntimeComponent
{
    // ...
}

// AFTER: Inheriting from Component
public class MyComponent : Component
{
    // Everything else stays the same!
}
```

### 2. Update Interface References

```csharp
// BEFORE: Old interface names
IComponent hierarchy = GetComponent();
IRuntimeComponent runtime = GetRuntimeComponent();

// AFTER: New interface names
IComponentHierarchy hierarchy = GetComponent();
IComponent component = GetComponent();  // Unified interface
```

### 3. Update Generic Constraints

```csharp
// BEFORE: Old base class constraints
public T Create<T>() where T : RuntimeComponent

// AFTER: New base class constraints
public T Create<T>() where T : Component
```

### 4. Update Source Generators (If Applicable)

```csharp
// BEFORE: Checking for RuntimeComponent
if (baseType.Name == "RuntimeComponent")

// AFTER: Checking for Component
if (baseType.Name == "Component")
```

---

## Creating a New Component

### Basic Component

```csharp
namespace MyGame.Components;

/// <summary>
/// Example component using new consolidated architecture.
/// </summary>
public class PlayerComponent : Component
{
    // Use [ComponentProperty] for deferred, interpolatable properties
    [ComponentProperty]
    private Vector2 _position;
    
    [ComponentProperty]
    private float _health = 100f;
    
    // Override lifecycle methods as needed
    protected override void OnLoad(Template? template)
    {
        base.OnLoad(template);
        // Component-specific configuration
    }
    
    protected override void OnActivate()
    {
        base.OnActivate();  // CRITICAL: Activates property bindings
        // Component-specific setup
        EventBus.Subscribe<DamageEvent>(OnDamage);
    }
    
    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);
        // Component-specific update logic
        ProcessInput(deltaTime);
    }
    
    protected override void OnDeactivate()
    {
        base.OnDeactivate();  // CRITICAL: Deactivates property bindings
        // Component-specific cleanup
        EventBus.Unsubscribe<DamageEvent>(OnDamage);
    }
    
    private void OnDamage(DamageEvent e)
    {
        SetHealth(Health - e.Amount, duration: 0.2, mode: EaseOut);
    }
}
```

---

## Working with Interfaces

### Unified Interface (IComponent)

Use when you need all component capabilities:

```csharp
public class ContentManager
{
    public IComponent CreateAndActivate(Template template)
    {
        var component = CreateInstance(template);  // Returns IComponent
        
        component.Load(template);      // ILoadable
        component.Validate();          // IValidatable
        component.Activate();          // IActivatable
        component.Update(0);           // IUpdatable
        
        return component;
    }
}
```

### Granular Interfaces

Use when you only need specific capabilities:

```csharp
// Only need activation lifecycle
public class ActivationSystem
{
    private readonly IEnumerable<IActivatable> _activatables;
    
    public void ActivateAll()
    {
        foreach (var activatable in _activatables)
            activatable.Activate();
    }
}

// Only need update lifecycle
public class RenderSystem
{
    private readonly IEnumerable<IUpdatable> _updatables;
    
    public void UpdateFrame(double deltaTime)
    {
        foreach (var updatable in _updatables)
            updatable.Update(deltaTime);
    }
}

// Only need hierarchy operations
public class HierarchyDebugger
{
    public void PrintTree(IComponentHierarchy root, int depth = 0)
    {
        Console.WriteLine($"{new string(' ', depth * 2)}{root.Name}");
        
        foreach (var child in root.Children.OfType<IComponentHierarchy>())
            PrintTree(child, depth + 1);
    }
}
```

---

## Common Patterns

### Static Component (No Updates)

Components that don't change per-frame should only implement `IActivatable`:

```csharp
public class StaticImageComponent : Component, IActivatable
{
    // Loads texture during activation
    protected override void OnActivate()
    {
        base.OnActivate();
        _texture = ResourceManager.Load(_imagePath);
    }
    
    // No IUpdatable - no OnUpdate needed
    // Rendering system won't call Update on this component
}
```

### Animated Component (Needs Updates)

Components that change per-frame implement both `IActivatable` and `IUpdatable`:

```csharp
public class AnimatedSpriteComponent : Component, IActivatable, IUpdatable
{
    // Loads frames during activation
    protected override void OnActivate()
    {
        base.OnActivate();
        _frames = ResourceManager.LoadFrames(_spritePath);
    }
    
    // Advances animation during updates
    protected override void OnUpdate(double deltaTime)
    {
        _animationTime += deltaTime;
        _currentFrame = (int)(_animationTime * _fps) % _frames.Length;
    }
}
```

### Creating Children

```csharp
public class UIContainerComponent : Component
{
    protected override void OnLoad(Template? template)
    {
        base.OnLoad(template);
        
        if (template is UIContainerTemplate containerTemplate)
        {
            // Create children from templates
            foreach (var childTemplate in containerTemplate.Subcomponents)
            {
                CreateChild(childTemplate);  // Uses ContentManager
            }
        }
    }
}
```

### Property Bindings

```csharp
// Template with property binding
var healthBarTemplate = new ProgressBarTemplate
{
    Bindings = new()
    {
        // Bind ProgressBar.Value to Player.Health
        ["Value"] = Binding.FromParent<PlayerComponent>(p => p.Health)
            .WithConverter(new NormalizeConverter(0, 100))  // 0-100 → 0-1
    }
};

// Property bindings activated/deactivated automatically
// in Component.OnActivate/OnDeactivate (base implementation)
```

---

## Lifecycle Flow

### Complete Component Lifecycle

```
1. Creation (via DI)
   └─> Component instantiated
   
2. Configuration
   └─> Load(template)
       └─> Configure from template
       └─> IsLoaded = true
       
3. Validation
   └─> Validate() or IsValid()
       └─> Component-specific validation
       └─> Cache results
       
4. Activation
   └─> Activate()
       └─> Check parent.IsActive()
       └─> Validate if needed
       └─> OnActivate() (bindings activated)
       └─> Active = true
       └─> ActivateChildren()
       
5. Runtime Loop
   └─> Update(deltaTime)
       └─> ApplyUpdates(deltaTime)  // Deferred properties
       └─> OnUpdate(deltaTime)      // Component logic
       └─> UpdateChildren(deltaTime)
       
6. Deactivation
   └─> Deactivate()
       └─> DeactivateChildren()
       └─> OnDeactivate() (bindings deactivated)
       └─> Active = false
       
7. Disposal
   └─> Dispose()
       └─> Resource cleanup
```

---

## Testing Your Component

### Unit Test Example

```csharp
public class MyComponentTests
{
    [Fact]
    public void Component_Activates_WhenValid()
    {
        // Arrange
        var component = new MyComponent();
        var template = new MyComponentTemplate();
        component.Load(template);
        
        // Act
        component.Activate();
        
        // Assert
        Assert.True(component.IsActive());
    }
    
    [Fact]
    public void Component_Updates_Position()
    {
        // Arrange
        var component = new MyComponent();
        component.SetPosition(new Vector2(0, 0));
        component.ApplyUpdates(0);  // Apply immediately
        
        // Act
        component.SetPosition(new Vector2(100, 100), duration: 1.0);
        component.ApplyUpdates(0.5);  // 50% of duration
        
        // Assert
        Assert.Equal(new Vector2(50, 50), component.Position);  // Halfway interpolated
    }
}
```

### Integration Test Example

```csharp
public class MyComponentIntegrationTest : IntegrationTestBase
{
    protected override void OnUpdate(double deltaTime)
    {
        // Arrange: Create component
        if (FrameCount == 0)
        {
            _component = ContentManager.CreateInstance(new MyComponentTemplate());
            _component.Activate();
        }
    }
    
    protected override void OnRender()
    {
        // Act: Component renders (implicit)
    }
    
    protected override void OnPostRender()
    {
        // Assert: Verify rendering results
        if (FrameCount == 5)
        {
            var pixel = PixelSampler.Sample(100, 100);
            Assert.Equal(ExpectedColor, pixel);
            TestComplete();
        }
    }
}
```

---

## Common Mistakes to Avoid

### ❌ Forgetting base.OnActivate() Call

```csharp
// WRONG: Property bindings won't work
protected override void OnActivate()
{
    // base.OnActivate() NOT called!
    EventBus.Subscribe<MyEvent>(OnMyEvent);
}

// CORRECT: Always call base.OnActivate()
protected override void OnActivate()
{
    base.OnActivate();  // Activates property bindings
    EventBus.Subscribe<MyEvent>(OnMyEvent);
}
```

### ❌ Using Old Interface Names

```csharp
// WRONG: Old interface names
IComponent hierarchy = GetComponent();        // Renamed!
IRuntimeComponent runtime = GetComponent();   // Split!

// CORRECT: New interface names
IComponentHierarchy hierarchy = GetComponent();  // Renamed from IComponent
IComponent component = GetComponent();           // Unified interface
IActivatable activatable = GetComponent();       // Split from IRuntimeComponent
IUpdatable updatable = GetComponent();           // Split from IRuntimeComponent
```

### ❌ Manual Child Activation

```csharp
// WRONG: Don't manually activate children
protected override void OnActivate()
{
    base.OnActivate();
    
    foreach (var child in Children)
        child.Activate();  // DUPLICATE - base already does this!
}

// CORRECT: Let base.Activate() handle children
protected override void OnActivate()
{
    base.OnActivate();
    // Children activated automatically
}
```

### ❌ Implementing IUpdatable for Static Components

```csharp
// WRONG: Static component shouldn't implement IUpdatable
public class StaticLabel : Component, IActivatable, IUpdatable
{
    protected override void OnUpdate(double deltaTime)
    {
        // Empty - wasted call every frame!
    }
}

// CORRECT: Only implement IActivatable
public class StaticLabel : Component, IActivatable
{
    // No IUpdatable - no wasted Update calls
}
```

---

## Finding Components in Codebase

### Search Patterns

```powershell
# Find all component implementations
rg "class \w+ : (Runtime)?Component" --type cs

# Find old interface references (need updating)
rg "IRuntimeComponent" --type cs
rg ": IComponent" --type cs  # May include new unified interface

# Find generic constraints (need updating)
rg "where T : RuntimeComponent" --type cs

# Find type checks (need updating)
rg "is RuntimeComponent" --type cs
rg "as RuntimeComponent" --type cs
```

---

## Performance Tips

### Use Granular Interfaces in DI

```csharp
// Better: Only depend on what you need
public class ActivationManager
{
    public ActivationManager(IEnumerable<IActivatable> activatables) { }
}

// Worse: Depend on unified interface when only using activation
public class ActivationManager
{
    public ActivationManager(IEnumerable<IComponent> components) { }
}
```

### Filter Update Collections

```csharp
// Better: Only updatable components in update loop
private readonly IEnumerable<IUpdatable> _updatables;

public void UpdateFrame(double deltaTime)
{
    foreach (var updatable in _updatables)
        updatable.Update(deltaTime);  // Every call does work
}

// Worse: All components in update loop (including static)
private readonly IEnumerable<IComponent> _components;

public void UpdateFrame(double deltaTime)
{
    foreach (var component in _components)
        component.Update(deltaTime);  // Many empty Update calls
}
```

---

## Next Steps

1. **Read Full Spec**: See `spec.md` for complete requirements and acceptance criteria
2. **Review Data Model**: See `data-model.md` for detailed class/interface structure
3. **Check Research**: See `research.md` for architectural decisions and alternatives
4. **Review Contracts**: See `contracts/` for detailed interface contracts
5. **Migration Tasks**: See `tasks.md` (when available) for step-by-step migration checklist

---

## Getting Help

- **Questions about lifecycle**: Review `data-model.md` lifecycle flow section
- **Questions about interfaces**: Review `contracts/` for detailed interface contracts
- **Questions about migration**: Review `research.md` Question 3 for migration strategy
- **Questions about partial classes**: Review `research.md` Question 1 and Question 5

---

## Summary

The component consolidation simplifies the architecture while preserving all functionality:

✅ **Simpler**: 1 class instead of 4-level hierarchy  
✅ **Organized**: Logical partial class organization  
✅ **Flexible**: Granular interfaces for specific needs  
✅ **Clear**: Better interface names reflecting purpose  
✅ **Performant**: Zero runtime overhead, compile-time only change  
✅ **Compatible**: All existing functionality preserved  

Welcome to the new consolidated component architecture!
