# Data Model: PerformanceMonitor UI Template

**Feature**: 015-performance-monitor  
**Date**: 2025-12-07  
**Status**: Design Complete

## Overview

This document defines the data model for the PerformanceMonitor UI overlay template system, including two new layout controllers (VerticalLayoutController, HorizontalLayoutController) and the template composition structure.

## Entities

### VerticalLayoutController

**Type**: Component extending LayoutController  
**Purpose**: Arranges sibling UserInterfaceElement children vertically with configurable spacing and alignment  
**Namespace**: `Nexus.GameEngine.GUI.Layout`

**Properties**:

| Property | Type | Default | Description | Template | Component |
|----------|------|---------|-------------|----------|-----------|
| ItemSpacing | float? | null | Fixed gap (pixels) between adjacent children. Null = no spacing | ✓ | ✓ |
| Alignment | float | 0.0f | Cross-axis (horizontal) alignment: -1.0 (left) to 1.0 (right), 0.0 = center | ✓ | ✓ |
| Spacing | SpacingMode | Stacked | Distribution strategy: Stacked (default), Justified, Distributed | ✓ | ✓ |

**Relationships**:
- **Parent**: Must be child of UserInterfaceElement (acts as sibling to elements being laid out)
- **Siblings**: Operates on sibling UserInterfaceElement components
- **Base Class**: Extends LayoutController

**Behavior**:
- Overrides `UpdateLayout(UserInterfaceElement container)` to position children vertically
- Called automatically when container's children collection changes or relevant properties update
- Positions children based on SpacingMode:
  - **Stacked**: Sequential positioning with ItemSpacing gap, aligned using Alignment value
  - **Justified**: First at top, last at bottom, equal spacing between
  - **Distributed**: Equal spacing before, between, and after all children
- Excludes zero-height children from layout calculations
- Single-child layouts with Justified/Distributed delegate to Stacked behavior

**Validation Rules**:
- ItemSpacing must be >= 0 if not null
- Alignment must be in range [-1.0, 1.0]
- Must be attached to a UserInterfaceElement parent

---

### HorizontalLayoutController

**Type**: Component extending LayoutController  
**Purpose**: Arranges sibling UserInterfaceElement children horizontally with configurable spacing and alignment  
**Namespace**: `Nexus.GameEngine.GUI.Layout`

**Properties**:

| Property | Type | Default | Description | Template | Component |
|----------|------|---------|-------------|----------|-----------|
| ItemSpacing | float? | null | Fixed gap (pixels) between adjacent children. Null = no spacing | ✓ | ✓ |
| Alignment | float | 0.0f | Cross-axis (vertical) alignment: -1.0 (top) to 1.0 (bottom), 0.0 = center | ✓ | ✓ |
| Spacing | SpacingMode | Stacked | Distribution strategy: Stacked (default), Justified, Distributed | ✓ | ✓ |

**Relationships**:
- **Parent**: Must be child of UserInterfaceElement (acts as sibling to elements being laid out)
- **Siblings**: Operates on sibling UserInterfaceElement components
- **Base Class**: Extends LayoutController

**Behavior**:
- Overrides `UpdateLayout(UserInterfaceElement container)` to position children horizontally
- Mirror of VerticalLayoutController but operates on horizontal axis
- Same SpacingMode behavior but applied horizontally
- Excludes zero-width children from layout calculations

**Validation Rules**:
- ItemSpacing must be >= 0 if not null
- Alignment must be in range [-1.0, 1.0]
- Must be attached to a UserInterfaceElement parent

---

### PerformanceMonitorTemplate

**Type**: Template composition (not a component class)  
**Purpose**: Declarative configuration for PerformanceMonitor UI overlay  
**Namespace**: `Nexus.GameEngine.Templates`

**Structure**:
```
UserInterfaceElementTemplate (root)
├── Position, Alignment, Offset (positioning properties)
├── Children:
│   ├── PerformanceMonitorTemplate (data source)
│   │   └── Enabled, UpdateIntervalSeconds, WarningThresholdMs
│   ├── VerticalLayoutControllerTemplate (layout controller)
│   │   └── ItemSpacing, Alignment, Spacing
│   └── TextRendererTemplate[] (metric displays)
│       └── Text (bound to PerformanceMonitor properties)
```

