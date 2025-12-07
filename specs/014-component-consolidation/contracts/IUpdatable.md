# Interface Contract: IUpdatable (New)

**Feature**: Component Base Class Consolidation  
**Date**: December 6, 2025  
**Status**: NEW - Split from `IRuntimeComponent`

## Purpose

Represents the frame-by-frame update lifecycle phase of components. Updates are temporal changes driven by elapsed time (deltaTime), such as animations, physics, input processing, and game logic. Separated from `IActivatable` to allow systems to depend only on the update lifecycle without requiring activation capabilities.

## Definition

```csharp
namespace Nexus.GameEngine.Components;

/// <summary>
/// Represents components that participate in frame-by-frame updates.
/// Updates are temporal changes driven by elapsed time (deltaTime).
/// Typical uses: animations, physics, input processing, deferred property interpolation.
/// </summary>
public interface IUpdatable
{
    // Lifecycle
    void Update(double deltaTime);
    
    // Extensibility
    void OnUpdate(double deltaTime);    // Protected in implementation, but part of contract
    
    // Child Management
    void UpdateChildren(double deltaTime);
    
    // Events
    event EventHandler<EventArgs>? Updating;
    event EventHandler<EventArgs>? Updated;
}
```

## Member Contracts

### Lifecycle Methods

#### Update
```csharp
void Update(double deltaTime)
```

**Purpose**: Update component and all children for current frame

**Parameters**:
- `deltaTime`: Time elapsed since last frame in seconds (e.g., `0.016` for 60 FPS)

**Contract**:
1. **Pre-Event**: Fire `Updating` event
2. **Deferred Properties**: Call `ApplyUpdates(deltaTime)` to interpolate deferred properties
3. **Component Logic**: Call `OnUpdate(deltaTime)` for component-specific update logic
4. **Child Updates**: Call `UpdateChildren(deltaTime)` to update all children
5. **Post-Event**: Fire `Updated` event

**Update Order**: Parent → Children (same as activation)

**Active State Check**: Implementation may skip updates if `!IsActive()`, but interface does not enforce this

**Example**:
```csharp
// Game loop calls Update every frame
while (running)
{
    double deltaTime = timer.Elapsed.TotalSeconds;
    rootComponent.Update(deltaTime);
    Render();
}
```

---

#### OnUpdate
```csharp
void OnUpdate(double deltaTime)
```

**Purpose**: Override point for component-specific update logic

**Parameters**:
- `deltaTime`: Time elapsed since last frame in seconds

**Contract**:
- Called during `Update()` AFTER `ApplyUpdates`, BEFORE `UpdateChildren`
- Base implementation is usually empty (no default update logic)
- Derived classes implement component-specific logic (animations, physics, input)
- Optional to call `base.OnUpdate(deltaTime)` if base class has no logic

**Implementation Pattern**:
```csharp
protected override void OnUpdate(double deltaTime)
{
    // Component-specific update logic
    _animationTime += deltaTime;
    _currentFrame = (int)(_animationTime * _framesPerSecond) % _totalFrames;
    
    // Update computed properties
    Transform = ComputeTransformForFrame(_currentFrame);
}
```

---

#### UpdateChildren
```csharp
void UpdateChildren(double deltaTime)
```

**Purpose**: Update all immediate children

**Parameters**:
- `deltaTime`: Time elapsed since last frame in seconds

**Contract**:
- Only updates immediate children (not grandchildren)
- Each child's `Update()` will handle its own children recursively
- Children of type `IUpdatable` are updated
- Non-`IUpdatable` children are skipped
- Called AFTER parent's `OnUpdate` (parent updates first)

**Example**:
```csharp
public virtual void UpdateChildren(double deltaTime)
{
    foreach (var child in Children.OfType<IUpdatable>())
    {
        child.Update(deltaTime);  // Child handles its own children
    }
}
```

---

### Events

#### Updating
```csharp
event EventHandler<EventArgs>? Updating
```

**Purpose**: Fired immediately before component update logic

**Contract**:
- Fired BEFORE `ApplyUpdates()` called
- Fired BEFORE `OnUpdate()` called
- Fired BEFORE children updated
- Allows observers to prepare for update

**Example**:
```csharp
component.Updating += (sender, e) =>
{
    // Prepare for update (e.g., cache state for comparison)
    _previousPosition = component.Position;
};
```

---

#### Updated
```csharp
event EventHandler<EventArgs>? Updated
```

**Purpose**: Fired immediately after component update completes

**Contract**:
- Fired AFTER `ApplyUpdates()` called
- Fired AFTER `OnUpdate()` called
- Fired AFTER children updated
- Component fully updated when event fires

