# Component Contracts: PerformanceMonitor UI

**Feature**: 015-performance-monitor  
**Date**: 2025-12-07

This directory contains the interface contracts for components in the PerformanceMonitor UI template system.

## VerticalLayoutController Contract

```csharp
namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// Layout controller that arranges sibling UserInterfaceElement children vertically.
/// </summary>
public partial class VerticalLayoutController : LayoutController
{
    /// <summary>
    /// Fixed gap (pixels) between adjacent children. Null = no spacing.
    /// </summary>
    [TemplateProperty]
    [ComponentProperty]
    public float? ItemSpacing { get; set; }
    
    /// <summary>
    /// Cross-axis (horizontal) alignment: -1.0 (left) to 1.0 (right), 0.0 = center.
    /// </summary>
    [TemplateProperty]
    [ComponentProperty]
    public float Alignment { get; set; } = 0.0f;
    
    /// <summary>
    /// Distribution strategy: Stacked (sequential), Justified (space-between), Distributed (space-evenly).
    /// </summary>
    [TemplateProperty]
    [ComponentProperty]
    public SpacingMode Spacing { get; set; } = SpacingMode.Stacked;
    
    /// <summary>
    /// Updates the layout of the specified container's children.
    /// Positions children vertically based on Spacing mode.
    /// </summary>
    public override void UpdateLayout(UserInterfaceElement container);
}
```

**Behavior Contract**:
- MUST position children along vertical axis only
- MUST exclude zero-height children from layout calculations
- MUST respect SpacingMode for positioning strategy
- MUST apply Alignment for cross-axis (horizontal) positioning
- MUST be called when container children collection changes
- MUST be called when ItemSpacing, Alignment, or Spacing properties change
- MUST NOT modify children's scale or rotation

**Validation Contract**:
- ItemSpacing MUST be >= 0 if not null
- Alignment MUST be in range [-1.0, 1.0]
- Parent MUST be UserInterfaceElement

---

## HorizontalLayoutController Contract

```csharp
namespace Nexus.GameEngine.GUI.Layout;

/// <summary>
/// Layout controller that arranges sibling UserInterfaceElement children horizontally.
/// </summary>
public partial class HorizontalLayoutController : LayoutController
{
    /// <summary>
    /// Fixed gap (pixels) between adjacent children. Null = no spacing.
    /// </summary>
    [TemplateProperty]
    [ComponentProperty]
    public float? ItemSpacing { get; set; }
    
    /// <summary>
    /// Cross-axis (vertical) alignment: -1.0 (top) to 1.0 (bottom), 0.0 = center.
    /// </summary>
    [TemplateProperty]
    [ComponentProperty]
    public float Alignment { get; set; } = 0.0f;
    
    /// <summary>
    /// Distribution strategy: Stacked (sequential), Justified (space-between), Distributed (space-evenly).
    /// </summary>
    [TemplateProperty]
    [ComponentProperty]
    public SpacingMode Spacing { get; set; } = SpacingMode.Stacked;
    
    /// <summary>
    /// Updates the layout of the specified container's children.
    /// Positions children horizontally based on Spacing mode.
    /// </summary>
    public override void UpdateLayout(UserInterfaceElement container);
}
```

**Behavior Contract**:
- MUST position children along horizontal axis only
- MUST exclude zero-width children from layout calculations
- MUST respect SpacingMode for positioning strategy
- MUST apply Alignment for cross-axis (vertical) positioning
- MUST be called when container children collection changes
- MUST be called when ItemSpacing, Alignment, or Spacing properties change
- MUST NOT modify children's scale or rotation

**Validation Contract**:
- ItemSpacing MUST be >= 0 if not null
- Alignment MUST be in range [-1.0, 1.0]
- Parent MUST be UserInterfaceElement

---

## Template Composition Contract

### PerformanceMonitor UI Template Structure

```csharp
UserInterfaceElementTemplate
{
    Position = Vector2D<float>,      // REQUIRED: Screen position
    Alignment = Alignment,            // REQUIRED: Anchor point
    Offset = Vector2D<float>,        // OPTIONAL: Additional offset
    
    Subcomponents = new Template[]
    {
        PerformanceMonitorTemplate   // REQUIRED: Data source
        {
            Enabled = bool,                    // REQUIRED
            UpdateIntervalSeconds = double,    // REQUIRED: > 0
            WarningThresholdMs = double,       // REQUIRED: > 0
            AverageFrameCount = int           // REQUIRED: > 0
        },
        
        VerticalLayoutControllerTemplate      // REQUIRED: Layout controller
        {
            ItemSpacing = float?,              // OPTIONAL: >= 0
            Alignment = float,                 // REQUIRED: [-1.0, 1.0]
            Spacing = SpacingMode             // REQUIRED
        },
        
        TextRendererTemplate[]                // REQUIRED: At least 1
        {
            Bindings = 
            {
                Text = Binding.FromParent<PerformanceMonitor>(...) // REQUIRED
            },
            Color = Vector4D<float>,          // OPTIONAL
            FontDefinition = FontDefinition?  // OPTIONAL
        }
    }
}
```

