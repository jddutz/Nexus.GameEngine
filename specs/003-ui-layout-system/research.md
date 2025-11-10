# Research: UI Layout System

**Feature**: `003-ui-layout-system`  
**Date**: 2025-11-04  
**Purpose**: Resolve technical unknowns and establish best practices for resolution-independent UI layout system

## Research Questions

1. How should constraint propagation handle percentage-based sizing when parent size is dynamic?
2. What's the optimal invalidation strategy for nested layouts (top-down vs bottom-up recalculation)?
3. How should layout containers handle children with intrinsic sizing vs fixed sizing?
4. What's the interaction between ComponentProperty animations and layout recalculation?
5. How should grid layouts handle uneven cell counts and aspect ratio preservation?
6. What's the best practice for safe-area handling across different aspect ratios?

---

## Question 1: Percentage-Based Sizing with Dynamic Parents

### Decision
**Two-pass constraint propagation**: First pass propagates known sizes top-down, second pass resolves percentages bottom-up once parent sizes stabilize.

### Rationale
Percentage-based sizing requires the parent's final size to be known before calculating child size. In nested layouts, this creates a dependency chain:
- Parent's size may depend on its children's intrinsic sizes (shrink-wrap mode)
- Child's size depends on parent's final size (percentage mode)

**Implementation approach**:
1. **Pass 1 (Measure)**: Traverse tree top-down, calculate intrinsic sizes for elements with fixed/content-based sizing
2. **Pass 2 (Arrange)**: Traverse tree top-down again, applying constraints and resolving percentages now that parent sizes are known

This matches standard layout engines (WPF, Flutter, UIKit):
- **WPF**: `Measure()` → `Arrange()` two-pass system
- **Flutter**: `performLayout()` with size constraints flowing down, sizes flowing up
- **UIKit**: `sizeThatFits()` → `layoutSubviews()` pattern

### Alternatives Considered
- **Single-pass with deferred resolution**: Queue percentage-based children for re-calculation after parent size known
  - Rejected: More complex state management, harder to debug, potential for multiple iterations
- **Always require explicit parent size**: Force developers to specify container sizes explicitly
  - Rejected: Defeats purpose of responsive design, increases developer burden

### Implementation Details
```csharp
// Element.cs
public override void SetSizeConstraints(Rectangle<int> constraints)
{
    _sizeConstraints = constraints;
    
    // Pass 1: Measure - calculate preferred size based on content/children
    var preferredSize = CalculatePreferredSize(constraints);
    
    // Apply size mode (Fill, Shrink, Fixed, Percentage)
    var finalSize = ApplySizeMode(preferredSize, constraints);
    
    // Update actual size
    Size = finalSize;
    
    // Pass 2: Arrange - propagate resolved size to children
    ArrangeChildren();
}

private Vector2D<int> CalculatePreferredSize(Rectangle<int> constraints)
{
    if (SizeMode == SizeMode.Fixed)
        return new Vector2D<int>((int)Width, (int)Height);
    
    if (SizeMode == SizeMode.Percent)
        return new Vector2D<int>(
            (int)(constraints.Size.X * WidthPercentage / 100),
            (int)(constraints.Size.Y * HeightPercentage / 100)
        );
    
    // Shrink mode: query children for intrinsic size
    return CalculateIntrinsicSize();
}
```

---

## Question 2: Optimal Invalidation Strategy for Nested Layouts

### Decision
**Leverage existing ComponentProperty deferred updates + nearest-layout invalidation + top-down recalculation**: When a layout-affecting property changes, locate the nearest ancestor layout via `FindParent<ILayout>()` and request recalculation from that layout. Layout-affecting properties are expected to affect the immediate children of the layout element; those children may in turn cause further layout recalculations down their own subtrees. We do not require change notifications to traverse all the way to the root in the common case.

### Rationale
The existing ComponentProperty system already provides frame-boundary update deferral via ContentManager's two-pass update:
1. **FIRST PASS** (ContentManager.OnUpdate): `ApplyUpdates(deltaTime)` on all Entity-based components
2. **SECOND PASS**: `Update(deltaTime)` on all active RuntimeComponents

