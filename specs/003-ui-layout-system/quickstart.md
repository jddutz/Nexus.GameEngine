# Quickstart: UI Layout System

**Feature**: `003-ui-layout-system`  
**Date**: 2025-11-04  
**Audience**: Developers using Nexus.GameEngine to build responsive UI layouts

## Prerequisites

- Nexus.GameEngine project set up and building
- Basic understanding of component-based architecture
- Familiarity with templates and `IRuntimeComponent` lifecycle

## Core Concepts (5-minute read)

### Anchor-Based Positioning

Elements position themselves relative to anchor points in their parent:

```csharp
// AnchorPoint coordinates: (-1, -1) = top-left, (0, 0) = center, (1, 1) = bottom-right
var element = new ElementTemplate
{
    AnchorPoint = new(0, 0),      // Center of element...
    Position = new(960, 540),     // ...aligns with this position (screen center for 1920x1080)
    Size = new(200, 100)
};
```

**Key insight**: When window resizes, Position stays constant, but anchor point means element moves appropriately. A top-left anchored element stays in the corner, a centered element stays centered.

### Size Modes

Elements can determine their size in four ways:

- **Fixed**: Explicit pixel dimensions (default)
  ```csharp
  SizeMode = SizeMode.Fixed,
  Width = 200,
  Height = 100
  ```

- **Percentage**: Size relative to parent
  ```csharp
  SizeMode = SizeMode.Percentage,
  WidthPercentage = 50,   // 50% of parent width
  HeightPercentage = 75   // 75% of parent height
  ```

- **Stretch**: Fill available space from parent constraints
  ```csharp
  SizeMode = SizeMode.Stretch  // Fills parent
  ```

- **Intrinsic**: Size determined by content (text length, child elements)
  ```csharp
  SizeMode = SizeMode.Intrinsic  // Size to fit content
  ```

### Layout Containers

Three layout types automatically arrange children:

- **VerticalLayout**: Stacks children vertically
- **HorizontalLayout**: Arranges children horizontally  
- **GridLayout**: Organizes children in grid pattern

## Quick Examples

### Example 1: Main Menu (Centered Vertical List)

Create a centered vertical menu with buttons:

```csharp
public static class MenuTemplates
{
    public static Template MainMenu => new VerticalLayoutTemplate
    {
        // Center the menu container
        AnchorPoint = new(0, 0),
        Position = new(960, 540),  // Will be dynamic based on viewport
        
        // Size to content (buttons)
        SizeMode = SizeMode.Intrinsic,
        
        // Spacing and padding
        Spacing = new(0, 15),
        Padding = new Padding(20),
        HorizontalAlignment = HorizontalAlignment.Center,
        
        Children = new[]
        {
            CreateButton("Start Game", new(1, 0, 0, 1)),   // Red
            CreateButton("Options", new(0, 1, 0, 1)),      // Green
            CreateButton("Quit", new(0, 0, 1, 1))          // Blue
        }
    };
    
    private static Template CreateButton(string label, Vector4D<float> color) =>
        new ElementTemplate
        {
            Size = new(200, 50),
            TintColor = color
            // Future: Add TextElement child for label
        };
}
```

### Example 2: Game HUD (Anchored Elements)

Create HUD elements anchored to screen edges:

```csharp
public static Template GameHUD => new ElementTemplate
{
    // Root element fills entire viewport
    SizeMode = SizeMode.Stretch,
    
    Children = new[]
    {
        // Health bar - top-left corner
        new ElementTemplate
        {
            AnchorPoint = new(-1, -1),  // Top-left of element...
            Position = new(20, 20),      // ...20px from top-left corner
            Size = new(200, 30),
            TintColor = new(1, 0, 0, 1)  // Red
        },
        
        // Mini-map - top-right corner
        new ElementTemplate
        {
            AnchorPoint = new(1, -1),    // Top-right of element...
            Position = new(1900, 20),    // ...20px from right edge (1920-20)
            Size = new(200, 200),
            TintColor = new(0, 0.5f, 1, 1)  // Cyan
        },
        
        // Action bar - bottom center
        new HorizontalLayoutTemplate
        {
            AnchorPoint = new(0, 1),     // Bottom-center of layout...
            Position = new(960, 1060),   // ...centered horizontally, 20px from bottom
            Spacing = new(10, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            
            Children = new[]
            {
                CreateActionButton(new(1, 0, 0, 1)),    // Red ability
                CreateActionButton(new(1, 0.5f, 0, 1)), // Orange ability
                CreateActionButton(new(1, 1, 0, 1)),    // Yellow ability
                CreateActionButton(new(0, 1, 0, 1)),    // Green ability
                CreateActionButton(new(0, 0, 1, 1))     // Blue ability
            }
        }
    }
};

private static Template CreateActionButton(Vector4D<float> color) =>
    new ElementTemplate
    {
        Size = new(60, 60),
        TintColor = color
    };
```