**Composition Contract**:
- Root MUST be UserInterfaceElement (for positioning)
- MUST contain exactly one PerformanceMonitor child
- MUST contain exactly one layout controller child (Vertical or Horizontal)
- MUST contain at least one TextRenderer child
- All TextRenderer bindings MUST use `FromParent<PerformanceMonitor>()`
- TextRenderer children MUST be siblings of layout controller

---

## Property Binding Contract

### Binding Sources (PerformanceMonitor)

```csharp
// Available binding sources (all [ComponentProperty])
public double CurrentFps { get; }
public double AverageFps { get; }
public double CurrentFrameTimeMs { get; }
public double AverageFrameTimeMs { get; }
public double MinFrameTimeMs { get; }
public double MaxFrameTimeMs { get; }
public bool PerformanceWarning { get; }
public string UpdateTimeMs { get; }
public string RenderTimeMs { get; }
public string ResourceLoadTimeMs { get; }
public string PerformanceSummary { get; }
```

### Binding Targets (TextRenderer)

```csharp
public string Text { get; set; }           // Primary binding target
public Vector4D<float> Color { get; set; } // Optional color binding
public bool Visible { get; set; }          // Optional visibility binding
```

**Binding Contract**:
- Bindings MUST activate in OnActivate lifecycle
- Bindings MUST deactivate in OnDeactivate lifecycle
- Bindings MUST be one-way (source → target) for read-only metrics
- Bindings MUST use StringFormatConverter for numeric → string conversion
- Binding updates MUST be deferred via ComponentPropertyUpdater

---

## Lifecycle Contract

### Component Activation Order

1. UserInterfaceElement (root) OnLoad
2. Children OnLoad (PerformanceMonitor, LayoutController, TextRenderer[])
3. UserInterfaceElement OnActivate
4. Children OnActivate:
   - PerformanceMonitor.OnActivate → starts profiler
   - PropertyBindings activate → subscriptions created
   - LayoutController.OnActivate → starts listening for layout changes
5. Runtime updates:
   - PerformanceMonitor.OnUpdate → metrics update
   - PropertyChanged events → bindings propagate
   - TextRenderer.OnTextChanged → geometry invalidated
   - LayoutController.UpdateLayout → positions updated

### Deactivation Order

1. UserInterfaceElement OnDeactivate
2. Children OnDeactivate:
   - PropertyBindings deactivate → subscriptions removed
   - PerformanceMonitor.OnDeactivate → stops profiler
   - LayoutController.OnDeactivate → stops listening
3. Cleanup complete

**Contract Guarantees**:
- Bindings MUST NOT leak memory (subscriptions cleaned up in OnDeactivate)
- PerformanceMonitor MUST stop profiling when deactivated
- Layout updates MUST NOT occur when deactivated

---

## Performance Contract

### Timing Guarantees

| Operation | Maximum Time | Frequency |
|-----------|-------------|-----------|
| VerticalLayoutController.UpdateLayout() | 0.1ms | On child/property change |
| HorizontalLayoutController.UpdateLayout() | 0.1ms | On child/property change |
| Property binding update | 0.1ms total | On source property change |
| TextRenderer.GetDrawCommands() | 0.5ms | Per frame (if visible) |
| Total overlay overhead | 1.0ms | Per frame |

### Allocation Guarantees

- Layout controllers MUST NOT allocate per-frame
- Property bindings MUST use deferred updates (batched)
- TextRenderer MUST reuse font atlas (no per-frame allocation)
- Template instantiation allocates once at creation time

---

## Error Handling Contract

### Validation Errors

- Invalid ItemSpacing (< 0): Log warning, clamp to 0
- Invalid Alignment (outside [-1.0, 1.0]): Log warning, clamp to range
- Missing PerformanceMonitor: Binding logs warning, displays empty text
- Missing property in binding source: Binding logs warning, displays default value
- Zero or negative UpdateIntervalSeconds: Clamp to 0.1 minimum

### Runtime Errors

- Layout controller parent not UserInterfaceElement: Throw InvalidOperationException in OnActivate
- Property binding source not found: Log warning, binding returns default value
- Font not found: TextRenderer uses default font, logs warning

**Contract Guarantee**: No crashes from invalid configuration, all errors logged with actionable messages.