**Template Properties** (UserInterfaceElement root):

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| Position | Vector2D<float> | (10, 10) | Screen position for overlay |
| Alignment | Alignment | TopLeft | Anchor point for positioning |
| Offset | Vector2D<float> | (0, 0) | Additional offset from alignment point |
| Size | Vector2D<float> | Auto | Container size (auto-sized to content) |

**Child: PerformanceMonitorTemplate**:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| Enabled | bool | true | Enable/disable performance monitoring |
| UpdateIntervalSeconds | double | 0.5 | Update frequency for metrics |
| WarningThresholdMs | double | 6.67 | Frame time threshold for warnings (ms) |
| AverageFrameCount | int | 60 | Number of frames for averaging |

**Child: VerticalLayoutControllerTemplate**:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| ItemSpacing | float? | 2.0 | Gap between text lines (pixels) |
| Alignment | float | -1.0 | Left-align text elements |
| Spacing | SpacingMode | Stacked | Stack text lines sequentially |

**Children: TextRendererTemplate[]** (one per metric):

| Property | Type | Binding Source | Description |
|----------|------|----------------|-------------|
| Text | string | PerformanceMonitor.CurrentFps (with converter) | Formatted FPS display |
| Text | string | PerformanceMonitor.AverageFps (with converter) | Formatted average FPS |
| Text | string | PerformanceMonitor.CurrentFrameTimeMs (with converter) | Frame time display |
| Text | string | PerformanceMonitor.UpdateTimeMs | Update subsystem time |
| Text | string | PerformanceMonitor.RenderTimeMs | Render subsystem time |
| FontDefinition | FontDefinition? | FontDefinitions.RobotoNormal | Font to use |
| Color | Vector4D<float> | (1, 1, 1, 1) | White text |
| Visible | bool | true | Always visible |

---

## Property Bindings

### Binding Pattern: FromParent with Converters

All TextRenderer elements bind to PerformanceMonitor properties via `Binding.FromParent<PerformanceMonitor>()`:

```csharp
// Example: FPS binding with formatting
new TextRendererTemplate
{
    Bindings = 
    {
        Text = Binding.FromParent<PerformanceMonitor>(m => m.CurrentFps)
                      .WithConverter(new StringFormatConverter("FPS: {0:F1}"))
    }
}
```

### Binding Lifecycle

1. **Template Configuration**: Bindings defined in template Bindings property
2. **Component Creation**: ContentManager creates components from templates
3. **OnLoad**: Template properties copied to component properties
4. **OnActivate**: Bindings activated, subscriptions created to source properties
5. **Runtime**: Property changes propagate from PerformanceMonitor → TextRenderer.Text
6. **OnDeactivate**: Bindings deactivated, subscriptions removed (prevents leaks)

### Converter Usage

StringFormatConverter formats numeric values as strings:

| Source Property | Converter Format | Example Output |
|-----------------|------------------|----------------|
| CurrentFps | "FPS: {0:F1}" | "FPS: 60.5" |
| AverageFps | "Avg: {0:F1}" | "Avg: 58.2" |
| CurrentFrameTimeMs | "Frame: {0:F2}ms" | "Frame: 16.67ms" |
| UpdateTimeMs | "Update: {0}ms" | "Update: 5.23ms" |
| RenderTimeMs | "Render: {0}ms" | "Render: 8.45ms" |

---

## State Transitions

### VerticalLayoutController Lifecycle

```
Created → Attached to Parent → OnLoad (template properties) → OnActivate
    ↓
Active: Listening for container child changes
    ↓
OnChildrenChanged → UpdateLayout() → Position siblings
    ↓
Property Changed (ItemSpacing, Alignment, Spacing) → UpdateLayout()
    ↓
OnDeactivate → Cleanup listeners
```

### PerformanceMonitor Template Lifecycle

