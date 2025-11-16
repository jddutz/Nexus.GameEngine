# Data Model: Layout Alignment Refactor

**Feature**: Layout Alignment Refactor  
**Branch**: `005-layout-alignment-refactor`  
**Date**: November 14, 2025

## Overview

This document defines the data structures and entities involved in refactoring HorizontalLayout and VerticalLayout to use a unified Vector2D<float> Alignment property.

## Core Entities

### 1. HorizontalLayout (Modified)

**Purpose**: Arranges child UI elements horizontally with configurable spacing and vertical alignment.

**Inheritance**: `Container` → `Element` → `Transformable` → `RuntimeComponent` → `IRuntimeComponent`

**Modified Fields**:

| Field | Type | Attributes | Default | Description |
|-------|------|------------|---------|-------------|
| `_alignment` | `Vector2D<float>` | `[ComponentProperty]` `[TemplateProperty]` | `new(0, 0)` | Alignment vector for child positioning. Only Y component is used for vertical alignment of children. X component is ignored. |

**Unchanged Fields** (inherited from Container):
- `_padding`: Padding - Space around the content area
- `_spacing`: Vector2D<float> - Horizontal (X) and vertical (Y) spacing between children
- `_safeArea`: SafeArea - Safe area margins for device-specific constraints
- `_stretchChildren`: bool - Whether to stretch children to fill content height

**Behavioral Changes**:
- **Previous**: Y position calculated using float `_alignment` with switch statement on VerticalAlignment constants
- **New**: Y position calculated using `_alignment.Y` component with formula: `y = contentArea.Origin.Y + (contentArea.Size.Y - childHeight) * ((Alignment.Y + 1) / 2)`

**Validation Rules**:
- None enforced at runtime (allows values outside -1 to 1 range for advanced use cases)
- Standard range: -1 (top) to 1 (bottom)
- When `_stretchChildren = true`, alignment is ignored and all children have Y = contentArea.Origin.Y

**State Transitions**:
- Alignment changes trigger deferred property update (applied during `ApplyUpdates()`)
- Layout invalidation occurs on size constraint changes, triggering `UpdateLayout()` call
- Child constraints updated synchronously when `SetSizeConstraints()` is called on the layout

---

### 2. VerticalLayout (Modified)

**Purpose**: Arranges child UI elements vertically with configurable spacing and horizontal alignment.

**Inheritance**: `Container` → `Element` → `Transformable` → `RuntimeComponent` → `IRuntimeComponent`

**Modified Fields**:

| Field | Type | Attributes | Default | Description |
|-------|------|------------|---------|-------------|
| `_alignment` | `Vector2D<float>` | `[ComponentProperty]` `[TemplateProperty]` | `new(0, 0)` | Alignment vector for child positioning. Only X component is used for horizontal alignment of children. Y component is ignored. |

**Unchanged Fields** (inherited from Container):
- `_padding`: Padding - Space around the content area
- `_spacing`: Vector2D<float> - Horizontal (X) and vertical (Y) spacing between children
- `_safeArea`: SafeArea - Safe area margins for device-specific constraints
- `_stretchChildren`: bool - Whether to stretch children to fill content width

**Behavioral Changes**:
- **Previous**: X position calculated using float `_alignment` with formula already using alignment fraction
- **New**: X position calculated using `_alignment.X` component (formula remains the same, just uses X component instead of float value)

**Validation Rules**:
- None enforced at runtime (allows values outside -1 to 1 range for advanced use cases)
- Standard range: -1 (left) to 1 (right)
- When `_stretchChildren = true`, alignment is ignored and all children have X = contentArea.Origin.X

**State Transitions**:
- Alignment changes trigger deferred property update (applied during `ApplyUpdates()`)
- Layout invalidation occurs on size constraint changes, triggering `UpdateLayout()` call
- Child constraints updated synchronously when `SetSizeConstraints()` is called on the layout

---

### 3. Container (Unchanged Base Class)

**Purpose**: Base class for all layout components, provides padding, spacing, and content area calculation.

**Key Responsibilities**:
- Calculate content area: `bounds - padding - safeArea`
- Manage layout invalidation lifecycle
- Trigger child constraint updates when layout changes
- Provide protected `UpdateLayout()` virtual method for derived classes

**Fields Used by Layouts**:
- `Padding`: Padding around content (Top, Right, Bottom, Left)
- `Spacing`: Vector2D<float> space between children (X = horizontal, Y = vertical)
- `SafeArea`: Device-specific safe margins

