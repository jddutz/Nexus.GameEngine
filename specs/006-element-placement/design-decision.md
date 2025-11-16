# Design Decision: Element vs Container Alignment

**Date**: November 16, 2025  
**Purpose**: Determine whether alignment properties belong on Element or Container

---

## Current System Analysis

### How It Works Now

**Container behavior** (HorizontalLayout/VerticalLayout):
1. Calculates content area (container bounds minus padding)
2. Creates a Rectangle for each child (specifying available space)
3. Calls `child.SetSizeConstraints(childConstraints)`
4. Child positions itself using its AnchorPoint within the constraints

**Element behavior** (in `OnSizeConstraintsChanged`):
```csharp
// Element calculates its Position from constraints + AnchorPoint
var posX = constraints.Center.X + AnchorPoint.X * constraints.HalfSize.X;
var posY = constraints.Center.Y + AnchorPoint.Y * constraints.HalfSize.Y;
SetPosition(new Vector3D<float>(posX, posY, Position.Z));
```

**Result**: Element's AnchorPoint aligns with the center of the constraints rectangle.

### TextElement Example

TextElement has **both** AnchorPoint AND TextAlign:

```csharp
/// <summary>
/// Text alignment within the element's bounds.
/// Determines how the text is positioned relative to the element's Position.
/// Default: TopLeft (-1, -1) - text starts at Position and extends right/down.
/// </summary>
[ComponentProperty]
private Vector2D<float> _textAlign = Align.TopLeft;
```

**TextAlign controls**: How text content positions itself relative to the element's Position
**AnchorPoint controls**: How the element positions itself within constraints

**Two levels of alignment**:
1. Container → Element (via AnchorPoint in OnSizeConstraintsChanged)
2. Element → Content (TextAlign positions text relative to Position)

---

## The Core Question

**Where should alignment control go?**

### Option A: Element-Based Alignment (Current Pattern)

**Add alignment property to Element**:
```csharp
[ComponentProperty]
[TemplateProperty]
private Vector2D<float> _selfAlign = new(0, 0);  // How to position within constraints
```

**How it works**:
- Container provides Rectangle constraints (available space)
- Element uses `_selfAlign` to choose where to position within that rectangle
- Element calculates: `Position = constraints.Origin + _selfAlign * constraints.Size` (or similar formula)
- AnchorPoint still affects rendering (shader-level offset)

**Pros**:
- Each element controls its own positioning
- Follows TextElement pattern (TextAlign controls content positioning)
- Flexible: different children can align differently
- No container changes needed
- Works with any parent (container or window)

**Cons**:
- Every element needs to understand alignment logic
- Duplicate alignment code in each Element subclass
- Container has less control over layout
- Need to decide relationship between _selfAlign and AnchorPoint

**Example**:
```csharp
var button = new ElementTemplate
{
    Size = new(100, 50),
    SelfAlign = Align.TopLeft,      // Position at top-left of constraints
    AnchorPoint = Align.TopLeft     // Render with top-left at Position
};
```

---

### Option B: Container-Based Alignment (Layout Control)

**Add alignment property to Container classes**:
```csharp
// In HorizontalLayout
[ComponentProperty]
[TemplateProperty]
private float _verticalAlignment = -1f;  // -1=top, 0=center, 1=bottom

// In VerticalLayout
[ComponentProperty]
[TemplateProperty]
private float _horizontalAlignment = -1f;  // -1=left, 0=center, 1=right
```

**How it works**:
- Container calculates child Position based on alignment rules
- Container sets appropriate constraints.Origin to achieve desired positioning
- OR: Container directly sets child AnchorPoint before calling SetSizeConstraints
- Children don't need to know about alignment

**Pros**:
- Container has full control over layout
- Consistent positioning for all children
- Simpler element implementation
- Matches typical UI layout patterns (CSS flexbox, etc.)
- Alignment is a layout concern, not element concern

**Cons**:
- Requires container changes
- Less flexible per-child positioning
- Need to handle interaction with child's AnchorPoint
- Only works in containers (not for window-placed elements)

**Example**:
```csharp
var layout = new HorizontalLayoutTemplate
{
    VerticalAlignment = Align.Center.Y,  // Center all children vertically
    Children = new[]
    {
        new ElementTemplate { Size = new(100, 50) },  // Centered
        new ElementTemplate { Size = new(80, 40) }     // Also centered
    }
};
```

---

### Option C: Hybrid Approach (Both Element and Container)

**Container provides default, Element can override**:

```csharp
// Container has default alignment
class HorizontalLayout
{
    private float _verticalAlignment = -1f;  // Default: top-align
}

// Element can override
class Element
{
    private Vector2D<float>? _selfAlign = null;  // null = use container default
}
```

**How it works**:
- Container checks if child has SelfAlign set
- If yes: use child's preference
- If no: use container's default alignment
- Best of both worlds

**Pros**:
- Flexible: container sets pattern, element can customize
- Works for most cases with minimal configuration
- Follows principle of least surprise

**Cons**:
- More complex implementation
- Two places to set alignment (potential confusion)
- Need clear precedence rules

---

## Current System Behavior (Discovered)

Looking at the current code, the system **already works like Option A**:

### Container Default Behavior