This eliminates the need for manual dirty flags and provides automatic batching of property changes within a frame.

**Simplified flow leveraging existing infrastructure**:
1. **Property Change** → ComponentProperty setter queues deferred update (automatic)
2. **Frame Boundary** → ContentManager applies all deferred updates in FIRST PASS (`ApplyUpdates()`)
3. **Property Applied** → Property changed callback locates the nearest `ILayout` ancestor via `FindParent<ILayout>()` and requests `RecalculateLayout()` on that layout
4. **Recalculation** → The layout recalculates its immediate children (parent→child). Children that are themselves layouts will receive `SetSizeConstraints()` and may recurse further as needed
5. **Constraint Propagation** → `child.SetSizeConstraints()` is called for each child as part of the layout's arrange pass
6. **Update Phase** → ContentManager SECOND PASS calls `Update()` which traverses leaf→root for game logic (layout already calculated)

**Key advantages**:
- ✅ **Zero new infrastructure** - Uses existing ComponentProperty + ContentManager pattern
- ✅ **Automatic batching** - Multiple property changes coalesced by frame boundary
- ✅ **Simple propagation** - Layout controls children, no upward notification needed
- ✅ **Consistent with engine** - Same pattern used for all animated properties
- ✅ **No layout thrashing** - Frame boundary guarantees single layout pass per frame
- ✅ **Clear ownership** - Parent sets child constraints, child doesn't notify parent

### Alternatives Considered
- **Manual dirty flags with immediate propagation**:
  - Rejected: Redundant with ComponentProperty deferred updates, more code to maintain
- **Bottom-up recalculation** from changed child:
  - Rejected: Breaks constraint propagation model (parent determines child constraints)
- **Event-based invalidation**:
  - Rejected: Adds complexity, notification bubbling via FindParent is simpler and type-safe

### Implementation Details
```csharp
// ILayout.cs (new interface)
public interface ILayout : IRuntimeComponent
{
    void RecalculateLayout();
}

// Layout.cs
public abstract class Layout : Element, ILayout
{
    // ComponentProperty for Padding (auto-generates deferred update logic)
    [ComponentProperty]
    [TemplateProperty]
    private Padding _padding = new Padding(0);
    
    // When generated Padding property setter is called, recalculate layout
    partial void OnPaddingChanged()
    {
        RecalculateLayout();
    }
    
    // When size constraints change (parent resized, window resized)
    public override void SetSizeConstraints(Rectangle<int> constraints)
    {
        if (_sizeConstraints != constraints)
        {
            base.SetSizeConstraints(constraints);
            RecalculateLayout();
        }
    }
    
    public void RecalculateLayout()
    {
        // Implement layout algorithm (VerticalLayout, HorizontalLayout, GridLayout)
        // This calculates positions and sizes for all children
        
        // Example for VerticalLayout:
        var constraints = GetSizeConstraints();
        var currentY = _padding.Top;
        
        foreach (var child in Children.OfType<Element>())
        {
            // Calculate child position and size based on layout algorithm
            child.Position = new Vector2D<float>(_padding.Left, currentY);
            
            // Propagate constraints to child Element
            var childConstraints = new Rectangle<int>(
                (int)child.Position.X,
                (int)child.Position.Y,
                availableWidth,
                childHeight
            );
            child.SetSizeConstraints(childConstraints);
            
            // SetSizeConstraints will trigger child layout's RecalculateLayout if needed
            
            currentY += child.Size.Y + _spacing.Y;
        }
        
        // Update own size based on children (for intrinsic sizing)
        if (_sizeMode == SizeMode.Intrinsic)
        {
            Size = new Vector2D<float>(constraints.Size.X, currentY + _padding.Bottom);
        }
    }
}

// ContentManager.cs (existing, no changes needed)
public void OnUpdate(double deltaTime)
{
    // FIRST PASS: Apply deferred updates (ComponentProperty changes)
    // RecalculateLayout() is called directly when layout properties change
    // This propagates top-down via SetSizeConstraints()
    foreach (var component in AllComponents)
    {
        if (component is Entity entity)
            entity.ApplyUpdates(deltaTime);
    }
    
    // SECOND PASS: Update logic (leaf → root traversal)
    // Layout recalculation already completed in FIRST PASS
    foreach (var component in ActiveComponents)
    {
        if (component is IRuntimeComponent runtime)
            runtime.Update(deltaTime); // Leaf → root
    }
}
```

