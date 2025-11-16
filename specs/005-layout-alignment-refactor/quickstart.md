# Quick Start Guide: Layout Alignment Refactor

**Feature**: Unified Vector2D Alignment for HorizontalLayout and VerticalLayout  
**Branch**: `005-layout-alignment-refactor`  
**Date**: November 14, 2025

## Overview

This guide provides quick examples for using the new unified `Alignment` property (Vector2D<float>) in HorizontalLayout and VerticalLayout components. The new API replaces separate single-axis alignment properties with a single two-dimensional alignment vector.

## Key Concepts

### Alignment Range
- **-1**: Start (left for horizontal, top for vertical)
- **0**: Center/Middle
- **1**: End (right for horizontal, bottom for vertical)

### Which Component is Used?
- **HorizontalLayout**: Uses only the **Y component** (vertical alignment of children)
- **VerticalLayout**: Uses only the **X component** (horizontal alignment of children)
- The unused component is typically set to 0 by convention

## Quick Reference: Predefined Constants

Use the `Align` static class for common alignment values:

```csharp
using Nexus.GameEngine.GUI;

// Individual axis constants
Align.Left = -1f    Align.Top = -1f
Align.Center = 0f   Align.Middle = 0f
Align.Right = 1f    Align.Bottom = 1f

// 2D alignment presets (Vector2D<float>)
Align.TopLeft       Align.TopCenter       Align.TopRight
Align.MiddleLeft    Align.MiddleCenter    Align.MiddleRight
Align.BottomLeft    Align.BottomCenter    Align.BottomRight
```

## HorizontalLayout Examples

### Example 1: Top-Aligned Toolbar

```csharp
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;

var toolbar = new HorizontalLayout(descriptorManager);
toolbar.SetPadding(new Padding(10));
toolbar.SetSpacing(new Vector2D<float>(5, 0));  // 5px between children
toolbar.SetAlignment(Align.TopCenter);  // Align children to top (Y = Top)

// Add children
toolbar.CreateChild(new ButtonTemplate { Size = new(80, 40) });
toolbar.CreateChild(new ButtonTemplate { Size = new(80, 40) });
toolbar.CreateChild(new ButtonTemplate { Size = new(80, 40) });
```

**Result**: Three buttons arranged horizontally, aligned to the top of the toolbar.

---

### Example 2: Center-Aligned Toolbar (Default)

```csharp
var toolbar = new HorizontalLayout(descriptorManager);
toolbar.SetAlignment(new Vector2D<float>(0, Align.Middle));  // Center vertically

// OR use predefined constant
toolbar.SetAlignment(Align.MiddleCenter);  // X doesn't matter, Y = Middle
```

**Result**: Children are vertically centered in the toolbar.

---

### Example 3: Bottom-Aligned Toolbar

```csharp
var toolbar = new HorizontalLayout(descriptorManager);
toolbar.SetAlignment(new Vector2D<float>(0, Align.Bottom));  // Bottom align

// OR
toolbar.SetAlignment(Align.BottomCenter);  // Only Y (Bottom) is used
```

**Result**: Children are aligned to the bottom of the toolbar.

---

### Example 4: Stretched Children (Full Height)

```csharp
var toolbar = new HorizontalLayout(descriptorManager);
toolbar.SetStretchChildren(true);  // Children fill full height
// Alignment is ignored when stretching
```

**Result**: All children are stretched to fill the full height of the toolbar's content area.

---

## VerticalLayout Examples

### Example 1: Left-Aligned Menu

```csharp
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.GUI.Layout;

var menu = new VerticalLayout(descriptorManager);
menu.SetPadding(new Padding(20));
menu.SetSpacing(new Vector2D<float>(0, 10));  // 10px between children
menu.SetAlignment(Align.TopLeft);  // Align children to left (X = Left)

// Add menu items
menu.CreateChild(new ButtonTemplate { Size = new(200, 50) });
menu.CreateChild(new ButtonTemplate { Size = new(200, 50) });
menu.CreateChild(new ButtonTemplate { Size = new(200, 50) });
```

**Result**: Three buttons arranged vertically, aligned to the left of the menu.

---

### Example 2: Center-Aligned Menu (Default)

```csharp
var menu = new VerticalLayout(descriptorManager);
menu.SetAlignment(new Vector2D<float>(Align.Center, 0));  // Center horizontally

// OR use predefined constant
menu.SetAlignment(Align.MiddleCenter);  // Y doesn't matter, X = Center
```

**Result**: Children are horizontally centered in the menu.

---

### Example 3: Right-Aligned Menu

```csharp
var menu = new VerticalLayout(descriptorManager);
menu.SetAlignment(new Vector2D<float>(Align.Right, 0));  // Right align

// OR
menu.SetAlignment(Align.TopRight);  // Only X (Right) is used
```

**Result**: Children are aligned to the right side of the menu.

---

### Example 4: Stretched Children (Full Width)

```csharp
var menu = new VerticalLayout(descriptorManager);
menu.SetStretchChildren(true);  // Children fill full width
// Alignment is ignored when stretching
```

**Result**: All children are stretched to fill the full width of the menu's content area.

---

## Advanced Examples

### Example 5: Custom Alignment Values

