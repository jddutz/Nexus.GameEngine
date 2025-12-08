# Research: PerformanceMonitor UI Template

**Feature**: 015-performance-monitor  
**Date**: 2025-12-07  
**Status**: Research Complete - BLOCKED by Dependencies

## Executive Summary

Implementation of PerformanceMonitor UI template can proceed with minor additions. TextRenderer component exists for text rendering. Layout controllers (VerticalLayout, HorizontalLayout) need to be implemented as part of this spec - the base LayoutController class exists but concrete implementations were removed in a previous refactor.

**Recommendation**: Implement VerticalLayout and HorizontalLayout controllers as part of this spec to enable template composition.

## Research Tasks

### Task 1: Verify Text Rendering Implementation Status

**Question**: Is text rendering component implemented?

**Finding**: ✅ **IMPLEMENTED** (as TextRenderer)
- Location: `src/GameEngine/GUI/TextRenderer.cs`
- Component name: `TextRenderer` (not TextElement)
- Implements: IDrawable interface
- Key properties:
  - `Text` (string) - with [ComponentProperty] and [TemplateProperty]
  - `FontDefinition` (FontDefinition?) - for font selection
  - `Color` (Vector4D<float>) - text color
  - `Visible` (bool) - visibility toggle
- Template support: Properties support template configuration
- Rendering: Uses font atlas and generates draw commands per glyph

**Evidence**:
```csharp
public partial class TextRenderer : Component, IDrawable
{
    [ComponentProperty]
    [TemplateProperty]
    protected string _text = string.Empty;
    
    [ComponentProperty]
    [TemplateProperty]
    protected FontDefinition? _fontDefinition;
    // ...
}
```

**Decision**: Use TextRenderer for displaying performance metrics.

**Rationale**: TextRenderer provides all required functionality - text property binding, font support, and rendering integration.

---

### Task 2: Verify Layout Controller Implementation Status

**Question**: Are VerticalLayout and HorizontalLayout controllers implemented?

**Finding**: ⚠️ **PARTIALLY IMPLEMENTED** - Infrastructure exists, concrete controllers missing
- Base class exists: `src/GameEngine/GUI/Layout/LayoutController.cs`
- Enums exist: `SpacingMode.cs`, `VerticalLayoutMode.cs`, `HorizontalLayoutMode.cs`
- Interface exists: `ILayout.cs`
- Concrete implementations: **NOT FOUND** (VerticalLayout, HorizontalLayout)

**Evidence**:
```csharp
// LayoutController base class exists
public abstract partial class LayoutController : Component
{
    public abstract void UpdateLayout(UserInterfaceElement container);
}

// Enums exist for configuration
public enum SpacingMode { Stacked, Justified, Distributed }
```

**Impact**: Cannot automatically arrange text elements without concrete layout controllers.

**Alternatives Considered**:
1. **Manual positioning**: Position each TextRenderer individually using absolute coordinates
   - ❌ Rejected: Defeats template composition purpose, brittle, no automatic spacing
2. **Hardcode positions in template**: Calculate Y offsets manually
   - ❌ Rejected: Not maintainable, breaks when adding/removing metrics

**Decision**: Implement VerticalLayout and HorizontalLayout controllers as part of this spec.

**Rationale**: 
- Infrastructure already exists (base class, enums, interfaces)
- Layout controllers are simple enough to implement within this spec
- Essential for clean template design and maintainability
- Vertical layout is primary requirement; horizontal layout useful for future features

---

### Task 3: Verify StringFormatConverter Availability

**Question**: Does the project have StringFormatConverter or equivalent for numeric-to-string conversion in bindings?

**Finding**: ✅ **EXISTS**
- Location: `src/GameEngine/Data/StringFormatConverter.cs`
- Implements: `IValueConverter` interface
- Usage: Already used in `PropertyBinding.cs` for format string conversion

**Evidence**:
```csharp
// From src/GameEngine/Data/StringFormatConverter.cs
public class StringFormatConverter : IValueConverter
{
    public StringFormatConverter(string format) { ... }
}

// From src/GameEngine/Components/PropertyBinding.cs
var converter = new StringFormatConverter(format);
```

**Decision**: StringFormatConverter is available and ready for use in template bindings.

**Rationale**: No implementation needed, existing converter supports required formatting.

---

### Task 4: Verify PerformanceMonitor Component Properties

**Question**: What properties does PerformanceMonitor expose for binding?