**Critical design**: 
- **RecalculateLayout() is called IMMEDIATELY** when layout property changes (during ApplyUpdates)
- **Not deferred** to OnUpdate() - ensures layout is consistent before Update() phase
- **Nearest-layout invalidation** - the ComponentProperty change locates the nearest `ILayout` ancestor (via `FindParent<ILayout>()`) and requests recalculation on that layout. This keeps recalculation scope small and predictable.
- **Top-down propagation only** - Layout calls SetSizeConstraints() on children, triggering child layout recalculation when necessary
- **No broad upward notification** - Children don't notify parents; parent/nearest-layout controls children
- **Update() is leaf → root**, but layout calculation is **parent → child** (separate traversal)

**Flow Summary**:
```
Frame Boundary (ContentManager.OnUpdate)
    │
    ├─→ FIRST PASS: ApplyUpdates() on all entities
    │       │
    │       ├─→ Property change applied (e.g., Padding updated)
    │       │
    │       ├─→ OnPaddingChanged() callback triggered
    │       │
    │       ├─→ RecalculateLayout() called IMMEDIATELY
    │       │       │
    │       │       ├─→ Calculate positions/sizes for children
    │       │       │
    │       │       ├─→ For each child:
    │       │       │       │
    │       │       │       ├─→ Calculate child position
    │       │       │       │
    │       │       │       ├─→ child.SetSizeConstraints(constraints)
    │       │       │       │       │
    │       │       │       │       └─→ If child is ILayout:
    │       │       │       │               │
    │       │       │       │               └─→ child.RecalculateLayout()
    │       │       │       │                       │
    │       │       │       │                       └─→ [RECURSE PARENT → CHILD]
    │       │       │       │
    │       │       │       └─→ Use child.Size for next iteration
    │       │       │
    │       │       └─→ Update own size (if intrinsic sizing)
    │       │
    │       └─→ Layout complete before Update() phase
    │
    └─→ SECOND PASS: Update() on all active components (LEAF → ROOT)
            │
            └─→ Game logic, animations, etc. (layout already stable)
```

**Key Points**:
1. Property change → ComponentProperty queues update
2. Frame boundary → ApplyUpdates() applies property change
3. Property changed callback → RecalculateLayout() called directly
4. **Layout recalculates IMMEDIATELY** (nearest-layout invalidation — the change locates the nearest `ILayout` and triggers it)
5. For each child → Calculate position and call SetSizeConstraints()
6. SetSizeConstraints() on child Layout → Triggers child's RecalculateLayout() (parent → child recursion)
7. **Pure top-down propagation** → No upward notification needed
8. Later: Update() traverses leaf → root for game logic (layout already calculated)

**Note**: This approach is optimal because it:
1. Eliminates redundant dirty flag infrastructure (ComponentProperty already handles this)
2. Provides automatic frame-boundary batching (ContentManager already implements two-pass update)
3. **Pure top-down propagation** - Nearest-layout invalidation then parent→child propagation; no broad upward notification
4. **Clear ownership model** - Parent controls children via SetSizeConstraints()
5. **Separates layout calculation (parent→child) from update logic (leaf→root)**
6. Aligns with engine's existing property animation system

---

## Question 3: Intrinsic vs Fixed Sizing in Layout Containers

### Decision
**Support three sizing strategies**: Intrinsic (content-driven), Fixed (developer-specified), and Stretch (fill available space). Layout containers query children for preferred size and distribute space accordingly.

### Rationale
Different UI elements have different sizing needs:
- **Text elements**: Size determined by text content (intrinsic)
- **Icons/images**: Size determined by texture dimensions or explicit size (intrinsic or fixed)
- **Containers**: Size determined by parent constraints or children (stretch or intrinsic)

