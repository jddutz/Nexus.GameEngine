# Quick Start: PerformanceMonitor UI Overlay

**Feature**: 015-performance-monitor  
**Audience**: Developers using Nexus.GameEngine  
**Time**: 5 minutes

## Overview

Add a real-time performance overlay to your game showing FPS, frame time, and subsystem timings using the PerformanceMonitor template.

## Prerequisites

- Nexus.GameEngine project with window and rendering set up
- Basic understanding of template-based component composition
- Application startup configured with required services

## Basic Usage

### Step 1: Add Performance Overlay to Your Scene

```csharp
using Nexus.GameEngine.Templates;
using Nexus.GameEngine.GUI;
using Nexus.GameEngine.Performance;
using Nexus.GameEngine.GUI.Layout;

// In your scene initialization or startup template:
var performanceOverlay = new UserInterfaceElementTemplate
{
    Position = new Vector2D<float>(10, 10),
    Alignment = Alignment.TopLeft,
    
    Subcomponents = new Template[]
    {
        // Data source
        new PerformanceMonitorTemplate
        {
            Enabled = true,
            UpdateIntervalSeconds = 0.5,  // Update twice per second
            WarningThresholdMs = 6.67     // 150 FPS threshold
        },
        
        // Layout controller
        new VerticalLayoutControllerTemplate
        {
            ItemSpacing = 2.0f,
            Alignment = -1.0f,  // Left-align
            Spacing = SpacingMode.Stacked
        },
        
        // Metric displays
        new TextRendererTemplate
        {
            Bindings = 
            {
                Text = Binding.FromParent<PerformanceMonitor>(m => m.CurrentFps)
                              .WithConverter(new StringFormatConverter("FPS: {0:F1}"))
            },
            Color = new Vector4D<float>(1, 1, 1, 1)  // White
        },
        
        new TextRendererTemplate
        {
            Bindings = 
            {
                Text = Binding.FromParent<PerformanceMonitor>(m => m.AverageFps)
                              .WithConverter(new StringFormatConverter("Avg: {0:F1}"))
            },
            Color = new Vector4D<float>(0.7f, 0.7f, 0.7f, 1)  // Gray
        },
        
        new TextRendererTemplate
        {
            Bindings = 
            {
                Text = Binding.FromParent<PerformanceMonitor>(m => m.CurrentFrameTimeMs)
                              .WithConverter(new StringFormatConverter("Frame: {0:F2}ms"))
            },
            Color = new Vector4D<float>(1, 1, 1, 1)
        },
        
        new TextRendererTemplate
        {
            Bindings = 
            {
                Text = Binding.FromParent<PerformanceMonitor>(m => m.UpdateTimeMs)
            },
            Color = new Vector4D<float>(0.5f, 1, 0.5f, 1)  // Green
        },
        
        new TextRendererTemplate
        {
            Bindings = 
            {
                Text = Binding.FromParent<PerformanceMonitor>(m => m.RenderTimeMs)
            },
            Color = new Vector4D<float>(0.5f, 0.5f, 1, 1)  // Blue
        }
    }
};

// Create the overlay instance
var overlay = ContentManager.CreateInstance(performanceOverlay);
scene.AddChild(overlay);
```

### Step 2: Run Your Application

Build and run. You should see a performance overlay in the top-left corner showing:
```
FPS: 60.5
Avg: 58.2
Frame: 16.67ms
Update: 5.23ms
Render: 8.45ms
```

## Customization

### Change Position

```csharp
// Top-right corner
Position = new Vector2D<float>(10, 10),
Alignment = Alignment.TopRight,

// Bottom-left corner
Position = new Vector2D<float>(10, 10),
Alignment = Alignment.BottomLeft,

// Center of screen
Position = new Vector2D<float>(0, 0),
Alignment = Alignment.Center,
```

### Adjust Update Frequency

```csharp
new PerformanceMonitorTemplate
{
    UpdateIntervalSeconds = 1.0,  // Update once per second (less overhead)
    // or
    UpdateIntervalSeconds = 0.1,  // Update 10 times per second (more responsive)
}
```

### Change Text Spacing