**Finding**: ✅ **FULLY IMPLEMENTED**
- Location: `src/GameEngine/Performance/PerformanceMonitor.cs`
- Component properties (with [ComponentProperty] attribute):
  - `CurrentFps` (double)
  - `AverageFps` (double)
  - `CurrentFrameTimeMs` (double)
  - `AverageFrameTimeMs` (double)
  - `MinFrameTimeMs` (double)
  - `MaxFrameTimeMs` (double)
  - `PerformanceWarning` (bool)
  - `UpdateTimeMs` (string)
  - `RenderTimeMs` (string)
  - `ResourceLoadTimeMs` (string)
  - `PerformanceSummary` (string) - pre-formatted multi-line summary

**Template Properties**:
- `Enabled` (bool) - default true
- `WarningThresholdMs` (double) - default 6.67ms (150 FPS)
- `AverageFrameCount` (int) - default 60 frames
- `UpdateIntervalSeconds` (double) - default 0.5s

**Decision**: PerformanceMonitor provides all required data properties for overlay display.

**Rationale**: Component is feature-complete with both individual metrics and pre-formatted summary string.

---

### Task 5: Property Binding Best Practices

**Question**: What are best practices for binding TextElement.Text to PerformanceMonitor properties?

**Finding**: Use `Binding.FromParent<T>()` pattern with optional converters

**Pattern**:
```csharp
// Binding to pre-formatted string (simplest)
new TextRendererTemplate 
{
    Bindings = 
    {
        Text = Binding.FromParent<PerformanceMonitor>(m => m.PerformanceSummary)
    }
}

// Binding to numeric property with formatting
new TextRendererTemplate 
{
    Bindings = 
    {
        Text = Binding.FromParent<PerformanceMonitor>(m => m.CurrentFps)
                      .WithConverter(new StringFormatConverter("FPS: {0:F1}"))
    }
}

// Binding to boolean for visibility
new TextRendererTemplate 
{
    Bindings = 
    {
        Visible = Binding.FromParent<PerformanceMonitor>(m => m.PerformanceWarning)
    }
}
```

**Best Practices**:
1. **Prefer pre-formatted properties**: Use `PerformanceSummary` for multi-line display to minimize bindings
2. **Use format strings**: Apply StringFormatConverter for consistent numeric formatting
3. **Minimize binding count**: Fewer bindings = better performance and simpler debugging
4. **One-way bindings**: Performance metrics are read-only, use `FromParent()` not `TwoWay()`

**Decision**: Use individual bindings to specific metrics (CurrentFps, AverageFps, etc.) for better layout control.

**Rationale**: VerticalLayout requires separate TextRenderer components for each metric line. Using individual bindings allows each line to be positioned independently and provides flexibility for future customization (e.g., color-coding warnings).

---

### Task 6: Template Composition Patterns

**Question**: How should PerformanceMonitor template be structured hierarchically?

**Finding**: Root should be UserInterfaceElement, not PerformanceMonitor

**Architecture Decision**:
```
UserInterfaceElementTemplate (root container)
├── PerformanceMonitorTemplate (data source component)
├── VerticalLayoutControllerTemplate (layout controller sibling)
└── TextRendererTemplate[] (child elements for metrics)
    └── PropertyBindings to PerformanceMonitor (via FromParent)
```

**Rationale**:
- **UserInterfaceElement root**: Provides positioning, alignment, offset for overlay placement
- **PerformanceMonitor as sibling**: Provides data source, controller can access via parent
- **VerticalLayoutController**: Arranges sibling TextRenderer children vertically
- **TextRenderers**: Render individual metrics via bindings, positioned by controller

**Alternative Considered**: Make PerformanceMonitor the root
- ❌ Rejected: PerformanceMonitor doesn't extend UserInterfaceElement, can't position overlay

**Decision**: Use UserInterfaceElement as root with PerformanceMonitor, VerticalLayoutController, and TextRenderer children as siblings.

---

### Task 7: Performance Overhead Analysis

**Question**: What is acceptable performance overhead for the overlay itself?

**Performance Budget**:
- Target frame time: 16.67ms (60 FPS)
- Warning threshold: 6.67ms (150 FPS target)
- Overlay budget: <1ms per frame

**Breakdown**:
- Property binding updates: ~0.1ms (deferred, batched)
- Text rendering: ~0.5ms (cached geometry, minimal re-generation)
- Layout calculation: ~0.1ms (only on size changes)
- Total overhead: ~0.7ms acceptable

**Mitigation Strategies**:
1. **Update interval**: Default 0.5s reduces update frequency (not per-frame)
2. **Deferred updates**: ComponentProperty system batches changes
3. **Text caching**: TextRenderer generates geometry per-frame but reuses font atlas
4. **Layout caching**: VerticalLayoutController only recalculates on property/child changes