**Standard practice** (WPF, SwiftUI, Flutter):
- Elements expose preferred/intrinsic size via measurement API
- Layouts query children during measure pass
- Layouts distribute available space based on sizing strategy and constraints

### Alternatives Considered
- **Fixed sizing only**: Require developers to specify all sizes explicitly
  - Rejected: Tedious for content-driven elements (text, dynamic lists)
- **Automatic only**: Always use intrinsic sizing
  - Rejected: Can't handle "fill remaining space" scenarios (stretch-to-fill)

### Implementation Details
```csharp
// Element.cs
public enum SizeMode
{
    Fixed,      // Use Width/Height properties explicitly
    Intrinsic,  // Size determined by content (text length, texture size)
    Stretch,    // Fill available space from parent constraints
    Percentage  // Percentage of parent size
}

[TemplateProperty]
[ComponentProperty]
private SizeMode _sizeMode = SizeMode.Fixed;

protected virtual Vector2D<int> CalculateIntrinsicSize()
{
    // Base implementation: query children and calculate bounds
    if (Children.Count == 0)
        return new Vector2D<int>(100, 100); // Default minimum size
    
    int maxX = 0, maxY = 0;
    foreach (var child in Children)
    {
        if (child is Element elem)
        {
            var childSize = elem.CalculateIntrinsicSize();
            var childPos = elem.Position;
            maxX = Math.Max(maxX, (int)childPos.X + childSize.X);
            maxY = Math.Max(maxY, (int)childPos.Y + childSize.Y);
        }
    }
    return new Vector2D<int>(maxX, maxY);
}

// TextElement.cs (override)
protected override Vector2D<int> CalculateIntrinsicSize()
{
    // Query text rendering system for text bounds
    return FontManager.MeasureText(Text, FontFamily, FontSize);
}

// Note: Intrinsic text measurement (line wrapping, localization, fallback fonts)
// is the responsibility of the `TextElement` and the FontManager. Layouts must
// trust the intrinsic size reported by text elements; they should not attempt
// to re-measure or override text layout behavior.
```

---

## Question 4: ComponentProperty Animations vs Layout Recalculation

### Decision
**Layout-affecting properties use deferred updates only (no animation)**. Visual properties can be animated without triggering layout recalculation. Properties use an interceptor pattern to control animation parameters before applying changes.

### Rationale
ComponentProperty animations interpolate values over time with per-frame updates. The key insight is distinguishing between:

**Deferred Updates (Most Layout Properties)**:
- Single-frame transition: old value → new value
- Applied at next frame boundary during ApplyUpdates()
- Layout recalculates once with final values
- Examples: Padding, Spacing, ColumnCount, SizeMode, child add/remove

**Animated Updates (Visual Properties Only)**:
- Multi-frame interpolation: old value → interpolated values → new value
- Per-frame callbacks during interpolation
- Layout does NOT recalculate during animation
- Examples: TintColor, Opacity, Scale, Rotation

**The Animation/Layout Problem**:
If layout properties were animated (e.g., Padding from 10 to 50 over 0.5 seconds):
- Each interpolated frame (10 → 15 → 20 → ...) triggers OnPaddingChanged() → RecalculateLayout()
- Layout thrashes at 60fps (60 recalculations/second)
- Child positions/sizes constantly update during parent animation
- Cascading per-frame updates to all descendants
- Performance degrades, animations stutter

Solution: BeforeChange Pattern:
Add optional interceptor to ComponentProperty attribute that allows properties to modify animation parameters before the change is queued.

Key clarifications (updated):

- The BeforeChange hook is an instance (non-static), protected method that lives on the component (typically `Entity`/`RuntimeComponent`) itself. The default helper `CancelAnimation` will be provided as a protected method on the `Entity` base class so layouts and other components can call it by name.
- Generator expectations: the source generator will look for an instance method with the exact signature shown below. If the method cannot be found or the signature doesn't match, the generator (or analyzer) should emit a clear compile-time diagnostic describing the expected signature.

Required method signature:

