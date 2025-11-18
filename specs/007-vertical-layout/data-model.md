# Data Model: Directional Layout Components (VerticalLayout & HorizontalLayout)

**Feature**: VerticalLayout and HorizontalLayout with property-based design  
**Date**: November 16-17, 2025  
**Status**: Updated for property-based design

## Overview

This document defines the data structures, types, and relationships for the VerticalLayout and HorizontalLayout components. The models extend the existing Container component with property-based layout control using four core properties (ItemHeight/ItemWidth, Spacing, ItemSpacing) plus inherited Alignment for flexible spacing and sizing. The design evolved from an initial enum-based approach to composable properties that provide greater flexibility.

## Core Entities

### VerticalLayout (Component)

**Type**: `public partial class VerticalLayout : Container`  
**Namespace**: `Nexus.GameEngine.GUI.Layout`  
**Inherits From**: `Container` → `Element` → `Transformable` → `IRuntimeComponent`

**Purpose**: A layout container that arranges child UI elements vertically according to a configurable layout mode.

**Properties**:

| Property | Type | Attributes | Default | Description |
|----------|------|------------|---------|-------------|
| `ItemSpacing` | `uint?` | `[ComponentProperty]`<br>`[TemplateProperty]` | `null` | Fixed spacing between children; null = use Spacing mode |
| `Spacing` | `SpacingMode` | `[ComponentProperty]`<br>`[TemplateProperty]` | `Justified` | Distribution mode when ItemSpacing is null |
| `ItemHeight` | `uint?` | `[ComponentProperty]`<br>`[TemplateProperty]` | `null` | Fixed height for all children (overrides child sizes); null = use child's measured height |

**Inherited Properties** (from Container):

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Padding` | `Padding` | `new(0)` | Inner spacing around content area |
| `Alignment` | `Alignment` | `Alignment.TopLeft` | Alignment for positioning within parent (Y component used for vertical distribution) |
| `SafeArea` | `SafeArea` | `SafeArea.Zero` | Safe-area margins for device notches/rounded corners |

**Methods**:

| Method | Signature | Purpose |
|--------|-----------|---------|
| `UpdateLayout()` | `protected override void UpdateLayout()` | Arranges children vertically based on properties |
| `OnItemSpacingChanged()` | `partial void OnItemSpacingChanged(uint? oldValue)` | Invalidates layout when spacing changes |
| `OnSpacingChanged()` | `partial void OnSpacingChanged(SpacingMode oldValue)` | Invalidates layout when spacing mode changes |
| `OnItemHeightChanged()` | `partial void OnItemHeightChanged(uint? oldValue)` | Invalidates layout when height changes |

**State Transitions**:
1. Inactive → Active: OnActivate() sets default SizeMode.Absolute, subscribes to events
2. Layout Invalid: Invalidate() sets _isLayoutInvalid flag
3. Layout Valid: OnUpdate() detects flag, calls UpdateLayout(), clears flag
4. Property Changed: OnPropertyChanged() partial methods trigger invalidation

**Validation Rules**:
- ItemSpacing must be >= 0 when set
- ItemHeight must be >= 0 when set
- Spacing must be valid SpacingMode enum value
- Padding values must be >= 0
- Children implement IUserInterfaceElement interface

### SpacingMode (Enumeration)

**Type**: `public enum SpacingMode`  
**Namespace**: `Nexus.GameEngine.GUI.Layout`

**Purpose**: Defines the spacing distribution algorithm when ItemSpacing is null.

**Values**:

| Value | Numeric | Description |
|-------|---------|-------------|
| `Justified` | `0` | Space between items only (first at start, last at end) |
| `Distributed` | `1` | Space before, between, and after items (equal everywhere) |

**Default Value**: `Justified` (space-between pattern)

**Mutability**: Can change at runtime; triggers layout invalidation

### HorizontalLayout (Component)

**Type**: `public partial class HorizontalLayout : Container`  
**Namespace**: `Nexus.GameEngine.GUI.Layout`  
**Inherits From**: `Container` → `Element` → `Transformable` → `IRuntimeComponent`

**Purpose**: A layout container that arranges child UI elements horizontally using the same property-based design as VerticalLayout.

**Properties**:

| Property | Type | Attributes | Default | Description |
|----------|------|------------|---------|-------------|
| `ItemSpacing` | `uint?` | `[ComponentProperty]`<br>`[TemplateProperty]` | `null` | Fixed spacing between children; null = use Spacing mode |
| `Spacing` | `SpacingMode` | `[ComponentProperty]`<br>`[TemplateProperty]` | `Justified` | Distribution mode when ItemSpacing is null |
| `ItemWidth` | `uint?` | `[ComponentProperty]`<br>`[TemplateProperty]` | `null` | Fixed width for all children (overrides child sizes); null = use child's measured width |

**Inherited Properties** (from Container):
- Same as VerticalLayout (Padding, Alignment.X for horizontal distribution, SafeArea)

**Methods**:
- Same pattern as VerticalLayout but for horizontal axis
- `UpdateLayout()`: Arranges children horizontally
- Property change partial methods for invalidation

**State Transitions**: Same as VerticalLayout

**Validation Rules**: Same as VerticalLayout (adapted for horizontal)

### Component Hierarchy

```
IRuntimeComponent (interface)
  ↑
