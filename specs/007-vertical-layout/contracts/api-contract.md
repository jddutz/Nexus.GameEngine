# API Contract: Directional Layout Components (VerticalLayout & HorizontalLayout)

**Feature**: VerticalLayout and HorizontalLayout with property-based design  
**Date**: November 16-17, 2025  
**Version**: 1.0.0

## Overview

This document defines the public API contract for the VerticalLayout and HorizontalLayout components. These components arrange child UI elements along their respective axes using a property-based design with four core properties (ItemHeight/ItemWidth, Spacing, ItemSpacing) plus inherited Alignment for flexible spacing and sizing control.

## Component API

### VerticalLayout Class

**Namespace**: `Nexus.GameEngine.GUI.Layout`  
**Assembly**: `Nexus.GameEngine`  
**Visibility**: `public`

```csharp
namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children vertically using
/// property-based configuration for flexible spacing and sizing.
/// </summary>
public partial class VerticalLayout : Container
{
    /// <summary>
    /// Gets or sets the fixed spacing between children in pixels.
    /// When set, overrides automatic spacing. When null, uses Spacing mode.
    /// Default: null
    /// </summary>
    public uint? ItemSpacing { get; set; }

    /// <summary>
    /// Gets or sets the automatic spacing distribution mode.
    /// Used when ItemSpacing is null. Default: Justified
    /// </summary>
    public SpacingMode Spacing { get; set; }

    /// <summary>
    /// Gets or sets the fixed height for each child item in pixels.
    /// When set, overrides child heights. When null, uses child measured heights.
    /// Default: null
    /// </summary>
    public uint? ItemHeight { get; set; }

    /// <summary>
    /// Initializes a new instance of the VerticalLayout component.
    /// </summary>
    /// <param name="descriptorManager">The descriptor manager for Vulkan rendering resources.</param>
    public VerticalLayout(IDescriptorManager descriptorManager);

    // Inherited from Container:
    // - Padding: Padding around content area
    // - Alignment: Alignment for positioning (Y component used for vertical distribution)
    // - SafeArea: Safe-area margins for device notches/rounded corners

    // Inherited from ILayout:
    // - void Invalidate(): Marks layout as needing recalculation
    // - void OnChildSizeChanged(IComponent child, Vector2D<int> oldValue): Called when child size changes
}
```

### HorizontalLayout Class

**Namespace**: `Nexus.GameEngine.GUI.Layout`  
**Assembly**: `Nexus.GameEngine`  
**Visibility**: `public`

```csharp
namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// A layout component that arranges its children horizontally using
/// property-based configuration for flexible spacing and sizing.
/// </summary>
public partial class HorizontalLayout : Container
{
    /// <summary>
    /// Gets or sets the fixed spacing between children in pixels.
    /// When set, overrides automatic spacing. When null, uses Spacing mode.
    /// Default: null
    /// </summary>
    public uint? ItemSpacing { get; set; }

    /// <summary>
    /// Gets or sets the automatic spacing distribution mode.
    /// Used when ItemSpacing is null. Default: Justified
    /// </summary>
    public SpacingMode Spacing { get; set; }

    /// <summary>
    /// Gets or sets the fixed width for each child item in pixels.
    /// When set, overrides child widths. When null, uses child measured widths.
    /// Default: null
    /// </summary>
    public uint? ItemWidth { get; set; }

    /// <summary>
    /// Initializes a new instance of the HorizontalLayout component.
    /// </summary>
    /// <param name="descriptorManager">The descriptor manager for Vulkan rendering resources.</param>
    public HorizontalLayout(IDescriptorManager descriptorManager);

    // Inherited from Container (same as VerticalLayout)
}
```

### SpacingMode Enumeration

**Namespace**: `Nexus.GameEngine.GUI.Layout`  
**Assembly**: `Nexus.GameEngine`  
**Visibility**: `public`

```csharp
namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// Defines the spacing distribution algorithm when ItemSpacing is null.
/// Each mode defines how remaining space is distributed between children.
/// </summary>
public enum SpacingMode
{
    /// <summary>
    /// Space between items only (first item at start, last item at end).
    /// Equivalent to CSS justify-content: space-between.
    /// </summary>
    Justified = 0,

    /// <summary>
    /// Space before, between, and after items (equal spacing everywhere).
    /// Equivalent to CSS justify-content: space-evenly.
    /// </summary>
    Distributed = 1
}
```

## Template API

### VerticalLayoutTemplate Record

**Namespace**: `Nexus.GameEngine.GUI.Layout`  
**Assembly**: `Nexus.GameEngine`  
**Visibility**: `public`  
**Generated**: By TemplateGenerator source generator

```csharp
namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// Template record for declarative VerticalLayout configuration.
/// </summary>
public record VerticalLayoutTemplate : ContainerTemplate
{
    /// <summary>
    /// Gets or sets the fixed spacing between children in pixels.
    /// Default: null (uses Spacing mode)
    /// </summary>
    public uint? ItemSpacing { get; init; }

    /// <summary>
    /// Gets or sets the automatic spacing distribution mode.
    /// Default: Justified
    /// </summary>
    public SpacingMode? Spacing { get; init; }

    /// <summary>
    /// Gets or sets the fixed item height in pixels.
    /// Default: null (use intrinsic sizing)
    /// </summary>
    public uint? ItemHeight { get; init; }

    // Inherited from ContainerTemplate:
    // - Padding? Padding { get; init; }
    // - Alignment? Alignment { get; init; }
    // - SafeArea? SafeArea { get; init; }
}
```