```csharp
// Must be an instance (protected/private allowed) method on the same class.
// T must match the field type the attribute decorates.
void Before{PropertyName}Change(ref T newValue, ref float duration, ref InterpolationMode mode)
```

Example use (layout properties - default behavior cancels animation):

```csharp
[ComponentProperty(BeforeChange = nameof(BeforePaddingChange))]
[TemplateProperty]
private Padding _padding = new Padding(0);

// Instance method on the component; generator will call this before queuing the update
protected void BeforePaddingChange(ref Padding newValue, ref float duration, ref InterpolationMode mode)
{
    // Default policy for layout-affecting properties: cancel animations
    duration = 0f;
    mode = InterpolationMode.Step;
}
```

Notes:
- The project will ship a protected helper on `Entity` (e.g., `protected void CancelAnimation<T>(ref T newValue, ref float duration, ref InterpolationMode mode)`) so generators and consumers can reference a common implementation without needing a static helper.
- The generator will enforce the presence and signature of the BeforeChange hook at compile-time via a diagnostic when `BeforeChange` is specified but the method cannot be resolved with the expected signature.
- This approach keeps the API deterministic and avoids reflection-based runtime invocation.

Developer animation policy (clarified):

- The default helper (`CancelAnimation`) and default generated behavior will prevent animations of layout-affecting properties (duration forced to 0). This protects the common case from layout thrash.
- We will not block developers from requesting animations on layout properties. If a developer explicitly calls a generated setter with a non-zero duration (or supplies a custom `BeforeChange` implementation that leaves the duration intact), the engine will attempt to honor it. The onus is on the developer to use such animations responsibly.
- Components that need different semantics may provide a custom `BeforeChange` method to gate or adjust animations (for example, to allow small deltas to animate but cancel large ones).

### Alternatives Considered
- **Recalculate on every frame during animation**: 
  - Rejected: Causes layout thrashing, defeats animation performance goals
- **Disable animations for all layout properties globally**:
  - Rejected: Too rigid, loses flexibility for edge cases
- **Separate visual transform from layout transform**:
  - Rejected: Adds complexity (two transform systems), doesn't solve core issue
- **Animation completion callback only**:
  - Rejected: Still allows per-frame callbacks during animation, thrashing still occurs

### Implementation Details

**ComponentProperty Attribute Enhancement**:
```csharp
// ComponentPropertyAttribute.cs
[AttributeUsage(AttributeTargets.Field)]
public class ComponentPropertyAttribute : Attribute
{
    public string? Name { get; set; }
    public string? BeforeChange { get; set; }  // NEW: Optional before-change method name
}
```

**Generated Code Pattern** (ComponentPropertyGenerator):
```csharp
// Generated Set{PropertyName} method with BeforeChange support
public void SetPadding(Padding value, float duration = 0f, InterpolationMode interpolation = InterpolationMode.Linear)
{
    // If a BeforeChange hook is specified, call it first
    BeforePaddingChange(ref value, ref duration, ref interpolation);
    
    // Queue the (potentially modified) update
    QueueUpdate(() => {
        var oldValue = _padding;
        _padding = value;
        OnPaddingChanged(oldValue);
    }, duration, interpolation);
}
```

**Static Utility BeforeChange** (Entity.cs or RuntimeComponent.cs):
```csharp
// RuntimeComponent.cs (or Entity.cs)
/// <summary>
/// Static BeforeChange hook that cancels all animations, forcing immediate updates.
/// Use with [ComponentProperty] for layout-affecting properties.
/// </summary>
protected static void CancelAnimation<T>(ref T newValue, ref float duration, ref InterpolationMode mode)
{
    duration = 0f;
    mode = InterpolationMode.Step;
}
```