**Key point**: When window resizes, health bar stays in top-left, mini-map in top-right, action bar stays centered at bottom. No hardcoding for different resolutions!

### Example 3: Inventory Grid (Responsive)

Create inventory grid that scales with window size:

```csharp
public static Template InventoryPanel => new GridLayoutTemplate
{
    // Center the inventory panel
    AnchorPoint = new(0, 0),
    Position = new(960, 540),
    
    // Take up 60% of screen width/height
    SizeMode = SizeMode.Percentage,
    WidthPercentage = 60,
    HeightPercentage = 60,
    
    // Grid configuration
    ColumnCount = 6,
    Spacing = new(5, 5),
    Padding = new Padding(15),
    MaintainCellAspectRatio = true,
    CellAspectRatio = 1.0f,  // Square cells
    
    // Create 36 inventory slots (6x6 grid)
    Children = Enumerable.Range(0, 36)
        .Select(i => new ElementTemplate
        {
            TintColor = new(0.2f, 0.2f, 0.2f, 1)  // Dark gray slots
        })
        .ToArray()
};
```

This grid automatically:
- Resizes when window changes (60% of viewport)
- Maintains square cells regardless of aspect ratio
- Distributes cells evenly with consistent spacing

### Example 4: Responsive Dialog

Create dialog that adapts to different screen sizes:

```csharp
public static Template SettingsDialog => new ElementTemplate
{
    // Center the dialog
    AnchorPoint = new(0, 0),
    Position = new(960, 540),
    
    // Use percentage sizing to adapt to small screens
    SizeMode = SizeMode.Percentage,
    WidthPercentage = 40,
    HeightPercentage = 50,
    
    // Set minimum size for readability
    MinSize = new(400, 300),
    
    // Set maximum size to prevent huge dialogs on 4K
    MaxSize = new(800, 600),
    
    TintColor = new(0.1f, 0.1f, 0.1f, 0.9f),  // Semi-transparent dark background
    
    Children = new[]
    {
        new VerticalLayoutTemplate
        {
            Padding = new Padding(20),
            Spacing = new(0, 10),
            
            Children = new[]
            {
                // Title
                new ElementTemplate
                {
                    Size = new(300, 40),
                    TintColor = new(1, 1, 1, 1)
                    // Future: TextElement for "Settings"
                },
                
                // Options...
                CreateOption("Volume", 50),
                CreateOption("Graphics", 75),
                CreateOption("Controls", 100),
                
                // Buttons
                new HorizontalLayoutTemplate
                {
                    Spacing = new(10, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    
                    Children = new[]
                    {
                        CreateButton("OK", new(0, 1, 0, 1)),
                        CreateButton("Cancel", new(1, 0, 0, 1))
                    }
                }
            }
        }
    }
};

private static Template CreateOption(string label, int value) =>
    new HorizontalLayoutTemplate
    {
        Spacing = new(10, 0),
        Children = new[]
        {
            new ElementTemplate { Size = new(150, 30), TintColor = new(0.5f, 0.5f, 0.5f, 1) },
            new ElementTemplate { Size = new(100, 30), TintColor = new(0.3f, 0.3f, 0.8f, 1) }
        }
    };
```

## Common Patterns

### Pattern 1: Full-Screen Background

```csharp
new ElementTemplate
{
    SizeMode = SizeMode.Stretch,  // Fills entire viewport
    TintColor = new(0.1f, 0.1f, 0.2f, 1)  // Dark blue background
}
```

### Pattern 2: Anchored Corner Element

```csharp
// Top-left
AnchorPoint = new(-1, -1),
Position = new(margin, margin)

// Top-right
AnchorPoint = new(1, -1),
Position = new(screenWidth - margin, margin)

// Bottom-left
AnchorPoint = new(-1, 1),
Position = new(margin, screenHeight - margin)

// Bottom-right
AnchorPoint = new(1, 1),
Position = new(screenWidth - margin, screenHeight - margin)
```

### Pattern 3: Centered Element

```csharp
AnchorPoint = new(0, 0),
Position = new(screenWidth / 2, screenHeight / 2)
```