**Example**:
```csharp
component.Updated += (sender, e) =>
{
    // React to update (e.g., check if position changed)
    if (component.Position != _previousPosition)
        Console.WriteLine("Component moved");
};
```

---

## Update Flow

### Update Sequence

```
Update(deltaTime) called
│
├─> Fire Updating event
├─> Call ApplyUpdates(deltaTime)
│   └─> Interpolate all deferred properties
│       └─> Position: oldValue → newValue (based on deltaTime)
│       └─> Color: oldColor → newColor (based on deltaTime)
│       └─> ...
├─> Call OnUpdate(deltaTime)
│   └─> Component-specific update logic
│       └─> Physics calculations
│       └─> Animation frame updates
│       └─> Input processing
├─> Call UpdateChildren(deltaTime)
│   └─> Each child.Update(deltaTime) recursively
├─> Fire Updated event
└─> COMPLETE
```

---

## Deferred Property Integration

### ApplyUpdates in Update Flow

```csharp
public override void Update(double deltaTime)
{
    Updating?.Invoke(this, EventArgs.Empty);
    
    // CRITICAL: Apply deferred property updates
    ApplyUpdates(deltaTime);
    
    OnUpdate(deltaTime);
    UpdateChildren(deltaTime);
    
    Updated?.Invoke(this, EventArgs.Empty);
}
```

**Deferred Property Interpolation**:

```csharp
// User queues property update with interpolation
component.SetPosition(new Vector2(100, 200), duration: 1.0, mode: Linear);

// Each Update applies interpolation based on deltaTime
// Frame 1 (deltaTime = 0.016): Position interpolated ~1.6% toward target
// Frame 2 (deltaTime = 0.016): Position interpolated ~3.2% toward target
// ...
// Frame 60 (total time = 1.0): Position reaches target (100, 200)
```

**Source-Generated Properties**:
- `[ComponentProperty]` attributes generate `SetXxx` methods
- `ApplyUpdates` calls generated `_xxxUpdater.Apply(ref _xxx, deltaTime)`
- Type-specific interpolation (vectors, colors, floats, etc.)
- Zero overhead (compile-time code generation)

---

## Separation from IActivatable

### Why Separate Updates from Activation?

**Different Frequencies**:
- **Activation**: One-time or rare (component lifecycle changes)
- **Updates**: Continuous every frame (60+ times per second)

**Different Performance Characteristics**:
- **Activation**: Can be expensive (resource loading, setup)
- **Updates**: Must be fast (tight loop, performance-critical)

**Different Dependencies**:
- **Activation**: May depend on many systems (DI, resources, validation)
- **Updates**: Usually depends on time and internal state

**Example Use Cases**:

```csharp
// Rendering system: only cares about updates
public class Renderer
{
    private readonly IEnumerable<IUpdatable> _updatables;
    
    public void RenderFrame(double deltaTime)
    {
        // Update all components before rendering
        foreach (var updatable in _updatables)
            updatable.Update(deltaTime);
        
        // Render scene
        DrawScene();
    }
}

// Static UI element: needs activation, NOT updates
public class StaticImage : Component, IActivatable
{
    // Loads texture during activation
    // No per-frame updates needed - image never changes
    
    protected override void OnActivate()
    {
        base.OnActivate();
        _texture = ResourceManager.Load(_imagePath);
    }
}

// Animated UI element: needs both activation and updates
public class AnimatedGif : Component, IActivatable, IUpdatable
{
    // Loads frames during activation
    protected override void OnActivate()
    {
        base.OnActivate();
        _frames = ResourceManager.LoadFrames(_gifPath);
    }
    
    // Advances animation during updates
    protected override void OnUpdate(double deltaTime)
    {
        _currentTime += deltaTime;
        _currentFrame = (int)(_currentTime * _fps) % _frames.Length;
    }
}
```

---

## Migration from IRuntimeComponent

### Old IRuntimeComponent Usage

```csharp
// BEFORE: Forced to implement both activation and updates
public class StaticLabel : RuntimeComponent
{
    // Must implement Update even though label never changes
    protected override void OnUpdate(double deltaTime)
    {
        // Empty - wasted call every frame
    }
}
```

### New IUpdatable Usage

```csharp
// AFTER: Only implement IUpdatable if component needs updates
public class StaticLabel : Component, IActivatable
{
    // No IUpdatable - no wasted Update calls
    // Rendering system won't even see this component in update loop
}

public class AnimatedLabel : Component, IActivatable, IUpdatable
{
    // Implements IUpdatable because animation needs per-frame updates
    protected override void OnUpdate(double deltaTime)
    {
        AdvanceAnimation(deltaTime);
    }
}
```

### Unified Interface Still Available

