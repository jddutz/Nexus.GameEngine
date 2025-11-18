# Quickstart: Directional Layout Components (VerticalLayout & HorizontalLayout)

**Feature**: VerticalLayout and HorizontalLayout with property-based design  
**Date**: November 16-17, 2025  
**Audience**: Game developers using Nexus.GameEngine

## Introduction

The `VerticalLayout` and `HorizontalLayout` components arrange child UI elements along their respective axes using a property-based design with four core properties (ItemHeight/ItemWidth, Spacing, ItemSpacing) plus inherited Alignment for flexible spacing and sizing control. This design provides greater flexibility than enum-based modes, allowing combinations like centered fixed-spacing layouts or uniform-height automatic spacing.

## Prerequisites

- Nexus.GameEngine project with UI components configured
- Basic understanding of the component-based architecture
- Familiarity with template-based component creation

## Child Sizing Strategies

Directional layouts provide flexible child sizing with three strategies:

### Strategy 1: Use Child's Intrinsic Size (Default)
Children that return a size from `Measure()` use their preferred size:
```csharp
new ButtonTemplate { Width = 200, Height = 50 }  // Uses 50px height/width
```

### Strategy 2: Fixed Size for All Children (ItemHeight/ItemWidth)
Override all child sizes uniformly:
```csharp
new VerticalLayoutTemplate
{
    ItemHeight = 60,  // All children forced to 60px height
    Children = [...]  // No need to specify child heights
}
```

### Strategy 3: Fallback for Unsized Children
If a child returns 0 from `Measure()` and ItemHeight/ItemWidth is not set, a fallback size (50px) is used automatically.

## Basic Usage

### 1. Simple Vertical Menu (Fixed Spacing, Top-Aligned)

The most common use case: stack buttons vertically from the top with fixed spacing.

```csharp
var menuTemplate = new VerticalLayoutTemplate
{
    ItemSpacing = 10,  // 10px between buttons
    Alignment = Alignment.TopCenter,  // Top-aligned (Y = -1)
    Padding = new Padding(20),
    Width = 300,
    Height = 400,
    Children = new Template[]
    {
        new ButtonTemplate { Width = 260, Height = 50, Text = "New Game" },
        new ButtonTemplate { Width = 260, Height = 50, Text = "Load Game" },
        new ButtonTemplate { Width = 260, Height = 50, Text = "Settings" },
        new ButtonTemplate { Width = 260, Height = 50, Text = "Exit" }
    }
};

var menu = componentFactory.CreateInstance<VerticalLayout>(menuTemplate);
contentManager.Activate(menu);
```

**Result**: Four buttons stacked from top with 10px spacing, 20px padding around edges.

### 2. Centered Dialog Buttons (Fixed Spacing, Center-Aligned)

Center action buttons vertically in a dialog box with fixed spacing.

```csharp
var dialogButtonsTemplate = new VerticalLayoutTemplate
{
    ItemSpacing = 12,  // 12px between buttons
    Alignment = Alignment.Center,  // Center-aligned (Y = 0)
    Width = 400,
    Height = 200,
    Children = new Template[]
    {
        new ButtonTemplate { Width = 200, Height = 40, Text = "Confirm" },
        new ButtonTemplate { Width = 200, Height = 40, Text = "Cancel" }
    }
};
```

**Result**: Two buttons centered vertically in the 200px height container with 12px spacing.

### 3. Bottom Navigation Bar (Fixed Spacing, Bottom-Aligned)

Align navigation buttons to the bottom of the screen with fixed spacing.

```csharp
var navBarTemplate = new VerticalLayoutTemplate
{
    ItemSpacing = 8,  // 8px between buttons
    Alignment = Alignment.BottomCenter,  // Bottom-aligned (Y = 1)
    Padding = new Padding(0, 0, 0, 20),  // 20px bottom padding
    SizeMode = SizeMode.Absolute,  // Fill parent
    Children = new Template[]
    {
        new ButtonTemplate { Width = 300, Height = 50, Text = "Back" },
        new ButtonTemplate { Width = 300, Height = 50, Text = "Next" }
    }
};
```

**Result**: Buttons aligned to bottom edge with 8px spacing, stacking upward.

### 4. Evenly Spaced Menu Items (Distributed Spacing)

Distribute menu items evenly across available space with equal spacing around each item.

```csharp
var spacedMenuTemplate = new VerticalLayoutTemplate
{
    Spacing = SpacingMode.Distributed,  // Space before/between/after items
    Padding = new Padding(30),
    Width = 350,
    Height = 500,
    Children = new Template[]
    {
        new ButtonTemplate { Width = 290, Height = 60, Text = "Campaign" },
        new ButtonTemplate { Width = 290, Height = 60, Text = "Multiplayer" },
        new ButtonTemplate { Width = 290, Height = 60, Text = "Challenges" },
        new ButtonTemplate { Width = 290, Height = 60, Text = "Options" }
    }
};
```