### Pattern 4: List with Equal-Width Items

```csharp
new VerticalLayoutTemplate
{
    Spacing = new(0, 10),
    HorizontalAlignment = HorizontalAlignment.Stretch,  // Children fill width
    Children = items
}
```

### Pattern 5: Toolbar with Icon Buttons

```csharp
new HorizontalLayoutTemplate
{
    Spacing = new(5, 0),
    Padding = new Padding(10),
    VerticalAlignment = VerticalAlignment.Center,
    Children = iconButtons
}
```

## Testing Your Layouts

### Manual Testing Checklist

Test your layouts at these resolutions:

- **HD**: 1280x720 (16:9)
- **Full HD**: 1920x1080 (16:9)
- **4K**: 3840x2160 (16:9)
- **Ultrawide**: 2560x1080 (21:9)
- **Classic**: 1024x768 (4:3)
- **Mobile portrait**: 640x1136 (9:16)

### Automated Testing Pattern

Use pixel sampling to verify layouts:

```csharp
[Test]
public static Template CenteredRedSquareTest => new ElementTemplate
{
    AnchorPoint = new(0, 0),
    Position = new(960, 540),
    Size = new(100, 100),
    TintColor = new(1, 0, 0, 1),  // Red
    
    // Middleware to sample and verify
    OnPostRender = (elem) =>
    {
        var sampler = elem.GetService<IPixelSampler>();
        var centerColor = sampler.SamplePixel(960, 540);
        Assert.Equal(new(1, 0, 0, 1), centerColor);
    }
};
```

See `src/GameEngine/Testing/README.md` for full testing documentation.

## Common Pitfalls

### Pitfall 1: Forgetting Anchor Points

❌ **Wrong**: Hardcoding positions for top-right without anchor
```csharp
Position = new(1820, 20)  // This won't stay in corner when window resizes!
```

✅ **Correct**: Use anchor point
```csharp
AnchorPoint = new(1, -1),
Position = new(1900, 20)  // Now it stays in top-right corner
```

### Pitfall 2: Fixed Sizes on Small Screens

❌ **Wrong**: Fixed size that's too large for mobile
```csharp
Size = new(1000, 800)  // Exceeds 640x360 mobile screen!
```

✅ **Correct**: Use percentage with min/max
```csharp
SizeMode = SizeMode.Percentage,
WidthPercentage = 80,
HeightPercentage = 70,
MinSize = new(300, 200),
MaxSize = new(1000, 800)
```

### Pitfall 3: Not Using Layouts for Lists

❌ **Wrong**: Manually calculating positions
```csharp
Children = new[]
{
    new ElementTemplate { Position = new(0, 0), Size = new(200, 50) },
    new ElementTemplate { Position = new(0, 60), Size = new(200, 50) },  // Manual spacing!
    new ElementTemplate { Position = new(0, 120), Size = new(200, 50) }
}
```

✅ **Correct**: Use VerticalLayout
```csharp
Children = new[]
{
    new VerticalLayoutTemplate
    {
        Spacing = new(0, 10),
        Children = new[]
        {
            new ElementTemplate { Size = new(200, 50) },
            new ElementTemplate { Size = new(200, 50) },  // Automatic spacing!
            new ElementTemplate { Size = new(200, 50) }
        }
    }
}
```

### Pitfall 4: Animating Size Without Considering Layout

❌ **Wrong**: Animating size during gameplay
```csharp
element.Size = new(200, 200);  // This might trigger layout recalculation every frame!
```

✅ **Correct**: Animate visual properties (Scale, TintColor) instead
```csharp
element.Scale = new(1.5f, 1.5f);  // Visual scaling, no layout recalculation
```

## Next Steps

1. **Read the data model**: See `data-model.md` for detailed entity descriptions
2. **Review existing code**: Check `src/GameEngine/GUI/Element.cs` for current implementation
3. **Run integration tests**: Execute `TestApp` to see layout examples in action
4. **Experiment**: Create your own layouts and test across different resolutions

## Additional Resources

- **Architecture Overview**: `.github/copilot-instructions.md` - Component architecture and property system
- **Testing Guide**: `src/GameEngine/Testing/README.md` - Frame-based testing with pixel sampling
- **Project Structure**: `.docs/Project Structure.md` - Overall codebase organization

## Support

Questions or issues? Check:
1. Feature spec: `specs/003-ui-layout-system/spec.md`
2. Implementation plan: `specs/003-ui-layout-system/plan.md`
3. Research decisions: `specs/003-ui-layout-system/research.md`
