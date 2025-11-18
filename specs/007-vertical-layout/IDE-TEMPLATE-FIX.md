# Fix for Templates.NexusIDE.cs

**Date**: November 17, 2025  
**Issue**: DrawableElement children not visible in VerticalLayout  
**Root Cause**: Children missing explicit Width/Height (default to 0x0)

## Quick Fix

Add `ItemHeight` to the VerticalLayoutTemplate:

```csharp
new VerticalLayoutTemplate()
{
    Name = "VerticalLayout",
    ItemHeight = 80,  // ✅ ADD THIS LINE - gives all children 80px height
    Width = 300,
    VerticalSizeMode = SizeMode.Absolute,
    RelativeHeight = -200,
    Alignment = Align.MiddleLeft,
    AnchorPoint = Align.MiddleLeft,
    OffsetLeft = 100,
    Subcomponents = [
        new DrawableElementTemplate()
        {
            Name = "Item1",
            TintColor = Colors.Red
            // No Width/Height needed when ItemHeight is set
        },
        new DrawableElementTemplate()
        {
            Name = "Item2",
            TintColor = Colors.Green
        },
        new DrawableElementTemplate()
        {
            Name = "Item3",
            TintColor = Colors.Blue
        },
        new DrawableElementTemplate()
        {
            Name = "Item4",
            TintColor = Colors.Yellow
        },
        new DrawableElementTemplate()
        {
            Name = "Item5",
            TintColor = Colors.Cyan
        },
    ]
}
```

## Alternative: Explicit Child Sizes

If you want different heights for each child:

```csharp
new VerticalLayoutTemplate()
{
    Name = "VerticalLayout",
    Width = 300,
    VerticalSizeMode = SizeMode.Absolute,
    RelativeHeight = -200,
    Alignment = Align.MiddleLeft,
    AnchorPoint = Align.MiddleLeft,
    OffsetLeft = 100,
    Spacing = new Vector2D<float>(0, 10),  // Optional: spacing between items
    Subcomponents = [
        new DrawableElementTemplate()
        {
            Name = "Item1",
            Width = 280,  // ✅ Explicit sizes
            Height = 60,
            TintColor = Colors.Red
        },
        new DrawableElementTemplate()
        {
            Name = "Item2",
            Width = 280,
            Height = 80,  // Different height
            TintColor = Colors.Green
        },
        new DrawableElementTemplate()
        {
            Name = "Item3",
            Width = 280,
            Height = 60,
            TintColor = Colors.Blue
        },
        new DrawableElementTemplate()
        {
            Name = "Item4",
            Width = 280,
            Height = 100,  // Different height
            TintColor = Colors.Yellow
        },
        new DrawableElementTemplate()
        {
            Name = "Item5",
            Width = 280,
            Height = 60,
            TintColor = Colors.Cyan
        },
    ]
}
```

## Why This Happens

1. **DrawableElement Default**: `Size = (0, 0)` and `SizeMode.Fixed`
2. **VerticalLayout Measure**: Calls `child.Measure()` → returns `(0, 0)` for unsized children
3. **Zero Height Allocation**: Child gets 0px height → invisible

## Implementation Plan Updated

The plan has been updated to address this:

1. **Research**: Added "Critical Finding: Child Sizing Issue" section
2. **Data Model**: Added edge case for zero-size children with warning
3. **Quickstart**: Added prominent "Important: Child Sizing Requirements" section
4. **Plan**: Added implementation requirement for warning log when zero-height children detected

## Testing After Fix

After applying the fix, you should see:
- Five colored rectangles stacked vertically
- Each item 80px tall (with ItemHeight approach) or varied heights (with explicit sizes)
- Positioned on left side of screen (MiddleLeft alignment)
- 100px offset from left edge

If items still don't appear, check:
1. VerticalLayout itself has size (Width=300, RelativeHeight=-200 means it fills height minus 200px)
2. Parent Container is activated and sized correctly
3. Rendering system is processing DrawableElement commands
