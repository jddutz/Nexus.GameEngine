# Data Model: UI Layout System

**Feature**: `003-ui-layout-system`  
**Date**: 2025-11-04  
**Purpose**: Define entities, relationships, and state for resolution-independent UI layout system

## Core Entities

### 1. Element (Existing - Enhanced)

**Purpose**: Base UI component with position, size, and anchor-point-based positioning

**Fields**:
- `Position: Vector2D<float>` - Position in parent's coordinate space (pixels)
- `Size: Vector2D<float>` - Width and height in pixels
- `AnchorPoint: Vector2D<float>` - Normalized coordinate (-1 to 1) defining alignment point
- `Scale: Vector2D<float>` - Visual scaling factor (multiplier, default 1.0)
- `SizeMode: SizeMode` - How element determines its size (NEW)
- `WidthPercentage: float` - Width as percentage of parent (0-100) (NEW)
- `HeightPercentage: float` - Height as percentage of parent (0-100) (NEW)
- `MinSize: Vector2D<int>` - Minimum size constraints (NEW)
- `MaxSize: Vector2D<int>` - Maximum size constraints (NEW)
- `_sizeConstraints: Rectangle<int>` - Available space from parent (private, NEW)

**Relationships**:
- Inherits from `Transformable` (provides Position, Scale, Transform matrix)
- Implements `IRuntimeComponent` (lifecycle management)
- Contains `Children: List<IComponent>` (child components)
- Has `Parent: IComponent?` (parent component)

**State Transitions**:
- `Inactive` → `Active` (OnActivate called)
- `Active` → `LayoutDirty` (SetSizeConstraints called)
- `LayoutDirty` → `Active` (OnUpdate calculates layout)
- `Active` → `Inactive` (OnDeactivate called)

**Validation Rules**:
- `Size.X > 0 && Size.Y > 0` (positive dimensions)
- `AnchorPoint.X >= -1 && AnchorPoint.X <= 1` (valid anchor range)
- `AnchorPoint.Y >= -1 && AnchorPoint.Y <= 1` (valid anchor range)
- `MinSize <= Size <= MaxSize` (respects constraints)

---

### 2. Layout (New - Abstract Base)

**Purpose**: Abstract container that arranges child Elements according to layout algorithm

**Fields**:
- All fields from `Element` (inherits)
- `Padding: Padding` - Inner margins (left, top, right, bottom)
- `Spacing: Vector2D<float>` - Space between children (x=horizontal, y=vertical)
- `_needsLayout: bool` - Dirty flag indicating recalculation needed (private)

**Relationships**:
- Inherits from `Element`
- Overrides `OnUpdate()` to recalculate layout when dirty
- Overrides `SetSizeConstraints()` to invalidate layout on constraint changes

**State Transitions**:
- `Active` → `LayoutDirty` (child added/removed, property changed, constraints changed)
- `LayoutDirty` → `Calculating` (OnUpdate begins layout calculation)
- `Calculating` → `Active` (layout calculation completes, _needsLayout = false)

**Validation Rules**:
- `Children.All(c => c is Element)` (all children must be Elements)
- `Padding.Left >= 0 && Padding.Top >= 0 && Padding.Right >= 0 && Padding.Bottom >= 0` (non-negative padding)
- `Spacing.X >= 0 && Spacing.Y >= 0` (non-negative spacing)

---

### 3. VerticalLayout (New - Concrete)

**Purpose**: Arranges children vertically with configurable spacing and alignment

**Fields**:
- All fields from `Layout` (inherits)
- `HorizontalAlignment: HorizontalAlignment` - How to align children horizontally (Left, Center, Right, Stretch)

**Layout Algorithm**:
1. Calculate available height: `constraints.Height - Padding.Top - Padding.Bottom`
2. Calculate total required height: `sum(child.Size.Y) + (childCount - 1) * Spacing.Y`
3. For each child:
   - Position Y: `Padding.Top + currentY`
   - Position X: Based on HorizontalAlignment:
     - `Left`: `Padding.Left`
     - `Center`: `(constraints.Width - child.Size.X) / 2`
     - `Right`: `constraints.Width - Padding.Right - child.Size.X`
     - `Stretch`: `Padding.Left`, and set child width to available width
   - Propagate constraints to child: `Rectangle(childX, childY, availableWidth, childHeight)`
   - Increment currentY: `currentY += child.Size.Y + Spacing.Y`
