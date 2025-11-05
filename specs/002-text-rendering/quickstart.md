# Quickstart: Text Rendering with TextElement

**Feature**: Text Rendering with TextElement  
**Date**: 2025-11-04  
**Audience**: Developers using the Nexus.GameEngine

## Overview

This guide demonstrates how to display text on screen using the new `TextElement` component. TextElement leverages the Element positioning system (Position, AnchorPoint, Size, Scale) and automatically manages font atlas resources for efficient text rendering.

---

## Hello World Example

### Basic Usage

Display "Hello World" centered on screen using the default font (Roboto Regular 16pt):

```csharp
using Nexus.GameEngine.Components.GUI;
using Silk.NET.Maths;

// Create TextElement via template
var helloWorldTemplate = new TextElementTemplate {
    Text = "Hello World",
    Position = new Vector3D<float>(960, 540, 0),  // Screen center (1920×1080)
    AnchorPoint = new Vector2D<float>(0, 0),      // Center alignment
    Scale = new Vector3D<float>(1, 1, 1)          // Default scale
};

// Add to content hierarchy
var textElement = ContentManager.CreateInstance(helloWorldTemplate);
parentComponent.AddChild(textElement);
```

**Result**: "Hello World" renders centered at screen position (960, 540) with all 10 characters visible (including space).

---

## Understanding Positioning

TextElement inherits Element's positioning system. The key properties are:

### Position
Where the element **IS** in screen space (pixels):
```csharp
Position = new Vector3D<float>(960, 540, 0)  // X, Y, Z coordinates
```

### AnchorPoint
Which point of the element aligns with Position (normalized -1 to 1 range):
```csharp
// (-1, -1) = top-left corner aligns with Position
AnchorPoint = new Vector2D<float>(-1, -1)

// (0, 0) = center aligns with Position
AnchorPoint = new Vector2D<float>(0, 0)

// (1, 1) = bottom-right corner aligns with Position
AnchorPoint = new Vector2D<float>(1, 1)
```

### Size
Automatically calculated from text dimensions (read-only in MVP):
```csharp
// TextElement measures text and sets Size automatically
// Size.X = sum of glyph advances (text width)
// Size.Y = font line height (text height)
```

### Scale
Multiplier for scaling text (inherited from Transformable):
```csharp
Scale = new Vector3D<float>(2, 2, 1)  // 2× size (note: causes bitmap scaling artifacts)
```

---

## Common Positioning Scenarios

### Top-Left Alignment
```csharp
new TextElementTemplate {
    Text = "Top Left",
    Position = new Vector3D<float>(50, 50, 0),   // Top-left corner
    AnchorPoint = new Vector2D<float>(-1, -1)    // Anchor at top-left
}
```

### Top-Right Alignment
```csharp
new TextElementTemplate {
    Text = "Top Right",
    Position = new Vector3D<float>(1870, 50, 0),  // Top-right corner
    AnchorPoint = new Vector2D<float>(1, -1)      // Anchor at top-right
}
```

### Bottom-Center Alignment
```csharp
new TextElementTemplate {
    Text = "Bottom Center",
    Position = new Vector3D<float>(960, 1030, 0),  // Bottom-center
    AnchorPoint = new Vector2D<float>(0, 1)        // Anchor at bottom-center
}
```

### Centered Text
```csharp
new TextElementTemplate {
    Text = "Centered",
    Position = new Vector3D<float>(960, 540, 0),  // Screen center
    AnchorPoint = new Vector2D<float>(0, 0)       // Anchor at center
}
```

---

## Scaling Text

Apply scale transformation for larger/smaller text:

```csharp
new TextElementTemplate {
    Text = "Large Text",
    Position = new Vector3D<float>(960, 540, 0),
    AnchorPoint = new Vector2D<float>(0, 0),
    Scale = new Vector3D<float>(2, 2, 1)  // 2× larger
}
```

**Note**: Scaling causes bitmap scaling artifacts (blurry/pixelated edges). This is expected behavior for rasterized fonts. Future SDF rendering will address this limitation.

---

## Advanced Usage

### Creating Multiple TextElements

Font atlases are automatically cached and shared across TextElements using the same font:

```csharp
// Create multiple text elements - all share same font atlas
var texts = new[] {
    new TextElementTemplate { Text = "Line 1", Position = new(100, 100, 0), AnchorPoint = new(-1, -1) },
    new TextElementTemplate { Text = "Line 2", Position = new(100, 150, 0), AnchorPoint = new(-1, -1) },
    new TextElementTemplate { Text = "Line 3", Position = new(100, 200, 0), AnchorPoint = new(-1, -1) }
};

foreach (var template in texts) {
    var element = ContentManager.CreateInstance(template);
    parentComponent.AddChild(element);
}
```

**Performance**: All three TextElements share a single 6KB geometry buffer and 256KB atlas texture. No duplicate GPU resources!

---

## Component Lifecycle

### Activation
When TextElement activates (via OnActivate):
1. Loads default font resource (gets shared FontResource from FontResourceManager)
2. Creates descriptor set for font atlas texture binding
3. Measures text and calculates Size property
4. Ready to render

### Rendering
Each frame, TextElement.GetDrawCommands():
1. Emits N DrawCommands (one per visible character)
2. Each DrawCommand references shared geometry at different FirstVertex offset
3. Calculates per-glyph WorldMatrix for positioning
4. Renderer batches all DrawCommands into single GPU draw call