### HorizontalLayoutTemplate Record

**Namespace**: `Nexus.GameEngine.GUI.Layout`  
**Assembly**: `Nexus.GameEngine`  
**Visibility**: `public`  
**Generated**: By TemplateGenerator source generator

```csharp
namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// Template record for declarative HorizontalLayout configuration.
/// </summary>
public record HorizontalLayoutTemplate : ContainerTemplate
{
    /// <summary>
    /// Gets or sets the fixed spacing between children in pixels.
    /// Default: null (uses Spacing mode)
    /// </summary>
    public uint? ItemSpacing { get; init; }

    /// <summary>
    /// Gets or sets the automatic spacing distribution mode.
    /// Default: Justified
    /// </summary>
    public SpacingMode? Spacing { get; init; }

    /// <summary>
    /// Gets or sets the fixed item width in pixels.
    /// Default: null (use intrinsic sizing)
    /// </summary>
    public uint? ItemWidth { get; init; }

    // Inherited from ContainerTemplate (same as VerticalLayoutTemplate)
}
```

## Behavioral Contract

### Child Size Determination

Directional layouts use a priority-based algorithm to determine each child's size:

**Priority Order**:
1. **ItemHeight/ItemWidth override**: If set, use this value
2. **Child's intrinsic size**: Call `child.Measure(availableSize)`
3. **Fallback**: Use default size (50px) for zero-size children

**Pseudocode**:
```csharp
uint DetermineChildSize(IUserInterfaceElement child)
{
    // Override takes precedence
    if (ItemHeight.HasValue) // or ItemWidth for HorizontalLayout
        return ItemHeight.Value;
    
    // Measure child
    var measured = child.Measure(availableSize);
    if (measured.Height > 0) // or Width for HorizontalLayout
        return (uint)measured.Height;
    
    // Fallback for zero-size children
    return 50;
}
```

**Examples**:

| Scenario | ItemHeight | Measure() | Result |
|----------|-----------|-----------|--------|
| Child with size | null | 80 | 80 (uses measured) |
| Child without size | null | 0 | 50 (uses fallback) |
| ItemHeight set | 60 | 80 | 60 (override) |
| ItemHeight set | 60 | 0 | 60 (override) |

### Property-Based Layout Behaviors

#### Fixed Spacing (ItemSpacing set)

**Behavior**: Children are positioned with fixed spacing between them, remaining space distributed based on Alignment.

**Guarantees**:
- Spacing between children is exactly ItemSpacing
- Remaining space distributed using Alignment.Y (-1=top, 0=center, 1=bottom)
- Children maintain their determined sizes

**Formula (VerticalLayout)**:
```
totalHeight = sum(child[*].Height) + ItemSpacing * (childCount - 1)
remainingSpace = contentArea.Height - totalHeight
startY = contentArea.Origin.Y + remainingSpace * ((Alignment.Y + 1.0f) / 2.0f)
child[i].Y = startY + sum(child[0..i-1].Height) + ItemSpacing * i
```

#### Justified Spacing (SpacingMode.Justified)

**Behavior**: Space between children only (first at start, last at end).

**Guarantees**:
- First child starts at contentArea.Origin.Y
- Last child ends at contentArea.Origin.Y + contentArea.Height - lastChild.Height
- Equal spacing between children

**Formula (VerticalLayout)**:
```
childHeight = sum(child[*].Height)
availableSpace = contentArea.Height - childHeight
spacing = availableSpace / (childCount - 1)
child[i].Y = contentArea.Origin.Y + sum(child[0..i-1].Height) + spacing * i
```

#### Distributed Spacing (SpacingMode.Distributed)

**Behavior**: Equal spacing before, between, and after children.

**Guarantees**:
- Spacing above first child equals spacing between children equals spacing below last child
- Total spacing distributed evenly

**Formula (VerticalLayout)**:
```
childHeight = sum(child[*].Height)
availableSpace = contentArea.Height - childHeight
spacing = availableSpace / (childCount + 1)
child[i].Y = contentArea.Origin.Y + spacing * (i + 1) + sum(child[0..i-1].Height)
```

### Invalidation Contract

**Triggers**: Layout is invalidated (recalculated on next OnUpdate) when:
- VerticalLayoutMode property changes
- Container size changes (SetSizeConstraints called)
- Child is added or removed (ChildCollectionChanged event)
- Padding property changes
- Spacing property changes
- ItemHeight property changes
- Child size changes (if SizeMode = FitContent)

**Timing**: Layout recalculation occurs during OnUpdate() phase, not immediately on invalidation.

**Guarantee**: Layout is recalculated within 1 frame update cycle after invalidation.

### Child Measurement Contract