4. Update own size: Width = constraints.Width, Height = totalRequiredHeight + padding

**Validation Rules**:
- If total child height exceeds available height, children may clip or overflow (no scrolling in MVP)

---

### 4. HorizontalLayout (New - Concrete)

**Purpose**: Arranges children horizontally with configurable spacing and alignment

**Fields**:
- All fields from `Layout` (inherits)
- `VerticalAlignment: VerticalAlignment` - How to align children vertically (Top, Center, Bottom, Stretch)

**Layout Algorithm**:
1. Calculate available width: `constraints.Width - Padding.Left - Padding.Right`
2. Calculate total required width: `sum(child.Size.X) + (childCount - 1) * Spacing.X`
3. For each child:
   - Position X: `Padding.Left + currentX`
   - Position Y: Based on VerticalAlignment:
     - `Top`: `Padding.Top`
     - `Center`: `(constraints.Height - child.Size.Y) / 2`
     - `Bottom`: `constraints.Height - Padding.Bottom - child.Size.Y`
     - `Stretch`: `Padding.Top`, and set child height to available height
   - Propagate constraints to child: `Rectangle(childX, childY, childWidth, availableHeight)`
   - Increment currentX: `currentX += child.Size.X + Spacing.X`
4. Update own size: Width = totalRequiredWidth + padding, Height = constraints.Height

**Validation Rules**:
- If total child width exceeds available width, children may clip or overflow (no scrolling in MVP)

---

### 5. GridLayout (New - Concrete)

**Purpose**: Arranges children in a grid pattern with configurable rows/columns

**Fields**:
- All fields from `Layout` (inherits)
- `ColumnCount: int` - Number of columns in grid
- `MaintainCellAspectRatio: bool` - Whether cells preserve aspect ratio
- `CellAspectRatio: float` - Width:Height ratio for cells (1.0 = square)

**Layout Algorithm**:
1. Calculate available space:
   - `availableWidth = constraints.Width - Padding.Left - Padding.Right`
   - `availableHeight = constraints.Height - Padding.Top - Padding.Bottom`
2. Calculate cell dimensions:
   - `cellWidth = (availableWidth - Spacing.X * (ColumnCount - 1)) / ColumnCount`
   - `cellHeight = MaintainCellAspectRatio ? cellWidth / CellAspectRatio : cellWidth`
3. For each child (row-major order):
   - Calculate row/column: `row = index / ColumnCount`, `col = index % ColumnCount`
   - Position: 
     - `x = Padding.Left + col * (cellWidth + Spacing.X)`
     - `y = Padding.Top + row * (cellHeight + Spacing.Y)`
   - Set child size: `child.Size = (cellWidth, cellHeight)`
   - Propagate constraints: `Rectangle(x, y, cellWidth, cellHeight)`
4. Update own size:
   - `rowCount = (childCount + ColumnCount - 1) / ColumnCount`
   - `Height = Padding.Top + Padding.Bottom + rowCount * cellHeight + (rowCount - 1) * Spacing.Y`
   - `Width = constraints.Width`

**Validation Rules**:
- `ColumnCount > 0` (at least one column)
- `CellAspectRatio > 0` (positive aspect ratio)

---

## Supporting Types

### 6. Padding (New - Struct)

**Purpose**: Define inner margins for layout containers

**Fields**:
- `Left: int` - Left margin in pixels
- `Top: int` - Top margin in pixels
- `Right: int` - Right margin in pixels
- `Bottom: int` - Bottom margin in pixels

**Constructors**:
- `Padding(int all)` - All sides equal
- `Padding(int horizontal, int vertical)` - Left/Right = horizontal, Top/Bottom = vertical
- `Padding(int left, int top, int right, int bottom)` - Individual sides

**Validation Rules**:
- `Left >= 0 && Top >= 0 && Right >= 0 && Bottom >= 0` (non-negative values)

---

### 7. SizeMode (New - Enum)

**Purpose**: Define how element determines its size

**Values**:
- `Fixed` - Use explicit Width/Height properties
- `Intrinsic` - Size determined by content (text, texture, children)
- `Stretch` - Fill available space from parent constraints
- `Percentage` - Size as percentage of parent dimensions