**Result**: Four buttons distributed with equal spacing above, below, and between each button.

### 5. Space-Between Menu Items (Justified Spacing)

Distribute menu items with space between them only (first at start, last at end).

```csharp
var justifiedMenuTemplate = new VerticalLayoutTemplate
{
    Spacing = SpacingMode.Justified,  // Space between items only
    Padding = new Padding(30),
    Width = 350,
    Height = 500,
    Children = new Template[]
    {
        new ButtonTemplate { Width = 290, Height = 60, Text = "Campaign" },
        new ButtonTemplate { Width = 290, Height = 60, Text = "Multiplayer" },
        new ButtonTemplate { Width = 290, Height = 60, Text = "Challenges" },
        new ButtonTemplate { Width = 290, Height = 60, Text = "Options" }
    }
};
```

**Result**: Four buttons with equal spacing between them, first button at top, last at bottom.

## Programmatic Creation

If you prefer creating components programmatically instead of using templates:

```csharp
// Create layout
var layout = new VerticalLayout(descriptorManager);
layout.ItemSpacing = 5;  // 5px between children
layout.Spacing = SpacingMode.Justified;  // Used when ItemSpacing is null
layout.Alignment = Alignment.TopLeft;  // Top-aligned
layout.SetPadding(new Padding(15));
layout.SetSize(new Vector2D<int>(300, 600));

// Create and add children
var button1 = new Button(descriptorManager);
button1.SetSize(new Vector2D<int>(270, 50));
button1.SetText("Option 1");

var button2 = new Button(descriptorManager);
button2.SetSize(new Vector2D<int>(270, 50));
button2.SetText("Option 2");

layout.AddChild(button1);
layout.AddChild(button2);

// Activate
contentManager.Activate(layout);
```

## Dynamic Updates

### Changing Layout Mode at Runtime

```csharp
// Animate between spacing modes (discrete change, no interpolation)
layout.Spacing = SpacingMode.Distributed;

// Change fixed spacing dynamically
layout.ItemSpacing = 20;  // Layout will invalidate and recalculate
```

### Adding Children Dynamically

```csharp
var newButton = new Button(descriptorManager);
newButton.SetSize(new Vector2D<int>(200, 40));
newButton.SetText("Dynamic Button");

layout.AddChild(newButton);
// Layout automatically invalidates and repositions all children
```

### Removing Children

```csharp
var childToRemove = layout.GetChildren<Button>().FirstOrDefault();
if (childToRemove != null)
{
    layout.RemoveChild(childToRemove);
    // Layout automatically invalidates and repositions remaining children
}
```

## Advanced Configuration

### Nested VerticalLayouts

Create complex layouts by nesting VerticalLayouts:

```csharp
var outerLayout = new VerticalLayoutTemplate
{
    VerticalLayoutMode = VerticalLayoutMode.StackedTop,
    Padding = new Padding(20),
    Spacing = new Vector2D<float>(0, 20),
    Width = 400,
    Height = 600,
    Children = new Template[]
    {
        new TextElementTemplate { Text = "Main Menu", Height = 40 },
        
        // Nested VerticalLayout for buttons
        new VerticalLayoutTemplate
        {
            VerticalLayoutMode = VerticalLayoutMode.SpacedEqually,
            Height = 300,
            Children = new Template[]
            {
                new ButtonTemplate { Width = 360, Height = 50, Text = "Start" },
                new ButtonTemplate { Width = 360, Height = 50, Text = "Options" },
                new ButtonTemplate { Width = 360, Height = 50, Text = "Quit" }
            }
        },
        
        new TextElementTemplate { Text = "Version 1.0", Height = 30 }
    }
};
```

### Using ItemHeight for Fixed-Size Items

Override child heights with ItemHeight property:

```csharp
var fixedHeightLayout = new VerticalLayoutTemplate
{
    ItemHeight = 60,  // All children forced to 60px height
    ItemSpacing = 0,  // No spacing
    Width = 300,
    Height = 240,
    Children = new Template[]
    {
        new ButtonTemplate { Text = "Button 1" },  // Will be 60px tall
        new ButtonTemplate { Text = "Button 2" },  // Will be 60px tall
        new ButtonTemplate { Text = "Button 3" },  // Will be 60px tall
        new ButtonTemplate { Text = "Button 4" }   // Will be 60px tall
    }
};
```

### Safe Area Support

Handle device notches and rounded corners:

```csharp
var safeLayout = new VerticalLayoutTemplate
{
    VerticalLayoutMode = VerticalLayoutMode.StackedTop,
    SafeArea = new SafeArea(top: 0.05f, bottom: 0.05f),  // 5% top/bottom margins
    Padding = new Padding(10),
    // Children...
};
```

SafeArea margins are added to Padding automatically.

## Common Patterns

### Form Layout

```csharp
new VerticalLayoutTemplate
{
    ItemSpacing = 15,  // 15px between form elements
    Alignment = Alignment.TopLeft,  // Top-aligned
    Padding = new Padding(20),
    Children = new Template[]
    {
        new TextElementTemplate { Text = "Username:", Height = 30 },
        new TextInputTemplate { Width = 300, Height = 40 },
        new TextElementTemplate { Text = "Password:", Height = 30 },
        new TextInputTemplate { Width = 300, Height = 40 },
        new ButtonTemplate { Width = 300, Height = 50, Text = "Login" }
    }
};
```

### Loading Screen

```csharp
new VerticalLayoutTemplate
{
    ItemSpacing = 20,  // 20px between elements
    Alignment = Alignment.Center,  // Center-aligned
    SizeMode = SizeMode.Absolute,  // Fill screen
    Children = new Template[]
    {
        new ImageTemplate { Width = 200, Height = 200 /* Logo */ },
        new TextElementTemplate { Text = "Loading...", Height = 40 },
        new ProgressBarTemplate { Width = 400, Height = 20 }
    }
};
```

### Settings Menu with Sections

```csharp
new VerticalLayoutTemplate
{
    ItemSpacing = 25,  // 25px between sections
    Alignment = Alignment.TopLeft,
    Padding = new Padding(30),
    Children = new Template[]
    {
        new TextElementTemplate { Text = "Graphics", Height = 30 },
        
        // Nested VerticalLayout for graphics options
        new VerticalLayoutTemplate
        {
            ItemSpacing = 10,  // 10px between options
            Children = new Template[]
            {
                new CheckboxTemplate { Text = "VSync", Height = 30 },
                new CheckboxTemplate { Text = "Shadows", Height = 30 }
            }
        },
        
        new TextElementTemplate { Text = "Audio", Height = 30 },
        
        // Nested VerticalLayout for audio options
        new VerticalLayoutTemplate
        {
            ItemSpacing = 10,
            Children = new Template[]
            {
                new SliderTemplate { Text = "Master Volume", Height = 40 },
                new SliderTemplate { Text = "Music Volume", Height = 40 }
            }
        }
    }
};
```

## Troubleshooting

### Children Not Appearing

**Problem**: Layout is created but children are not visible.

**Solutions**: 
1. Ensure you call `contentManager.Activate(layout)` to activate the layout and its children
2. Check that VerticalLayout itself has size (either from parent constraints or explicit Width/Height)
3. Verify children have renderable content (DrawableElement needs texture/tint, TextElement needs text, etc.)

**Note**: VerticalLayout automatically provides fallback sizing (DefaultChildHeight: 50px) for children without explicit sizes, so size specification is optional.

### Layout Not Updating

**Problem**: Changed VerticalLayoutMode but layout doesn't update.

**Solution**: Layout updates occur on the next frame. If using deferred properties, ensure `ApplyUpdates()` is called in the update loop.

### Children Overlapping

**Problem**: Children overlap each other unexpectedly.

**Solution**: 
- Check that Spacing.Y is not negative (unless overlap is intended)
- In SpacedEqually mode, total child height may exceed content area, causing negative spacing
- Verify Padding is not consuming all available space

### Justified Mode Not Stretching

**Problem**: Children in Justified mode are not the same height.

**Solution**: Justified mode sets size constraints, but children must respect them. Ensure children have appropriate SizeMode (e.g., Absolute or Relative, not Fixed).

## Next Steps

- **Custom Layout Modes**: Extend VerticalLayout with custom layout algorithms
- **Horizontal Layouts**: Implement HorizontalLayout using similar patterns
- **Grid Layouts**: Combine multiple VerticalLayouts for grid-based layouts
- **Scroll Support**: Wrap VerticalLayout in ScrollContainer for scrolling long lists

## API Reference

For complete API documentation, see:
- [API Contract](./contracts/api-contract.md)
- [Data Model](./data-model.md)
- [Research Document](./research.md)

## Examples

Full working examples available in:
- Unit Tests: `Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs`
- Integration Tests: `TestApp/Tests/VerticalLayoutTests.cs`

## Support

For issues or questions:
1. Check the [specification](./spec.md) for functional requirements
2. Review [research document](./research.md) for design decisions
3. Consult [API contract](./contracts/api-contract.md) for behavioral guarantees