**Methods Used by Layouts**:
- `GetContentArea()`: Returns Rectangle<int> representing available space for children (after padding/safeArea)
- `Invalidate()`: Marks layout as needing recalculation

---

## Supporting Types

### 4. Alignment Vector (Vector2D<float>)

**Purpose**: Two-dimensional alignment vector representing horizontal (X) and vertical (Y) alignment.

**Type**: `Silk.NET.Maths.Vector2D<float>`

**Component Ranges**:
- **X (Horizontal)**: -1 (left), 0 (center), 1 (right)
- **Y (Vertical)**: -1 (top), 0 (middle/center), 1 (bottom)

**Predefined Constants** (from `Align` static class):
```csharp
Align.TopLeft       = new(-1, -1)
Align.TopCenter     = new(0, -1)
Align.TopRight      = new(1, -1)
Align.MiddleLeft    = new(-1, 0)
Align.MiddleCenter  = new(0, 0)
Align.MiddleRight   = new(1, 0)
Align.BottomLeft    = new(-1, 1)
Align.BottomCenter  = new(0, 1)
Align.BottomRight   = new(1, 1)
```

**Usage in Layouts**:
- **HorizontalLayout**: Uses only Y component for vertical alignment of children
- **VerticalLayout**: Uses only X component for horizontal alignment of children
- **Unused component**: Can be any value (typically 0 by convention)

**Conversion Formula** (from -1..1 to 0..1 fraction):
```csharp
float fraction = (alignment + 1.0f) * 0.5f;
int offset = (int)((availableSpace - childSize) * fraction);
```

---

### 5. Rectangle<int> (Content Area)

**Purpose**: Represents the available space for child elements after subtracting padding and safe area.

**Type**: `Silk.NET.Maths.Rectangle<int>`

**Fields**:
- `Origin`: Vector2D<int> - Top-left corner of content area
- `Size`: Vector2D<int> - Width (X) and height (Y) of content area

**Calculation** (in Container.GetContentArea()):
```csharp
new Rectangle<int>(
    x: (int)TargetPosition.X + Padding.Left,
    y: (int)TargetPosition.Y + Padding.Top,
    width: Max(0, TargetSize.X - Padding.Left - Padding.Right),
    height: Max(0, TargetSize.Y - Padding.Top - Padding.Bottom)
)
```

**Usage**: Provided to `child.Measure()` and used to calculate child position/size constraints

---

### 6. Child Constraints (Rectangle<int>)

**Purpose**: Position and size bounds assigned to each child element.

**Type**: `Silk.NET.Maths.Rectangle<int>`

**Calculation Pattern** (HorizontalLayout):
```csharp
// Measure child to get preferred size
var measured = child.Measure(contentArea.Size);

// Calculate position based on alignment (or stretch)
var x = currentX; // Cumulative X position
var y = _stretchChildren 
    ? contentArea.Origin.Y
    : contentArea.Origin.Y + (int)((contentArea.Size.Y - measured.Y) * ((Alignment.Y + 1) / 2));

// Calculate size
var w = measured.X;
var h = _stretchChildren ? contentArea.Size.Y : measured.Y;

// Create and apply constraints
var constraints = new Rectangle<int>(x, y, w, h);
child.SetSizeConstraints(constraints);

// Advance to next child position
currentX += measured.X + (int)Spacing.X;
```

**Calculation Pattern** (VerticalLayout):
```csharp
// Measure child to get preferred size
var measured = child.Measure(contentArea.Size);

// Calculate size first (needed for position calculation)
var w = _stretchChildren ? contentArea.Size.X : measured.X;
var h = measured.Y;

// Calculate position based on alignment (or stretch)
var x = _stretchChildren
    ? contentArea.Origin.X
    : contentArea.Origin.X + (int)((contentArea.Size.X - w) * ((Alignment.X + 1) / 2));
var y = currentY; // Cumulative Y position

// Create and apply constraints
var constraints = new Rectangle<int>(x, y, w, h);
child.SetSizeConstraints(constraints);

// Advance to next child position
currentY += measured.Y + (int)Spacing.Y;
```

---

## Relationships

### Component Hierarchy
```
IRuntimeComponent
└── RuntimeComponent
    └── Transformable
        └── Element
            └── Container (provides padding, spacing, content area)
                ├── HorizontalLayout (arranges children horizontally)
                └── VerticalLayout (arranges children vertically)
```