---

### 8. HorizontalAlignment (New - Enum)

**Purpose**: Define horizontal alignment of children in layout

**Values**:
- `Left` - Align to left edge
- `Center` - Center horizontally
- `Right` - Align to right edge
- `Stretch` - Stretch to fill horizontal space

---

### 9. VerticalAlignment (New - Enum)

**Purpose**: Define vertical alignment of children in layout

**Values**:
- `Top` - Align to top edge
- `Center` - Center vertically
- `Bottom` - Align to bottom edge
- `Stretch` - Stretch to fill vertical space

---

### 10. Rectangle<T> (Existing - Used for Constraints)

**Purpose**: Define rectangular area (origin + dimensions)

**Fields**:
- `Origin: Vector2D<T>` - Top-left corner position
- `Size: Vector2D<T>` - Width and height

**Usage**: Represents size constraints passed from parent to child during layout propagation

---

## Entity Relationships Diagram

```
IRuntimeComponent
       ↑
       |
  Transformable
       ↑
       |
    Element ────────────────┐
   (enhanced)               │
       ↑                    │
       |                    │
     Layout ────────────────┤
   (abstract)               │
       ↑                    │
       |                    │
   ┌───┴────┬──────┐        │
   |        |      |        │
Vertical  Horiz  Grid       │
Layout    Layout Layout     │
                            │
                      Contains (Children)
```

**Key Relationships**:
- Inheritance: Layout → Element → Transformable → IRuntimeComponent
- Composition: Layout contains multiple Element children
- Constraint Propagation: Parent calls `child.SetSizeConstraints(Rectangle<int>)`
- Template Generation: Each concrete class gets auto-generated Template record

---

## State Machine: Layout Lifecycle

```
┌─────────────────────────────────────────────┐
│           Component Created                 │
│         (from template)                     │
└─────────────┬───────────────────────────────┘
              │
              ↓
     ┌────────────────┐
     │   Inactive     │
     └────────┬───────┘
              │ OnActivate()
              ↓
     ┌────────────────┐
     │     Active     │◄────┐
     └────────┬───────┘     │
              │             │
    Property changed or     │
    Constraints changed     │
              │             │
              ↓             │
     ┌────────────────┐     │
     │  LayoutDirty   │     │
     │ (_needsLayout) │     │
     └────────┬───────┘     │
              │             │
        OnUpdate()          │
              │             │
              ↓             │
     ┌────────────────┐     │
     │  Calculating   │     │
     │RecalculateLayout│    │
     └────────┬───────┘     │
              │             │
    Layout complete         │
    (_needsLayout=false)────┘
              │
              ↓
     ┌────────────────┐
     │   Inactive     │
     └────────────────┘
           OnDeactivate()
```

---

## Data Flow: Constraint Propagation

```
Window Resize Event
        │
        ↓
  Viewport Size Updated
        │
        ↓
  Root Element.SetSizeConstraints(viewportRect)
        │
        ↓
  Root Element.OnSizeConstraintsChanged()
        │
        ├─> Calculate own size (based on SizeMode)
        │
        ├─> Update Position (based on AnchorPoint)
        │
        └─> For each child:
                │
                ├─> Calculate child constraints (based on layout algorithm)
                │
                └─> child.SetSizeConstraints(childConstraints)
                        │
                        └─> [RECURSE] Child propagates to its children
```

---

## Usage Examples

### Example 1: Centered Dialog with Vertical Menu

```csharp
var dialog = new LayoutTemplate
{
    SizeMode = SizeMode.Fixed,
    Width = 400,
    Height = 300,
    AnchorPoint = new(0, 0), // Center of dialog aligns with center of screen
    Position = new(960, 540), // 1920x1080 center
    Padding = new Padding(20),
    Children = new[]
    {
        new VerticalLayoutTemplate
        {
            Spacing = new(0, 10),
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = new[]
            {
                new ElementTemplate { Size = new(200, 50), TintColor = new(1, 0, 0, 1) }, // Red button
                new ElementTemplate { Size = new(200, 50), TintColor = new(0, 1, 0, 1) }, // Green button
                new ElementTemplate { Size = new(200, 50), TintColor = new(0, 0, 1, 1) }, // Blue button
            }
        }
    }
};
```