**Decision**: 0.5s update interval with deferred property updates is acceptable.

**Rationale**: Performance monitoring doesn't require real-time updates, 500ms refresh is sufficient.

---

## Technology Decisions

### Decision 1: Template Composition Over Custom Component

**Decision**: Implement as template composition, not custom PerformanceMonitorUI component

**Rationale**: 
- Demonstrates template system capabilities
- Maximizes component reusability
- Easier to customize without code changes
- Aligns with component-based architecture principle

**Alternatives Considered**:
- Custom component: ❌ Rejected - adds complexity, reduces flexibility

---

### Decision 2: Property Bindings for Data Synchronization

**Decision**: Use declarative PropertyBindings in template, not manual wiring

**Rationale**:
- Type-safe compile-time checking
- Automatic lifecycle management (activate/deactivate)
- No memory leaks from manual subscriptions
- Clean separation of data and presentation

**Alternatives Considered**:
- Manual PropertyChanged subscriptions: ❌ Rejected - error-prone, manual cleanup required
- Direct property access: ❌ Rejected - tight coupling, no automatic updates

---

### Decision 3: Minimal Styling for MVP

**Decision**: Defer advanced styling (colors, fonts, backgrounds) to later iteration

**Rationale**:
- Focus on functional overlay first
- Reduces dependencies (no custom shader/material system needed)
- Default appearance sufficient for development tools
- Can add styling incrementally

**Future Enhancements**:
- Background panel for readability
- Color-coded warnings (red for performance issues)
- Custom fonts or text sizes
- Transparency/opacity controls

---

## Implementation Requirements

### REQUIREMENT 1: VerticalLayoutController Implementation

**Status**: ⚠️ **TO BE IMPLEMENTED** (as part of this spec)

**Required For**: Automatic vertical arrangement of text renderers

**Base Class**: `src/GameEngine/GUI/Layout/LayoutController.cs` (already exists)

**Action Required**:
1. Implement VerticalLayoutController extending LayoutController
2. Properties needed:
   - `ItemSpacing` (float?) - gap between children
   - `Alignment` (float) - cross-axis alignment (-1.0 to 1.0)
   - `SpacingMode` (SpacingMode enum) - Stacked/Justified/Distributed
3. Override `UpdateLayout(UserInterfaceElement container)` method
4. Logic: Position sibling children vertically based on spacing mode
5. Create template support (VerticalLayoutControllerTemplate)

**Estimated Effort**: 4-6 hours (infrastructure exists, just need concrete implementation)

---

### REQUIREMENT 2: HorizontalLayoutController Implementation

**Status**: ⚠️ **TO BE IMPLEMENTED** (as part of this spec, for completeness)

**Required For**: Future horizontal layouts (not strictly needed for PerformanceMonitor)

**Base Class**: `src/GameEngine/GUI/Layout/LayoutController.cs` (already exists)

**Action Required**:
1. Implement HorizontalLayoutController extending LayoutController
2. Properties: Same as VerticalLayoutController but for horizontal axis
3. Override `UpdateLayout(UserInterfaceElement container)` method
4. Logic: Position sibling children horizontally
5. Create template support (HorizontalLayoutControllerTemplate)

**Estimated Effort**: 4-6 hours (mirror of vertical implementation)

**Decision**: Implement both controllers together for completeness and symmetry.

---

## Next Steps

1. **PROCEED TO PHASE 1** ✅ - All research complete, implementation path clear
2. **Phase 1 Deliverables**:
   - Create data-model.md (VerticalLayoutController, HorizontalLayoutController, PerformanceMonitor template composition)
   - Create quickstart.md (usage guide for adding performance overlay)
   - Update agent context with new components
3. **Implementation Order**:
   - VerticalLayoutController (required for overlay)
   - HorizontalLayoutController (for completeness)
   - PerformanceMonitor template (composition of existing + new components)
   - Tests and documentation
4. **No External Blockers**: All dependencies either exist (TextRenderer, PerformanceMonitor) or will be created within this spec (layout controllers)

---

## Research Validation

✅ All NEEDS CLARIFICATION items resolved  
✅ Technology choices validated (bindings, templates, composition)  
✅ Architecture decisions documented  
✅ TextRenderer exists for text rendering  
✅ Layout controller infrastructure exists (base class, enums, interfaces)  
✅ PerformanceMonitor component exists with all required properties  
⚠️ **ACTION REQUIRED** - Implement VerticalLayoutController and HorizontalLayoutController as part of this spec  

**Outcome**: Research phase complete. Implementation unblocked. Proceeding to Phase 1 (design and contracts).