### Deactivation
When TextElement deactivates (via OnDeactivate):
1. Releases descriptor set
2. Releases font resource reference (FontResourceManager handles disposal when unused)

---

## Integration with UI System

TextElement works seamlessly with other UI components:

```csharp
// Create container with text children
var menuTemplate = new ElementTemplate {
    Position = new Vector3D<float>(960, 540, 0),
    AnchorPoint = new Vector2D<float>(0, 0),
    Children = new IComponentTemplate[] {
        new TextElementTemplate {
            Text = "Main Menu",
            Position = new Vector3D<float>(0, -100, 0),
            AnchorPoint = new Vector2D<float>(0, 0),
            Scale = new Vector3D<float>(2, 2, 1)
        },
        new TextElementTemplate {
            Text = "Press SPACE to start",
            Position = new Vector3D<float>(0, 50, 0),
            AnchorPoint = new Vector2D<float>(0, 0)
        }
    }
};

var menu = ContentManager.CreateInstance(menuTemplate);
```

---

## Performance Characteristics

### Font Atlas Generation
- **Timing**: <100ms during application startup (one-time per font)
- **Memory**: ~272KB per unique font (atlas texture + shared geometry + metrics)
- **GPU Upload**: One-time 6KB geometry + 256KB texture upload

### Runtime Rendering
- **DrawCommands**: N commands per frame (where N = character count)
- **GPU Draw Calls**: All DrawCommands batch into 1 draw call (same pipeline + texture)
- **Memory per TextElement**: ~124 bytes + text string allocation
- **Dynamic Text Changes**: Zero geometry uploads (only push constants change)

### Example: 100 TextElements
- **Font Resources**: 272KB (shared)
- **TextElement Instances**: 12.4KB (100 × 124 bytes)
- **Total**: ~285KB (vs 27MB without shared geometry optimization!)

---

## Known Limitations (MVP)

The following features are **not** included in the MVP:

1. **Single Font Only**: Default Roboto Regular 16pt (no font selection)
2. **Single Line Only**: No multi-line text or line wrapping
3. **Left-to-Right Only**: No RTL or vertical text
4. **ASCII Printable Only**: Characters 32-126 (no Unicode, emoji, accents)
5. **No Text Styling**: White color only (no color selection, bold, italic)
6. **No Kerning**: Simple advance-based spacing
7. **Scaling Artifacts**: Bitmap scaling causes blurriness (SDF addresses this later)
8. **No Clipping**: Text extends beyond bounds if too long

---

## Testing Your Implementation

### Pixel Sampling Validation

Integration tests use pixel sampling to verify rendering:

```csharp
[Test]
public static Template HelloWorldTest => new ComponentTestTemplate {
    TestName = "HelloWorld_CenteredText",
    ArrangeAction = (test) => {
        test.CreateChild(new TextElementTemplate {
            Text = "Hello World",
            Position = new Vector3D<float>(960, 540, 0),
            AnchorPoint = new Vector2D<float>(0, 0)
        });
    },
    AssertAction = (test) => {
        // Sample pixels at expected character positions
        var centerPixel = test.PixelSampler.Sample(960, 540);
        Assert.NotEqual(backgroundColor, centerPixel, "Expected text pixel at center");
    }
};
```

### Visual Verification

Run TestApp to visually verify text rendering:

```powershell
dotnet run --project TestApp/TestApp.csproj -- --filter=HelloWorld
```

---

## Troubleshooting

### Text Not Visible
- Verify Position is within viewport bounds (0-1920 for X, 0-1080 for Y)
- Check AnchorPoint alignment (center = 0,0; top-left = -1,-1)
- Ensure TextElement is activated and added to component hierarchy

### Text Cut Off
- Text extends beyond SizeConstraints (expected behavior, no clipping in MVP)
- Adjust Position to accommodate full text width

### Blurry/Pixelated Text
- Using Scale > 1 causes bitmap scaling artifacts (expected behavior)
- Use larger base font size for clearer large text (future enhancement)

### Missing Characters
- Character not in ASCII printable range (32-126)?
- Check for typos or special characters outside MVP character set

---

## Next Steps

After implementing this MVP, future enhancements include:

1. **Multiple Fonts**: Support custom fonts via FontDefinition
2. **Text Styling**: Color, alignment, font size selection
3. **Multi-Line Text**: Automatic line wrapping and vertical spacing
4. **SDF Rendering**: Signed Distance Field for scale-independent crisp text
5. **Unicode Support**: Extended character sets (emoji, accents, CJK)
6. **Rich Text Markup**: Inline styling (<b>bold</b>, <i>italic</i>, colors)
7. **Text Measurement API**: Calculate text dimensions before rendering
8. **Dynamic Text Updates**: Efficient runtime text changes

---

## Additional Resources

- **Specification**: [spec.md](spec.md) - Complete feature specification
- **Data Model**: [data-model.md](data-model.md) - Entity relationships and data structures
- **API Contracts**: [contracts/](contracts/) - Interface definitions and schemas
- **Testing Guide**: `src/GameEngine/Testing/README.md` - Integration testing patterns

---

**Quickstart Guide Complete** ✅  
**Ready to implement text rendering!**