### Example 2: HUD with Anchored Elements

```csharp
var hud = new ElementTemplate
{
    SizeMode = SizeMode.Stretch, // Fill entire viewport
    Children = new[]
    {
        // Health bar - top-left
        new ElementTemplate
        {
            AnchorPoint = new(-1, -1),
            Position = new(20, 20),
            Size = new(200, 30),
            TintColor = new(1, 0, 0, 1)
        },
        
        // Mini-map - top-right
        new ElementTemplate
        {
            AnchorPoint = new(1, -1),
            Position = new(1900, 20), // 1920 - 20
            Size = new(200, 200),
            TintColor = new(0, 1, 1, 1)
        },
        
        // Action bar - bottom-center
        new HorizontalLayoutTemplate
        {
            AnchorPoint = new(0, 1),
            Position = new(960, 1060), // Center X, Bottom Y
            Spacing = new(10, 0),
            Children = CreateActionButtons(5)
        }
    }
};
```

### Example 3: Responsive Grid (Inventory)

```csharp
var inventory = new GridLayoutTemplate
{
    SizeMode = SizeMode.Percentage,
    WidthPercentage = 80,
    HeightPercentage = 80,
    AnchorPoint = new(0, 0),
    Position = new(960, 540), // Centered
    ColumnCount = 5,
    Spacing = new(5, 5),
    Padding = new Padding(10),
    MaintainCellAspectRatio = true,
    CellAspectRatio = 1.0f, // Square cells
    Children = CreateInventorySlots(25) // 5x5 grid
};
```

---

## Validation Summary

### Compile-Time Validation (via Source Generators)
- Template properties must match backing fields marked with `[TemplateProperty]`
- ComponentProperty interpolation types must be supported or implement `IInterpolatable<T>`

### Runtime Validation (in constructors/property setters)
- Positive dimensions (`Size.X > 0 && Size.Y > 0`)
- Valid anchor range (`-1 <= AnchorPoint <= 1`)
- Non-negative padding/spacing
- Positive column count for GridLayout
- Size constraints respected (`MinSize <= Size <= MaxSize`)

### Layout Validation (during RecalculateLayout)
- Children exceed available space → clip/overflow (no error, expected behavior)
- Invalid child types (non-Element) → log warning, skip child
- Circular dependencies (layout contains itself) → prevented by component hierarchy

---

## Open Questions for Implementation

1. **Intrinsic sizing**: Should Element base class calculate intrinsic size by querying children bounds, or should each subclass (TextElement, etc.) override?
   - **Answer**: Both. Base class provides default implementation (query children), subclasses override for content-specific sizing (text measurement).

2. **Constraint caching**: Should we cache constraints and only recalculate when they change?
   - **Answer**: Yes, compare incoming constraints with cached `_sizeConstraints`, only trigger `OnSizeConstraintsChanged()` if different.

3. **Animation handling**: Should we add explicit animation lifecycle hooks for layout-affecting properties?
   - **Answer**: For MVP, avoid animating layout-affecting properties (Size, Padding). Future: add `OnAnimationEnded` hook to trigger layout recalculation.

4. **Overflow behavior**: What happens when children exceed container bounds?
   - **Answer**: Clip by default (children render beyond container bounds are cut off). Scrolling deferred to post-MVP.

---

## Implementation Priority

**Phase 1 (MVP - P1)**:
1. `Padding` struct
2. `SizeMode`, `HorizontalAlignment`, `VerticalAlignment` enums
3. Enhance `Element` with SizeMode, constraints, min/max size
4. `Layout` abstract base class
5. `VerticalLayout` concrete implementation
6. `HorizontalLayout` concrete implementation

**Phase 2 (Post-MVP - P2)**:
7. `GridLayout` concrete implementation
8. Aspect ratio preservation for grids
9. Safe-area margin support

**Phase 3 (Future - P3)**:
10. Intrinsic sizing for TextElement
11. Animation lifecycle hooks for layout properties
12. Performance optimizations (layout caching, visibility culling)

**Testing** (Concurrent with all phases):
- Unit tests for each layout algorithm
- Integration tests with pixel sampling for visual verification
- Resize/aspect ratio tests across various viewport dimensions