### Data Flow
```
1. Layout receives SetSizeConstraints() call
   ↓
2. Container.OnSizeConstraintsChanged() invalidates layout
   ↓
3. Container.UpdateLayout() called synchronously
   ↓
4. HorizontalLayout/VerticalLayout.UpdateLayout() override executes:
   a. Get content area (bounds - padding - safeArea)
   b. For each child:
      - Call child.Measure(contentArea.Size)
      - Calculate child position using Alignment vector
      - Calculate child size (measured or stretched)
      - Call child.SetSizeConstraints(new Rectangle(x, y, w, h))
```

### Property Update Flow
```
1. Developer calls layout.SetAlignment(value)
   ↓
2. [ComponentProperty] source generator queues deferred update
   ↓
3. During next frame, layout.ApplyUpdates() executes
   ↓
4. Alignment property updated
   ↓
5. PropertyChanged event fires (if subscribed)
   ↓
6. Layout invalidation triggered (if layout is active)
   ↓
7. UpdateLayout() called on next Update cycle
```

---

## Migration Impact

### Breaking Changes

**HorizontalLayout**:
- `Alignment` property type changed: `float` → `Vector2D<float>`
- Property semantics changed: Single vertical alignment value → 2D vector (only Y component used)

**VerticalLayout**:
- `Alignment` property type changed: `float` → `Vector2D<float>`
- Property semantics changed: Single horizontal alignment value → 2D vector (only X component used)

### Deprecated Types

**HorizontalAlignment static class**:
- Still usable for individual axis constants
- `Align.Left`, `Align.Center`, `Align.Right` still available
- Consider using `Align.Left`, `Align.Center`, `Align.Right` instead for consistency

**VerticalAlignment static class**:
- Still usable for individual axis constants
- `Align.Top`, `Align.Center`, `Align.Bottom` still available
- Consider using `Align.Top`, `Align.Middle`, `Align.Bottom` instead for consistency

### Code Migration Examples

See [research.md](./research.md#migration-path) for detailed migration guidance.

---

## Validation Rules Summary

| Entity | Field | Validation | Enforced |
|--------|-------|------------|----------|
| HorizontalLayout | Alignment | Standard range: -1 to 1 (Y component) | No (allows any float) |
| VerticalLayout | Alignment | Standard range: -1 to 1 (X component) | No (allows any float) |
| Both Layouts | StretchChildren | Must be boolean | Yes (type system) |
| Both Layouts | Spacing | Must be Vector2D<float> | Yes (type system) |
| Both Layouts | Padding | Must be valid Padding struct | Yes (type system) |

No runtime validation is performed on alignment values. Values outside -1 to 1 are permitted for advanced use cases (e.g., overflow effects, parallax).

---

## State Diagram: Layout Update Cycle

```
[Inactive] 
    ↓ OnActivate()
[Active - Valid Layout]
    ↓ SetSizeConstraints() / Invalidate()
[Active - Invalid Layout]
    ↓ OnUpdate() / OnSizeConstraintsChanged()
[Active - Updating Layout]
    ↓ UpdateLayout() completes
[Active - Valid Layout]
    ↓ OnDeactivate()
[Inactive]
```

**State Transitions**:
- **Inactive → Active**: Component activation, initializes with valid layout state
- **Valid → Invalid**: Size constraints change, padding change, spacing change, alignment change, child collection change
- **Invalid → Updating**: Update cycle begins or synchronous update triggered
- **Updating → Valid**: Layout calculations complete, all children have updated constraints
- **Active → Inactive**: Component deactivation

---

## Performance Considerations

**Hot Path Optimizations**:
- `UpdateLayout()` is called frequently (on every size change)
- Alignment formula uses simple arithmetic (no branching except for StretchChildren check)
- Content area calculation uses target properties (handles deferred updates correctly)
- Child enumeration uses LINQ `ToArray()` to avoid multiple enumeration

**Allocation Profile**:
- One `Rectangle<int>` allocation per child per layout update (struct, stack allocation likely)
- One `Vector2D<int>` allocation for measured size per child (struct, stack allocation likely)
- Child array allocation (`ToArray()`) - necessary to avoid collection modification during iteration

**Potential Optimizations** (future):
- Cache child array if collection hasn't changed
- Early exit if no children (already implemented)
- Consider object pooling for large child collections (unlikely to be needed)