**Behavior**: For each child (except in Justified mode):
1. VerticalLayout calls `child.Measure(contentArea.Size)` to get desired size
2. Child returns preferred height given available width
3. If ItemHeight > 0, ItemHeight overrides measured height

**Child Responsibility**: Children position themselves within constraints using their AnchorPoint/Alignment properties.

**Layout Responsibility**: VerticalLayout only controls:
- Vertical position (Y coordinate)
- Height allocation
- Width is always full content area width (child handles horizontal layout internally)

## Event Contract

### PropertyChanged Events

Generated properties (VerticalLayoutMode, ItemHeight) raise PropertyChanged events when modified:

```csharp
public event PropertyChangedEventHandler? PropertyChanged;
```

**Event Args**: Standard PropertyChangedEventArgs with property name.

**Timing**: Raised after value change, before layout invalidation.

### Animation Events

Generated properties support animation events:

```csharp
public event EventHandler<PropertyAnimationEventArgs>? AnimationStarted;
public event EventHandler<PropertyAnimationEventArgs>? AnimationEnded;
```

**Note**: VerticalLayoutMode is an enum and does not support interpolation (duration parameter ignored).

## Constraints and Preconditions

### Preconditions

- VerticalLayout must be activated (OnActivate called) before layout updates occur
- Children must implement IUserInterfaceElement interface to be positioned
- Descriptor manager must be provided via constructor

### Invariants

- Content area width = TargetSize.X - Padding.Left - Padding.Right (clamped to >= 0)
- Content area height = TargetSize.Y - Padding.Top - Padding.Bottom (clamped to >= 0)
- Layout mode is always one of five valid enum values
- ItemHeight is always >= 0

### Postconditions (after UpdateLayout)

- Each child has SetSizeConstraints called exactly once
- _isLayoutInvalid flag is cleared
- All children receive constraints with:
  - Width = content area width
  - Height determined by layout mode algorithm
  - Position determined by layout mode algorithm

## Thread Safety

**Not Thread-Safe**: VerticalLayout is designed for single-threaded use within the game engine's update loop.

**Concurrent Access**: Multiple threads modifying properties or children concurrently will result in undefined behavior.

**Recommendation**: All VerticalLayout operations should occur on the main engine thread.

## Backward Compatibility

### Version 1.0.0 Guarantees

- VerticalLayoutMode.StackedTop is the default and matches existing VerticalLayout behavior
- ItemHeight property is retained and functional
- No breaking changes to Container or Element APIs
- Existing code using VerticalLayout continues working without modification

### Future Compatibility

**Stable API Surface**:
- VerticalLayoutMode enumeration values (will not be removed or reordered)
- VerticalLayout public properties and methods
- Layout mode algorithms (behaviors guaranteed)

**May Change**:
- Internal implementation details (UpdateLayout algorithm optimization)
- Performance characteristics (may improve)
- Additional layout modes may be added (new enum values)

## Example Usage

### Programmatic Creation

```csharp
var layout = new VerticalLayout(descriptorManager)
{
    VerticalLayoutMode = VerticalLayoutMode.SpacedEqually,
    Padding = new Padding(10),
    Spacing = new Vector2D<float>(0, 5)
};

// Add children
var button1 = new Button(descriptorManager) { Size = new(200, 40) };
var button2 = new Button(descriptorManager) { Size = new(200, 40) };
var button3 = new Button(descriptorManager) { Size = new(200, 40) };

layout.AddChild(button1);
layout.AddChild(button2);
layout.AddChild(button3);

// Activate and set constraints
contentManager.Activate(layout);
layout.SetSizeConstraints(new Rectangle<int>(0, 0, 300, 600));
```

### Template-Based Creation

```csharp
var template = new VerticalLayoutTemplate
{
    VerticalLayoutMode = VerticalLayoutMode.StackedMiddle,
    Padding = new Padding(10, 10, 10, 10),
    Spacing = new Vector2D<float>(0, 8),
    Width = 300,
    Height = 400,
    Children = new Template[]
    {
        new ButtonTemplate { Width = 280, Height = 50, Text = "Start Game" },
        new ButtonTemplate { Width = 280, Height = 50, Text = "Options" },
        new ButtonTemplate { Width = 280, Height = 50, Text = "Exit" }
    }
};

var layout = componentFactory.CreateInstance<VerticalLayout>(template);
contentManager.Activate(layout);
```

## Testing Contract

### Unit Test Coverage

**Minimum Coverage**: 80% code coverage for VerticalLayout class

**Required Tests**:
- Each layout mode with known child sizes
- Edge cases: empty children, single child, overflow
- Property changes trigger invalidation
- ItemHeight override behavior
- Padding and spacing calculations

### Integration Test Coverage

**Required Scenarios**:
- Visual validation of all five layout modes
- Container resize responsiveness
- Nested VerticalLayouts
- Dynamic child addition/removal
- Different child SizeModes

## Deprecation Policy

**ItemHeight Property**: Currently supported but considered legacy. Recommended migration is to use child SizeMode.Fixed instead.

**Timeline**: No removal planned; maintained for backward compatibility indefinitely.

## Version History

- **1.0.0** (November 16, 2025): Initial API contract definition
