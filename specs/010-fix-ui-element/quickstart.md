# Quickstart: Using RectTransform and UserInterfaceElement

## Overview
The `UserInterfaceElement` now inherits from `RectTransform`, providing a robust 2D spatial system with Top-Left (0,0) coordinates.

## Basic Usage

### Creating a UI Element
`csharp
// In a template or factory
var button = new Button
{
    Position = new Vector2D<float>(100, 50),
    Size = new Vector2D<float>(200, 60),
    Pivot = new Vector2D<float>(0, 0) // Top-Left pivot (default)
};
``n
### Understanding Pivot
The `Pivot` property (0-1) determines the point around which the element is positioned, rotated, and scaled.
- **(0, 0)**: Top-Left (Standard UI)
- **(0.5, 0.5)**: Center (Sprites/Popups)
- **(1, 1)**: Bottom-Right

### Getting Screen Bounds
Use `GetBounds()` to get the screen-space rectangle for input detection.
`csharp
Rectangle<int> bounds = element.GetBounds();
if (bounds.Contains(mousePosition))
{
    // Handle click
}
``n
## Migration Guide
- **Legacy**: `AnchorPoint` (-1 to 1) is removed.
- **New**: Use `Pivot` (0 to 1).
    - Old (-1, -1) -> New (0, 0)
    - Old (0, 0) -> New (0.5, 0.5)
    - Old (1, 1) -> New (1, 1)
- **Geometry**: `TexturedQuad` is now 0..1. Custom shaders using this geometry may need updates.