Transformable (abstract)
  ↑
Element (abstract)
  ↑
Container (partial class)
  ↑
VerticalLayout (partial class)    HorizontalLayout (partial class)
  ↓                                    ↓
Child Elements (IUserInterfaceElement)  Child Elements (IUserInterfaceElement)
```

### Ownership Model

- **VerticalLayout** owns its children via `Children` collection (inherited from RuntimeComponent)
- **ContentManager** manages lifecycle (caching, activation, disposal)
- **ComponentFactory** instantiates children from templates
- **VerticalLayout** positions children but does NOT own their rendering resources

### Data Flow

```
Template (record)
  ↓ (deserialization)
ComponentFactory.CreateInstance()
  ↓ (instantiation)
VerticalLayout (inactive)
  ↓ (activation)
ContentManager.Activate()
  ↓ (lifecycle)
VerticalLayout.OnActivate()
  ↓ (initial layout)
UpdateLayout()
  ↓ (positioning)
child.SetSizeConstraints()
  ↓ (child self-layout)
child.OnSizeConstraintsChanged()
```

### Layout Update Flow

```
External Event
  ↓
Invalidate() [container resize, child added/removed, property change]
  ↓
_isLayoutInvalid = true
  ↓
OnUpdate(deltaTime)
  ↓
if (_isLayoutInvalid)
  ↓
UpdateLayout()
  ├─ Calculate content area (subtract padding)
  ├─ Query children (IUserInterfaceElement)
  ├─ Check ItemSpacing.HasValue
  │   ├─ true: LayoutWithFixedSpacing() + Alignment.Y distribution
  │   └─ false: LayoutWithAutomaticSpacing() using Spacing mode
  ├─ For each child:
  │   ├─ Determine size (ItemHeight/ItemWidth vs Measure())
  │   ├─ Calculate constraints Rectangle
  │   └─ child.SetSizeConstraints(constraints)
  └─ _isLayoutInvalid = false
```

## Supporting Data Structures

### Rectangle<int> (Struct)

**Purpose**: Defines a rectangular area in pixel space (position + size)

**Usage in VerticalLayout**:
- Content area calculation
- Child size constraints

**Properties**:
- `Origin`: `Vector2D<int>` (X, Y position)
- `Size`: `Vector2D<int>` (Width, Height)

### Padding (Struct)

**Purpose**: Defines spacing around content area edges

**Properties**:
- `Left`: `int`
- `Top`: `int`
- `Right`: `int`
- `Bottom`: `int`

**Usage**: Subtracted from TargetSize to calculate content area

### Vector2D<T> (Struct)

**Purpose**: Two-dimensional vector (generic over numeric types)

**Usage in VerticalLayout**:
- Spacing (Vector2D<float>): Spacing.Y used for vertical spacing between children
- Size (Vector2D<int>): Child sizes and measurements
- Position (Vector2D<int>): Constraint positions

## Property-Based Layout Algorithms

### Child Size Determination

Both layouts use this algorithm to determine child sizes:

```csharp
uint DetermineChildSize(IUserInterfaceElement child)
{
    // ItemHeight/ItemWidth override
    if (ItemHeight.HasValue) // or ItemWidth for HorizontalLayout
        return ItemHeight.Value;
    
    // Measure child
    var measured = child.Measure(availableSize);
    if (measured.Height > 0) // or Width for HorizontalLayout
        return (uint)measured.Height;
    
    // Fallback for zero-size children
    return 50; // Default size
}
```

**Priority Order**:
1. **ItemHeight/ItemWidth override**: Fixed size for all children
2. **Child's intrinsic size**: child.Measure() result
3. **Fallback**: Default size for zero-size children

### Fixed Spacing Algorithm (ItemSpacing set)

**VerticalLayout**:
```
totalHeight = sum(DetermineChildSize(child) for each child)
totalSpacing = ItemSpacing * (childCount - 1)
remainingSpace = contentArea.Height - totalHeight - totalSpacing

