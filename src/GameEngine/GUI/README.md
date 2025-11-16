# GUI System Documentation

## Overview

The Nexus.GameEngine GUI system provides a resolution-independent UI layout framework with anchor-based positioning and automatic child arrangement. It supports responsive designs that adapt from mobile (640x360) to 4K displays (3840x2160) and various aspect ratios.

## Core Components

### Element

Base class for all UI components. Provides:
- **Anchor-based positioning**: Position elements relative to parent using normalized coordinates (-1 to 1)
- **Size modes**: Fixed, Percentage, Stretch, Intrinsic
- **Min/Max constraints**: Enforce minimum and maximum sizes
- **Automatic layout**: Integration with parent layout containers

**Example - Fixed Size Element**:
```csharp
var element = new ElementTemplate
{
    AnchorPoint = new(0, 0),        // Center
    Position = new(960, 540),       // Screen center for 1920x1080
    Size = new(200, 100),
    SizeMode = SizeMode.Fixed,
    TintColor = new(1, 0, 0, 1)    // Red
};
```

**Example - Percentage Sizing**:
```csharp
var element = new ElementTemplate
{
    SizeMode = SizeMode.Percent,
    WidthPercentage = 80,           // 80% of parent width
    HeightPercentage = 60,          // 60% of parent height
    MinSize = new(400, 300),        // Minimum for small screens
    MaxSize = new(1200, 900)        // Maximum for large screens
};
```

### Layout Containers

#### VerticalLayout

Arranges children vertically with configurable spacing and alignment.

```csharp
var menu = new VerticalLayoutTemplate
{
    Padding = new Padding(20),
    Spacing = new(0, 15),          // 15px between children
    HorizontalAlignment = Align.Center,
    Children = new[]
    {
        new ElementTemplate { Size = new(200, 50) },  // Button 1
        new ElementTemplate { Size = new(200, 50) },  // Button 2
        new ElementTemplate { Size = new(200, 50) }   // Button 3
    }
};
```

#### HorizontalLayout

Arranges children horizontally with configurable spacing and alignment.

```csharp
var toolbar = new HorizontalLayoutTemplate
{
    Padding = new Padding(10),
    Spacing = new(10, 0),          // 10px between children
    VerticalAlignment = Align.Center,
    Children = toolButtons
};
```

#### GridLayout

Arranges children in a grid with configurable columns and aspect ratio.

```csharp
var inventory = new GridLayoutTemplate
{
    ColumnCount = 5,
    Spacing = new(5, 5),
    Padding = new Padding(10),
    MaintainCellAspectRatio = true,
    CellAspectRatio = 1.0f,        // Square cells
    Children = CreateInventorySlots(25)  // 5x5 grid
};
```

## Size Modes

### Fixed
Elements use explicit Width/Height properties. Does not respond to parent size changes.

### Percentage
Elements size as percentage of parent constraints. Responsive to parent resize.

### Stretch
Elements fill available space from parent constraints.

### Intrinsic
Elements size based on content (children bounds). Useful for containers that wrap content.

## Anchor-Based Positioning

AnchorPoint defines which point of the element aligns with Position in screen space:
- `(-1, -1)` = top-left
- `(0, 0)` = center
- `(1, 1)` = bottom-right

**Example - Anchored HUD Elements**:
```csharp
// Health bar - top-left
new ElementTemplate
{
    AnchorPoint = new(-1, -1),
    Position = new(20, 20),
    Size = new(200, 30)
}

// Mini-map - top-right  
new ElementTemplate
{
    AnchorPoint = new(1, -1),
    Position = new(1900, 20),      // 1920 - 20
    Size = new(200, 200)
}
```

## Safe Area Support

Layouts support safe-area margins for avoiding unsafe areas (notches, rounded corners, overscan):

```csharp
var layout = new VerticalLayoutTemplate
{
    SafeArea = new SafeArea(
        leftPercent: 0.05f,         // 5% left margin
        topPercent: 0.05f,          // 5% top margin (for notch)
        rightPercent: 0.05f,        // 5% right margin
        bottomPercent: 0.05f,       // 5% bottom margin
        minPixels: 20,              // Minimum 20px on all sides
        maxPixels: 100              // Maximum 100px on all sides
    )
};
```

Safe-area margins are added to base Padding automatically via `GetEffectivePadding()`.

## Layout System Architecture

### Constraint Propagation

Layouts use a top-down constraint propagation model:
1. Parent calls `child.SetSizeConstraints(Rectangle<int>)`
2. Child calculates size based on SizeMode and constraints
3. Child applies Min/Max size constraints
4. Child updates Position and Size
5. If child is a Layout, it propagates constraints to its children

### Invalidation and Recalculation

Layouts use lazy invalidation:
1. Property changes mark layout as invalid
2. `OnUpdate()` recalculates layout once per frame if invalid
3. Child collection changes automatically invalidate layout
4. Constraint changes trigger immediate recalculation

### Animation Prevention

Layout-affecting properties use `BeforeChange` to cancel animations and prevent layout thrashing:
- Padding
- Spacing
- SizeMode
- ColumnCount
- Alignment properties

Visual properties (TintColor, Opacity, Scale) can animate freely.

## Testing

The layout system can be tested using:
- **Unit tests**: Test individual layout algorithms (requires mocking infrastructure)
- **Integration tests**: Frame-based tests with pixel sampling in TestApp
- **Manual testing**: Run TestApp with different window sizes and aspect ratios

See `src/GameEngine/Testing/README.md` for comprehensive testing guidelines.

## Performance Considerations

- Layout recalculation is deferred to frame boundary (max once per frame)
- Constraint caching prevents redundant recalculations
- SafeArea calculations are lightweight (percentage-based with clamping)
- Layout algorithms are O(n) where n is number of children

Target: <1ms for typical UI hierarchies (up to 50 elements)

## Examples

See `specs/003-ui-layout-system/quickstart.md` for comprehensive examples including:
- Main menus
- Game HUDs
- Inventory grids
- Settings dialogs
- Common patterns and pitfalls