```csharp
protected virtual void UpdateLayout()
{
    // Give each child the full content area (they'll overlap if multiple children)
    foreach (var child in children)
    {            
        child.SetSizeConstraints(contentArea);
    }
}
```

**Container.UpdateLayout()**: Gives ALL children the SAME constraints (full content area)

### HorizontalLayout/VerticalLayout Override

```csharp
// HorizontalLayout
var childConstraints = new Rectangle<int>(x, contentArea.Origin.Y, w, h);
//                                        ^different X for each child
//                                           ^same Y (top of content area)

// VerticalLayout
var childConstraints = new Rectangle<int>(contentArea.Origin.X, y, w, h);
//                                        ^same X (left of content area)
//                                                                 ^different Y
```

**Specific layouts**: Vary constraints.Origin to stack children

### Current Result

**Problem**: All children get constraints with Origin at the TOP (HorizontalLayout) or LEFT (VerticalLayout) edge:

```csharp
// HorizontalLayout gives each child:
Rectangle(x, contentArea.Origin.Y, w, h)
//           ^^^^^^^^^^^^^^^^^^^^^ always at top

// Then child calculates Position:
Position = constraints.Center + AnchorPoint * constraints.HalfSize
```

**If child has**:
- AnchorPoint = (-1, -1) [top-left] → Position at constraints top-left → renders at top
- AnchorPoint = (0, 0) [center] → Position at constraints center → renders centered
- AnchorPoint = (1, 1) [bottom-right] → Position at constraints bottom-right → renders at bottom

**So currently**: Child AnchorPoint DOES control vertical position in HorizontalLayout!

---

## The Real Problem

The issue is that **AnchorPoint serves two purposes**:

1. **Layout positioning**: Where element positions itself within constraints (OnSizeConstraintsChanged)
2. **Render offset**: How shader offsets vertices relative to Position

**These are conflated**, causing confusion.

---

## Recommendation

### Short-term: Document Current Behavior

The system **already** allows elements to position themselves via AnchorPoint. This is **working as designed**:

1. Container provides Rectangle constraints (available space + location)
2. Element uses AnchorPoint to choose where within constraints to position
3. Same AnchorPoint affects rendering (shader applies same offset)

**This is Option A** - element-based alignment using AnchorPoint.

### Issues with Current System

1. **Confusing**: AnchorPoint does TWO things
2. **Inconsistent**: Different children with different AnchorPoints position differently
3. **Unexpected**: Container can't guarantee uniform alignment

### Proposed Solution: Add Explicit Alignment Property

**Add to Element (base class)**:
```csharp
/// <summary>
/// How this element aligns itself within parent-provided constraints.
/// Separate from AnchorPoint (which affects rendering).
/// -1 = start (top/left), 0 = center, 1 = end (bottom/right)
/// null = use AnchorPoint for backward compatibility
/// </summary>
[ComponentProperty]
[TemplateProperty]
protected Vector2D<float>? _layoutAlign = null;
```

**Update OnSizeConstraintsChanged**:
```csharp
var alignToUse = _layoutAlign ?? _anchorPoint;  // Backward compatible
var posX = constraints.Center.X + alignToUse.X * constraints.HalfSize.X;
var posY = constraints.Center.Y + alignToUse.Y * constraints.HalfSize.Y;
SetPosition(new Vector3D<float>(posX, posY, Position.Z));
```

**Benefits**:
1. Explicit separation of layout vs rendering concerns
2. Backward compatible (layoutAlign=null uses AnchorPoint)
3. Clear intent: LayoutAlign for positioning, AnchorPoint for rendering
4. No container changes needed
5. Works for elements in containers AND direct window placement

**Usage**:
```csharp
// Old way (still works)
new ElementTemplate
{
    AnchorPoint = Align.TopLeft  // Used for both layout and rendering
};

// New way (explicit)
new ElementTemplate
{
    LayoutAlign = Align.Center,    // Position at center of constraints
    AnchorPoint = Align.TopLeft    // But render with top-left at that position
};
```

---

## Alternative: Container-Based Alignment

If you prefer **containers control alignment**:

```csharp
// Add to HorizontalLayout
[ComponentProperty]
[TemplateProperty]
private float _verticalAlignment = 0f;  // Default: center

// In UpdateLayout(), adjust constraints.Origin
var alignedY = contentArea.Origin.Y + 
               (contentArea.Size.Y - h) * ((_verticalAlignment + 1f) / 2f);
var childConstraints = new Rectangle<int>(x, alignedY, w, h);
```

This gives **container full control**, but requires:
1. Deciding how to handle child AnchorPoint
2. Updating all layout classes
3. Less flexibility for individual children

---

## Decision Needed

**Question 1**: Should alignment be:
- **A. Element property** (each child controls its own alignment)
- **B. Container property** (container controls all children uniformly)
- **C. Both** (container default, element can override)

**Question 2**: Should we:
- **A. Add new LayoutAlign property** (separate layout from rendering)
- **B. Keep using AnchorPoint** (document current behavior)
- **C. Change OnSizeConstraintsChanged logic** (different Position calculation)

**My recommendation**: **1A + 2A** (Element property + new LayoutAlign)
- Maintains current flexibility
- Clarifies intent
- Backward compatible
- Minimal code changes