```csharp
// Components implementing both: use unified IComponent
public class ContentManager
{
    public void RunComponentLifecycle(IComponent component, double deltaTime)
    {
        component.Activate();        // IActivatable
        component.Update(deltaTime); // IUpdatable
    }
}
```

---

## Performance Considerations

### Update Loop Optimization

```csharp
// BEFORE: All RuntimeComponents in update loop, even static ones
public class UpdateSystem
{
    private readonly IEnumerable<IRuntimeComponent> _components;
    
    public void UpdateFrame(double deltaTime)
    {
        foreach (var component in _components)
            component.Update(deltaTime);  // Many empty Update calls
    }
}

// AFTER: Only components needing updates in loop
public class UpdateSystem
{
    private readonly IEnumerable<IUpdatable> _updatables;
    
    public void UpdateFrame(double deltaTime)
    {
        foreach (var updatable in _updatables)
            updatable.Update(deltaTime);  // Every call does real work
    }
}
```

### Selective Child Updates

```csharp
// Update only specific child types for performance
public override void OnUpdate(double deltaTime)
{
    // Update only animated children, skip static ones
    foreach (var child in Children.OfType<IAnimated>())
    {
        child.Update(deltaTime);
    }
}
```

---

## Validation Rules

1. **Update Frequency**: `Update` called every frame (typically 60 FPS = ~0.016s deltaTime)
2. **DeltaTime Units**: Always in seconds (not milliseconds)
3. **DeltaTime Range**: Typically `0.001` to `0.1` seconds (games may clamp to prevent large jumps)
4. **Event Order**: Events fire in documented order (Updating → ApplyUpdates → OnUpdate → UpdateChildren → Updated)
5. **Recursion**: Each component updates its children - no manual grandchild updates needed

---

## Testing Contract

### Unit Tests
- Verify `Update()` calls `ApplyUpdates(deltaTime)`
- Verify `Update()` calls `OnUpdate(deltaTime)`
- Verify `Update()` calls `UpdateChildren(deltaTime)`
- Verify child update order (parent → children)
- Verify event firing order
- Verify deferred property interpolation progresses with deltaTime

### Integration Tests
- Verify update cascades through hierarchy
- Verify animations complete after expected time
- Verify deltaTime accumulation accuracy
- Verify performance (update loop overhead)

---

## Common Update Patterns

### Animation

```csharp
protected override void OnUpdate(double deltaTime)
{
    _animationTime += deltaTime;
    
    if (_animationTime >= _duration)
    {
        _animationTime = 0;  // Loop
        OnAnimationComplete?.Invoke(this, EventArgs.Empty);
    }
    
    float t = _animationTime / _duration;
    Transform = InterpolateTransform(_startTransform, _endTransform, t);
}
```

### Physics

```csharp
protected override void OnUpdate(double deltaTime)
{
    // Apply gravity
    _velocity += Gravity * deltaTime;
    
    // Apply velocity
    var newPosition = Position + _velocity * deltaTime;
    SetPosition(newPosition);  // Deferred property
}
```

### Input Processing

```csharp
protected override void OnUpdate(double deltaTime)
{
    if (InputMap.IsPressed("MoveLeft"))
        SetPosition(Position + Vector2.Left * _speed * deltaTime);
    
    if (InputMap.IsPressed("MoveRight"))
        SetPosition(Position + Vector2.Right * _speed * deltaTime);
}
```

### State Machine

```csharp
protected override void OnUpdate(double deltaTime)
{
    _currentState.Update(deltaTime);
    
    if (_currentState.ShouldTransition())
    {
        _currentState.Exit();
        _currentState = _currentState.NextState;
        _currentState.Enter();
    }
}
```

---

## Design Decisions

### Why Include OnUpdate in Interface?

**Rationale**: Makes extensibility point explicit. Derived classes know they can override `OnUpdate` for custom logic.

### Why Update Children After Component Logic?

**Rationale**: Parent computes state (transforms, positions) first, then children can use parent state for their updates.

### Why Pass DeltaTime to Events?

**Rejected**: Events use `EventArgs` (no deltaTime). Handlers can access deltaTime from outer scope if needed. Keeps event signatures consistent.

### Why Not Include IsActive Check?

**Rationale**: Allows flexibility - some systems may want to update inactive components (e.g., pre-loading, background processing). Component implementations can check `IsActive()` in `OnUpdate` if needed.

---

## References

- **Data Model**: See `data-model.md` Component.Lifecycle section
- **Research**: See `research.md` Question 2 for interface splitting rationale
- **Spec**: See `spec.md` FR-005 for `IUpdatable` creation requirement
- **Deferred Properties**: See `.docs/Deferred Property Generation System.md` for `ApplyUpdates` details