```csharp
new VerticalLayoutControllerTemplate
{
    ItemSpacing = 5.0f,  // More space between lines
    Alignment = 0.0f,    // Center-align instead of left
}
```

### Show/Hide Overlay

```csharp
// Toggle visibility at runtime
overlay.Visible = false;  // Hide
overlay.Visible = true;   // Show

// Or use key binding (in your input handler):
if (Input.IsKeyPressed(Key.F10))
{
    overlay.Visible = !overlay.Visible;
}
```

### Change Warning Threshold

```csharp
new PerformanceMonitorTemplate
{
    WarningThresholdMs = 16.67,  // 60 FPS threshold
    // or
    WarningThresholdMs = 33.33,  // 30 FPS threshold
}
```

## Advanced Usage

### Add Performance Warning Indicator

```csharp
// Add a warning text that appears when performance drops
new TextRendererTemplate
{
    Bindings = 
    {
        Text = Binding.FromParent<PerformanceMonitor>(m => m.PerformanceWarning)
                      .WithConverter(new BoolToStringConverter("⚠ PERFORMANCE WARNING", "")),
        Visible = Binding.FromParent<PerformanceMonitor>(m => m.PerformanceWarning)
    },
    Color = new Vector4D<float>(1, 0, 0, 1)  // Red
}
```

### Custom Font

```csharp
new TextRendererTemplate
{
    FontDefinition = FontDefinitions.YourCustomFont,
    // ... bindings ...
}
```

### Horizontal Layout (for compact overlay)

```csharp
// Use HorizontalLayoutController instead
new HorizontalLayoutControllerTemplate
{
    ItemSpacing = 10.0f,
    Alignment = 0.0f,  // Vertical center
    Spacing = SpacingMode.Stacked
},

// Text elements will arrange horizontally: "FPS: 60.5 | Frame: 16.67ms | ..."
```

### Multiple Metrics in One Line

```csharp
// Combine multiple metrics with custom formatting
new TextRendererTemplate
{
    Bindings = 
    {
        Text = Binding.FromParent<PerformanceMonitor>(m => m)
                      .WithConverter(new CustomPerformanceConverter())
        // CustomPerformanceConverter could format as: "60.5 FPS (16.67ms)"
    }
}
```

## Common Issues

### Overlay Not Appearing

**Check**:
1. Is the overlay added to the scene? `scene.AddChild(overlay)`
2. Is `Visible` property true?
3. Is `Position` within screen bounds?
4. Is `PerformanceMonitor.Enabled` set to true?

### Metrics Show "0.00" or Wrong Values

**Check**:
1. Is profiler enabled in application settings?
2. Is `UpdateIntervalSeconds` too long? (Try 0.5 or less)
3. Are property bindings correctly targeting PerformanceMonitor?

### Text Not Updating

**Check**:
1. Are bindings activated? (Should happen automatically in OnActivate)
2. Is ComponentPropertyUpdater system running?
3. Is PerformanceMonitor.OnUpdate being called each frame?

### Layout Not Working

**Check**:
1. Is VerticalLayoutController added as sibling to TextRenderer elements?
2. Are TextRenderer elements children of the same parent as the controller?
3. Is `ItemSpacing` set to a non-negative value?

## Performance Tips

1. **Update Interval**: Use 0.5-1.0 seconds for production, 0.1 for debugging
2. **Metric Count**: Limit to 5-10 text elements to minimize overhead
3. **Font Caching**: TextRenderer caches font atlas, no per-frame allocation
4. **Layout Caching**: Layout only updates when children or properties change

## Next Steps

- Add custom metrics by extending PerformanceMonitor with your own properties
- Create custom converters for specialized formatting
- Add background panel for better readability
- Implement toggle key binding for show/hide
- Explore color-coding for performance warnings

## Reference

- `PerformanceMonitor`: `src/GameEngine/Performance/PerformanceMonitor.cs`
- `VerticalLayoutController`: `src/GameEngine/GUI/Layout/VerticalLayoutController.cs`
- `TextRenderer`: `src/GameEngine/GUI/TextRenderer.cs`
- `PropertyBinding`: `src/GameEngine/Components/PropertyBinding.cs`