```
Template Defined → ContentManager.CreateInstance()
    ↓
Root UserInterfaceElement created
    ↓
Children created: PerformanceMonitor, VerticalLayoutController, TextRenderer[]
    ↓
OnLoad: Template properties → Component properties
    ↓
OnActivate: 
  - PerformanceMonitor starts profiling
  - Bindings activate (TextRenderer.Text ← PerformanceMonitor properties)
  - VerticalLayoutController starts layout updates
    ↓
Runtime:
  - PerformanceMonitor updates metrics every 0.5s
  - Property changes propagate via bindings
  - TextRenderer.Text updates trigger text regeneration
  - VerticalLayoutController positions elements
    ↓
OnDeactivate:
  - Bindings deactivate
  - PerformanceMonitor stops profiling
  - Cleanup
```

---

## Data Flow

```
IProfiler (game engine service)
    ↓ (frame timing data)
PerformanceMonitor.OnUpdate(deltaTime)
    ↓ (every 0.5s)
PerformanceMonitor calculates metrics
    ↓ (property updates)
[ComponentProperty] setters queue deferred updates
    ↓ (next frame)
ComponentPropertyUpdater.ApplyUpdates()
    ↓ (PropertyChanged events)
PropertyBinding subscriptions triggered
    ↓ (converter applied)
TextRenderer.Text property updated
    ↓ (OnTextChanged)
TextRenderer.UpdateSize() + invalidate geometry
    ↓ (next render)
TextRenderer.GetDrawCommands() generates draw commands
    ↓
Renderer executes commands → text appears on screen
```

---

## Validation Rules Summary

### VerticalLayoutController
- ✓ ItemSpacing >= 0 if not null
- ✓ Alignment ∈ [-1.0, 1.0]
- ✓ Must be child of UserInterfaceElement
- ✓ Operates on sibling UserInterfaceElement components only

### HorizontalLayoutController
- ✓ ItemSpacing >= 0 if not null
- ✓ Alignment ∈ [-1.0, 1.0]
- ✓ Must be child of UserInterfaceElement
- ✓ Operates on sibling UserInterfaceElement components only

### PerformanceMonitor Template
- ✓ UpdateIntervalSeconds > 0
- ✓ WarningThresholdMs > 0
- ✓ AverageFrameCount > 0
- ✓ All bindings target valid source properties
- ✓ At least one TextRenderer child for metrics display

---

## Constraints

### Performance Constraints
- Layout updates: Only on child changes or property updates (not per-frame)
- Text regeneration: Only when Text property changes (not per-frame)
- Binding updates: Deferred and batched via ComponentPropertyUpdater
- Total overlay overhead: <1ms per frame target

### Architectural Constraints
- Layout controllers are siblings, not containers
- TextRenderer components are children of root UserInterfaceElement
- PerformanceMonitor is sibling to TextRenderer components (provides data)
- Property bindings use FromParent<T>() to locate PerformanceMonitor in component tree

### Visual Constraints
- Text color: Default white (1,1,1,1) - configurable in template
- Font: Default Roboto Normal - configurable in template
- Background: None (transparent) - can add background panel in future iteration
- Z-order: UI render pass (appears on top of 3D scene)

---

## Extension Points

Future enhancements can extend this model:

1. **Background Panel**: Add UIElement with solid color behind text for readability
2. **Color-Coded Warnings**: Bind TextRenderer.Color to PerformanceMonitor.PerformanceWarning (red when true)
3. **Graph Visualization**: Add custom component for frame time graphs
4. **Interactive Controls**: Add buttons for toggling metrics or pausing monitoring
5. **Multiple Layouts**: Support switching between compact/detailed/minimal modes
6. **Custom Metrics**: Allow user-defined metrics via property bindings

---

## Testing Considerations

### Unit Tests
- VerticalLayoutController.UpdateLayout() positions children correctly
- HorizontalLayoutController.UpdateLayout() positions children correctly
- SpacingMode enum values produce expected layouts
- Alignment property affects cross-axis positioning
- Zero-size children excluded from layout

### Integration Tests
- Template instantiation creates all components
- Property bindings activate and sync correctly
- PerformanceMonitor metrics update at configured interval
- TextRenderer displays formatted values
- Layout controller positions text elements vertically
- Overlay appears at configured position/alignment
- Toggling Visible property shows/hides overlay

### Performance Tests
- Layout update overhead <0.1ms
- Text rendering overhead <0.5ms
- Binding update overhead <0.1ms
- Total overlay overhead <1ms per frame