```csharp
// Position children 75% of the way to the right
var layout = new VerticalLayout(descriptorManager);
layout.SetAlignment(new Vector2D<float>(0.5f, 0));  // Between Center (0) and Right (1)
```

**Result**: Children are positioned at 75% horizontal position (custom alignment).

---

### Example 6: Animated Alignment

```csharp
var menu = new VerticalLayout(descriptorManager);
menu.SetAlignment(Align.TopLeft);  // Start left-aligned

// Animate to right-aligned over 0.5 seconds
menu.SetAlignment(
    Align.TopRight,
    duration: 0.5f,
    mode: InterpolationMode.EaseInOut
);
```

**Result**: Menu children smoothly animate from left to right alignment.

---

### Example 7: Combining with Padding and Spacing

```csharp
var toolbar = new HorizontalLayout(descriptorManager);
toolbar.SetPadding(new Padding(
    top: 10,
    right: 20,
    bottom: 10,
    left: 20
));
toolbar.SetSpacing(new Vector2D<float>(8, 0));  // 8px horizontal spacing
toolbar.SetAlignment(new Vector2D<float>(0, Align.Middle));  // Center vertically

// Content area = toolbar bounds - padding
// Children positioned with 8px gaps, centered vertically in content area
```

---

## Migration from Old API

### HorizontalLayout Migration

```csharp
// OLD API (before refactor)
var toolbar = new HorizontalLayout(descriptorManager);
toolbar.SetAlignment(Align.Top);

// NEW API (after refactor)
var toolbar = new HorizontalLayout(descriptorManager);
toolbar.SetAlignment(new Vector2D<float>(0, Align.Top));
// OR
toolbar.SetAlignment(Align.TopCenter);  // More concise
```

---

### VerticalLayout Migration

```csharp
// OLD API (before refactor)
var menu = new VerticalLayout(descriptorManager);
menu.SetAlignment(Align.Left);

// NEW API (after refactor)
var menu = new VerticalLayout(descriptorManager);
menu.SetAlignment(new Vector2D<float>(Align.Left, 0));
// OR
menu.SetAlignment(Align.TopLeft);  // More concise
```

---

## Common Patterns

### Pattern 1: Full-Screen Centered Dialog

```csharp
var dialog = new VerticalLayout(descriptorManager);
dialog.SetSizeMode(SizeMode.Fixed);
dialog.SetSize(new Vector2D<int>(400, 300));
dialog.SetPosition(/* calculated center position */);
dialog.SetAlignment(Align.MiddleCenter);  // Center all children
dialog.SetPadding(new Padding(30));
dialog.SetSpacing(new Vector2D<float>(0, 15));
```

---

### Pattern 2: Top Navigation Bar

```csharp
var navbar = new HorizontalLayout(descriptorManager);
navbar.SetSizeConstraints(new Rectangle<int>(0, 0, screenWidth, 60));
navbar.SetAlignment(Align.MiddleLeft);  // Vertically centered, left-aligned
navbar.SetPadding(new Padding(left: 20, right: 20));
navbar.SetSpacing(new Vector2D<float>(15, 0));
```

---

### Pattern 3: Right-Aligned Action Buttons

```csharp
var actionBar = new HorizontalLayout(descriptorManager);
actionBar.SetAlignment(Align.MiddleRight);  // Vertically centered, right-aligned
actionBar.SetSpacing(new Vector2D<float>(10, 0));

actionBar.CreateChild(new ButtonTemplate { Label = "Cancel" });
actionBar.CreateChild(new ButtonTemplate { Label = "OK" });
```

---

## Best Practices

1. **Use Predefined Constants**: Prefer `Align.TopLeft` over `new Vector2D<float>(-1, -1)` for readability
2. **Set Unused Component to 0**: By convention, set the unused component to 0 (e.g., `new Vector2D<float>(0, Align.Top)` for HorizontalLayout)
3. **Consider StretchChildren**: Use `SetStretchChildren(true)` when you want uniform child sizes
4. **Combine with Padding**: Use padding to create space around the content area before alignment is applied
5. **Test Edge Cases**: Test with children of varying sizes to ensure alignment looks correct

---

## Troubleshooting

### Children not aligning as expected?
- **HorizontalLayout**: Check the **Y component** of Alignment (not X)
- **VerticalLayout**: Check the **X component** of Alignment (not Y)
- Ensure `StretchChildren` is not set to true (which overrides alignment)

### Children overlapping?
- Check `Spacing` property - ensure X (horizontal) or Y (vertical) is set appropriately
- Verify content area size is sufficient for all children

### Alignment seems to have no effect?
- If `StretchChildren = true`, alignment is ignored for the relevant axis
- Check if child sizes are exactly matching content area size (no room for alignment)

---

## Additional Resources

- **Full Specification**: See [spec.md](./spec.md) for complete requirements
- **Data Model**: See [data-model.md](./data-model.md) for detailed entity definitions
- **API Contracts**: See [contracts/](./contracts/) for complete API surface
- **Research**: See [research.md](./research.md) for design decisions and rationale

---

## Next Steps

1. Review the [feature specification](./spec.md) for complete user scenarios
2. Examine [data-model.md](./data-model.md) for implementation details
3. Check [contracts/](./contracts/) for expected API signatures
4. Proceed to implementation following the TDD workflow in [constitution.md](../../.specify/memory/constitution.md)