// Distribute remaining space based on Alignment.Y
startY = contentArea.Origin.Y + 
         remainingSpace * ((Alignment.Y + 1.0f) / 2.0f)  // -1 = top, 0 = center, 1 = bottom

Y = startY
For each child:
    h = DetermineChildSize(child)
    constraints = Rectangle(contentArea.X, Y, contentArea.Width, h)
    child.SetSizeConstraints(constraints)
    Y += h + ItemSpacing
```

**HorizontalLayout**: Same logic but on horizontal axis (X instead of Y, Width instead of Height)

### Automatic Spacing - Justified (SpacingMode.Justified)

**VerticalLayout**:
```
childHeight = sum(DetermineChildSize(child) for each child)
availableSpace = contentArea.Height - childHeight
spacing = availableSpace / (childCount - 1)  // Space between only

Y = contentArea.Origin.Y
For each child:
    h = DetermineChildSize(child)
    constraints = Rectangle(contentArea.X, Y, contentArea.Width, h)
    child.SetSizeConstraints(constraints)
    Y += h + spacing
```

**HorizontalLayout**: Same logic on horizontal axis

### Automatic Spacing - Distributed (SpacingMode.Distributed)

**VerticalLayout**:
```
childHeight = sum(DetermineChildSize(child) for each child)
availableSpace = contentArea.Height - childHeight
spacing = availableSpace / (childCount + 1)  // Space before/between/after

Y = contentArea.Origin.Y + spacing
For each child:
    h = DetermineChildSize(child)
    constraints = Rectangle(contentArea.X, Y, contentArea.Width, h)
    child.SetSizeConstraints(constraints)
    Y += h + spacing
```

**HorizontalLayout**: Same logic on horizontal axis

## Edge Case Handling

### Empty Children Collection

**Behavior**: UpdateLayout() returns early without errors

```csharp
if (children.Length == 0) return;
```

### Single Child

**Behavior**: All modes work correctly (no spacing applied, no division by zero)

### Insufficient Vertical Space

**Scenario**: Total child height > content area height

**Behavior**: 
- Children positioned according to mode algorithm
- Overflow extends beyond content area (clipping handled by parent/viewport)
- Negative spacing possible in SpacedEqually mode (causes overlap)

### Negative Spacing

**Scenario**: Spacing.Y < 0

**Behavior**: Children overlap by absolute spacing amount (allowed behavior)

### Zero-Size Children (Measure Returns 0)

**Scenario**: Child.Measure() returns (0, 0) or relevant dimension = 0

**Behavior** (depends on properties):
- If `ItemHeight/ItemWidth` set: Use override size
- Otherwise: Use fallback size (50px)

**Result**: Child is always allocated some size and will be visible (if it has renderable content)

## Template Configuration Example

```csharp
new VerticalLayoutTemplate
{
    ItemSpacing = 10,
    Spacing = SpacingMode.Justified,  // Used when ItemSpacing is null
    ItemHeight = 40,  // All children 40px tall
    Alignment = Alignment.TopCenter,  // Center remaining space vertically
    Padding = new(10, 10, 10, 10),
    SizeMode = SizeMode.Absolute,
    Children = new[]
    {
        new ButtonTemplate { Width = 200, Text = "Button 1" },
        new ButtonTemplate { Width = 200, Text = "Button 2" },
        new ButtonTemplate { Width = 200, Text = "Button 3" }
    }
}
```

## Validation Summary

| Rule | Enforced By | Failure Behavior |
|------|-------------|------------------|
| ItemSpacing >= 0 when set | uint? type | Compile error |
| ItemHeight/ItemWidth >= 0 when set | uint? type | Compile error |
| Spacing is valid SpacingMode | C# enum | Compile error |
| Padding >= 0 | Padding struct | ArgumentException |
| Children implement IUserInterfaceElement | LINQ OfType<>() filter | Non-matching children ignored |

## Performance Characteristics

- **Time Complexity**: O(n) where n = number of children
- **Space Complexity**: O(1) - no allocations in UpdateLayout()
- **Update Frequency**: Only when _isLayoutInvalid flag is set
- **Invalidation Triggers**: Container resize, child add/remove, mode change

## Backward Compatibility

**Existing Code Impact**: Minimal

- ItemHeight property retained for backward compatibility
- New properties have sensible defaults
- No breaking changes to Container or Element APIs

**Migration Path**: Existing uses of VerticalLayout continue working without modification. New features available via ItemSpacing, Spacing, and Alignment properties.