**Layout Properties** (No Animation):
[// Layout.cs
[ComponentProperty]  // Use static utility
[TemplateProperty]
private Padding _padding = new Padding(0);

[ComponentProperty]  // Use static utility
[TemplateProperty]
private Vector2D<float> _spacing = new(0, 0);

partial void OnPaddingChanged(Padding oldValue)
{
    // Called once at frame boundary when deferred update applied
    RecalculateLayout();
}
```

**Visual Properties** (Allow Animation):
```csharp
[// Element.cs
[ComponentProperty]  // No BeforeChange hook - animations allowed
[TemplateProperty]
private Vector4D<float> _tintColor = new(1, 1, 1, 1);

// Can be animated: element.SetTintColor(newColor, duration: 0.3f, InterpolationMode.EaseOut)
// Per-frame OnTintColorChanged() called during interpolation
partial void OnTintColorChanged(Vector4D<float> oldValue)
{
    // Update shader uniform, no layout recalculation needed
    UpdateTintUniform();
}
```

**Child Property Updates During Layout**:
```csharp
// Layout.cs - RecalculateLayout()
protected void RecalculateLayout()
{
    foreach (var child in Children.OfType<Element>())
    {
        // Calculate new position/size
        var newPosition = CalculateChildPosition(child);
        var newSize = CalculateChildSize(child);
        
        // Set child properties WITHOUT animation (immediate update)
        child.SetPosition(newPosition);  // duration defaults to 0
        child.SetSize(newSize);          // duration defaults to 0
        
        // Propagate constraints
        child.SetSizeConstraints(new Rectangle<int>(...));
    }
}
```

### Property Classification

**Layout-Affecting Properties** (Must Use BeforeChange to Disable Animation):
- Structure: `Padding`, `Spacing`, `ColumnCount`, `RowCount`, `GridTemplateColumns`
- Sizing: `SizeMode`, `WidthPercentage`, `HeightPercentage`, `MinSize`, `MaxSize`
- Alignment: `HorizontalAlignment`, `VerticalAlignment`, `HorizontalContentAlignment`, `VerticalContentAlignment`
- Behavior: `WrapContent`, `Orientation`, `CellAspectRatio`

**Visual-Only Properties** (Can Animate Freely):
- Color: `TintColor`, `Opacity`, `BackgroundColor`
- Transform: `Scale`, `Rotation` (via Transformable)
- Effects: `BlurRadius`, `ShadowOffset` (future)

**Special Case - Size/Position**:
- When set by **layout calculation**: No animation (immediate update)
- When set by **developer code**: Can optionally animate (e.g., manual slide-in effect)
- Layouts should use BeforeChange hooks or call with duration=0 to ensure immediate updates

### Benefits of BeforeChange Pattern

1. **Per-Property Control**: Each property can define its own animation policy
2. **No Generator Changes**: Core ComponentPropertyGenerator unchanged, extensibility via attribute
3. **Explicit Intent**: Developer sees `BeforeChange = nameof(...)` in code, understands behavior
4. **Flexibility**: Edge cases can use custom logic (e.g., "animate only if value delta > threshold")
5. **Type Safety**: BeforeChange method signature enforced at compile time
6. **MVP Ready**: Simple to implement, easy to understand, minimal code

### Summary

For MVP:
- Layout properties use `BeforeChange` to force immediate updates (duration=0)
- Visual properties have no BeforeChange hook, support optional animation
- Developer can call `SetProperty(value)` for immediate update, or `SetProperty(value, duration, interpolation)` for animation
- Layouts always set child properties with duration=0 (no animation during layout calculation)
- RecalculateLayout() called once per property change at frame boundary via OnPropertyChanged callback

---

## Question 5: Grid Layout Cell Distribution and Aspect Ratio

### Decision
**Row-major cell distribution with configurable column count**. Cells size to fit available space while respecting min/max constraints. Aspect ratio preservation is opt-in per grid configuration.

### Rationale
Grid layouts have two common use cases:
1. **Fixed grid** (e.g., inventory, ability bar): N columns, variable rows, cells same size
2. **Responsive grid** (e.g., photo gallery): Variable columns based on viewport width, cells maintain aspect ratio

**Standard patterns** (CSS Grid, SwiftUI Grid, Unity Grid):
- Specify columns (or rows), other dimension auto-calculates
- Cell sizing strategies: uniform (all same size) vs content-based (size to content)
- Aspect ratio: optional constraint per cell or global grid setting

### Alternatives Considered
- **Column-major layout**: Fill columns first, then next column
  - Rejected: Row-major is more intuitive for most game UI (left-to-right, top-to-bottom reading)
- **Automatic column calculation**: Determine columns based on cell count and viewport
  - Considered: Useful for responsive galleries, but adds complexity. Defer to post-MVP.

### Implementation Details
```csharp
// GridLayout.cs
public class GridLayout : Layout
{
    [TemplateProperty]
    [ComponentProperty]
    private int _columnCount = 3;
    
    [TemplateProperty]
    [ComponentProperty]
    private Vector2D<float> _spacing = new(10, 10);
    
    [TemplateProperty]
    [ComponentProperty]
    private bool _maintainCellAspectRatio = false;
    
    [TemplateProperty]
    [ComponentProperty]
    private float _cellAspectRatio = 1.0f; // Width:Height (1.0 = square)
    
    protected override void RecalculateLayout()
    {
        var constraints = GetSizeConstraints();
        var padding = Padding;
        
        int availableWidth = constraints.Size.X - padding.Left - padding.Right;
        int cellWidth = (availableWidth - (int)_spacing.X * (_columnCount - 1)) / _columnCount;
        
        int cellHeight = _maintainCellAspectRatio 
            ? (int)(cellWidth / _cellAspectRatio)
            : cellWidth; // Default: square cells
        
        int row = 0, col = 0;
        foreach (var child in Children)
        {
            if (child is Element elem)
            {
                // Position child in grid cell
                int x = padding.Left + col * (cellWidth + (int)_spacing.X);
                int y = padding.Top + row * (cellHeight + (int)_spacing.Y);
                
                elem.Position = new Vector2D<float>(x, y);
                elem.Size = new Vector2D<float>(cellWidth, cellHeight);
                
                // Propagate constraints to child
                var childConstraints = new Rectangle<int>(x, y, cellWidth, cellHeight);
                elem.SetSizeConstraints(childConstraints);
                
                // Move to next cell
                col++;
                if (col >= _columnCount)
                {
                    col = 0;
                    row++;
                }
            }
        }
        
        // Update own size based on grid dimensions
        int rowCount = (Children.Count + _columnCount - 1) / _columnCount;
        int totalHeight = padding.Top + padding.Bottom + 
                         rowCount * cellHeight + 
                         (rowCount - 1) * (int)_spacing.Y;
        
        Size = new Vector2D<float>(constraints.Size.X, totalHeight);
    }
}
```

---

## Question 6: Safe-Area Handling Across Aspect Ratios

### Decision
**Safe-area margins as percentage of viewport dimensions**, with min/max pixel constraints. Layouts respect safe areas by adding additional padding in their calculations.

### Rationale
Different aspect ratios create different "unsafe" zones:
- **21:9 ultrawide**: Side edges may be cropped or extend beyond visible area on some displays
- **Mobile notches**: Top area occupied by device notch/camera cutout
- **TV overscan**: Edges may be cut off on TVs (less common with modern displays)

**Best practice** (iOS Safe Area, Android Display Cutout, Unity Safe Area):
- Define safe area as insets from screen edges
- UI respects safe area by adjusting layout margins
- Percentage-based to scale across resolutions

### Alternatives Considered
- **Fixed pixel margins**: 
  - Rejected: Doesn't scale across resolutions (50px is large on 1280x720, small on 3840x2160)
- **Aspect-ratio-specific templates**:
  - Rejected: Requires multiple UI layouts per screen, high maintenance burden
- **Automatic edge detection**:
  - Rejected: Can't detect physical display characteristics (notches, overscan) from software alone

### Implementation Details
```csharp
// Viewport.cs (or settings)
public struct SafeArea
{
    public float LeftPercent { get; set; }   // e.g., 0.05 = 5% margin
    public float TopPercent { get; set; }
    public float RightPercent { get; set; }
    public float BottomPercent { get; set; }
    
    public int MinPixels { get; set; } = 20;  // Minimum safe margin in pixels
    public int MaxPixels { get; set; } = 100; // Maximum safe margin in pixels
    
    public Padding CalculateMargins(Vector2D<int> viewportSize)
    {
        int left = Clamp(
            (int)(viewportSize.X * LeftPercent),
            MinPixels,
            MaxPixels
        );
        int top = Clamp(
            (int)(viewportSize.Y * TopPercent),
            MinPixels,
            MaxPixels
        );
        int right = Clamp(
            (int)(viewportSize.X * RightPercent),
            MinPixels,
            MaxPixels
        );
        int bottom = Clamp(
            (int)(viewportSize.Y * BottomPercent),
            MinPixels,
            MaxPixels
        );
        
        return new Padding(left, top, right, bottom);
    }
    
    private int Clamp(int value, int min, int max) =>
        Math.Max(min, Math.Min(max, value));
}

// Layout.cs
protected virtual Padding GetEffectivePadding()
{
    var basePadding = Padding;
    var safeArea = Viewport.SafeArea.CalculateMargins(Viewport.Size);
    
    // Combine base padding with safe area margins
    return new Padding(
        basePadding.Left + safeArea.Left,
        basePadding.Top + safeArea.Top,
        basePadding.Right + safeArea.Right,
        basePadding.Bottom + safeArea.Bottom
    );
}
```

**Safe-area priority (updated)**: Implement now (Priority P2) with a simple, testable approach. We will add the percentage-based `SafeArea` struct and a `GetEffectivePadding()` helper as described above, and provide unit tests that validate pixel clamping and padding combination behavior. This implementation uses conservative defaults and can be refined later with platform-specific APIs when available.

---

## Technology Best Practices Summary

### C# UI Layout Patterns

**Source**: WPF, Avalonia UI, Uno Platform best practices

1. **Two-pass layout**: Measure (calculate preferred sizes) → Arrange (apply constraints)
2. **Constraint propagation**: Parent passes available space to children via constraints
3. **Dirty flag pattern**: Mark layouts as needing recalculation, defer actual calculation
4. **Separation of concerns**: Visual transform separate from layout calculation
5. **Template pattern**: Abstract base class defines layout lifecycle, subclasses implement specific algorithms

### Game Engine UI Patterns

**Source**: Unity UGUI, Unreal UMG, Godot Control nodes

1. **Anchor-based positioning**: Elements anchor to parent edges/center using normalized coordinates (-1 to 1)
2. **Rect transform**: Position + Size + Pivot + AnchorPoint = complete transform description
3. **Layout components**: Separate components for layout algorithms (VerticalLayoutGroup, HorizontalLayoutGroup, GridLayoutGroup)
4. **Automatic sizing**: Elements can size to content or stretch to fill parent
5. **Layout priority**: Some layouts calculate before others (e.g., ContentSizeFitter before parent layout)

### Performance Optimization Patterns

**Source**: Flutter, React Native, UIKit performance docs

1. **Layout caching**: Cache calculated layouts, only recalculate when inputs change. Make it work first; optimize after profiling.
2. **Constraint caching**: Cache size constraints from parent to detect changes
3. **Incremental updates**: Only recalculate affected subtrees, not entire hierarchy
4. **Frame coalescing**: Batch multiple changes into single layout pass per frame
5. **Visibility culling**: UI layout is different from scene frustum culling — all children should participate in layout calculation. Rendering culling/visibility optimizations belong in the renderer and presentation layer; they should not be used to skip layout work in the general case.

---

## Conclusion

All research questions resolved. Key architectural decisions:

1. **Two-pass constraint propagation** (measure → arrange) for percentage sizing
2. **Lazy invalidation with dirty flags** + top-down recalculation for nested layouts
3. **Three sizing strategies**: Intrinsic, Fixed, Stretch (+ Percentage)
4. **Animations don't trigger layout recalculation** during interpolation
5. **Row-major grid distribution** with configurable columns and aspect ratio
6. **Safe-area as percentage-based margins** with min/max pixel constraints

These patterns align with industry best practices from WPF, Flutter, Unity, and game engine UI systems. Implementation will follow existing Nexus.GameEngine architecture (Component model, templates, source-generated properties).

**Next Phase**: Data model definition and API contracts.
