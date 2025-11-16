# Research: Layout Alignment Refactor

**Feature**: Layout Alignment Refactor  
**Branch**: `005-layout-alignment-refactor`  
**Date**: November 14, 2025

## Overview

This document consolidates research findings for refactoring HorizontalLayout and VerticalLayout to use a unified Vector2D<float> Alignment property instead of separate single-axis alignment properties.

## Research Areas

### 1. Vector2D Alignment Pattern in UI Frameworks

**Research Question**: How do other UI frameworks handle multi-axis alignment? Is Vector2D<float> a common pattern?

**Findings**:
- **CSS Flexbox**: Uses separate `align-items` (cross-axis) and `justify-content` (main-axis) properties, similar to current implementation
- **SwiftUI**: Uses `Alignment` type which is a struct containing horizontal and vertical components
- **Flutter**: Uses separate `mainAxisAlignment` and `crossAxisAlignment` enums
- **Unity UI**: Uses `TextAnchor` enum with combined positions (UpperLeft, MiddleCenter, etc.) and `Vector2` for pivot points

**Decision**: Use Vector2D<float> with -1 to 1 range for alignment
**Rationale**: 
- Aligns with existing `Align` static class which already defines float constants in -1 to 1 range
- Provides flexibility for arbitrary alignment values (not limited to discrete enum values)
- Single property simplifies API surface while maintaining full 2D alignment capability
- Consistent with existing `Align.TopLeft`, `Align.MiddleCenter`, etc. Vector2D<float> constants

**Alternatives Considered**:
- **Separate properties**: Current approach, rejected because it increases API surface and doesn't align with `Align` class design
- **Enum-based alignment**: Rejected because it's less flexible and requires additional enum types
- **Percentage-based (0-1 range)**: Rejected because existing codebase uses -1 to 1 range consistently

---

### 2. Source Generator Compatibility with Vector2D<T>

**Research Question**: Can [ComponentProperty] and [TemplateProperty] attributes work with Vector2D<float>? Are there any special considerations?

**Findings**:
- **Existing Usage**: Container base class already uses Vector2D<float> for `_spacing` property with both `[ComponentProperty]` and `[TemplateProperty]` attributes
- **Source Generator Support**: AnimatedPropertyGenerator supports Silk.NET types including Vector2D<T> out of the box (listed in `AnimatedPropertyGenerator.cs` type support)
- **Interpolation**: Vector2D<float> has built-in interpolation support for animated properties

**Decision**: Use Vector2D<float> with both property attributes
**Rationale**: 
- Proven pattern in existing codebase (Container._spacing)
- Source generators already handle this type correctly
- Enables future animation support if needed

**Alternatives Considered**: None - this is the established pattern

---

### 3. Backward Compatibility Strategy

**Research Question**: How should we handle migration from single-axis alignment to Vector2D alignment?

**Findings**:
- **Current API**: HorizontalLayout uses `_alignment` (float) with VerticalAlignment constants, VerticalLayout uses `_alignment` (float) with HorizontalAlignment constants
- **Breaking Change**: Changing property type from float to Vector2D<float> is a breaking change
- **Deprecation Path**: HorizontalAlignment and VerticalAlignment static classes can be marked as obsolete with migration guidance

**Decision**: Accept breaking change with clear migration path
**Rationale**:
- Feature is marked as P1 priority, indicating it's core API change
- Spec does not require backward compatibility (P4 "Migration from Old API" user story has lower priority)
- Clean break is simpler than maintaining compatibility shims
- Migration is straightforward: `Align.Center` → `new Vector2D<float>(0, Align.Center)` or use existing `Align.MiddleCenter`

**Migration Guidance**:
```csharp
// OLD: HorizontalLayout with float alignment
layout.SetAlignment(Align.Center);

// NEW: HorizontalLayout with Vector2D alignment (Y component used)
layout.SetAlignment(new Vector2D<float>(0, Align.Middle));
// OR use predefined constant (any X value works, only Y is used)
layout.SetAlignment(Align.TopCenter); // Top vertical alignment

// OLD: VerticalLayout with float alignment  
layout.SetAlignment(Align.Left);

// NEW: VerticalLayout with Vector2D alignment (X component used)
layout.SetAlignment(new Vector2D<float>(Align.Left, 0));
// OR use predefined constant (any Y value works, only X is used)
layout.SetAlignment(Align.TopLeft); // Left horizontal alignment
```

**Alternatives Considered**:
- **Compatibility shim**: Provide overloads accepting float, rejected due to added complexity
- **Gradual migration**: Support both properties temporarily, rejected to avoid API confusion

---

### 4. Layout Calculation Algorithm

**Research Question**: What's the correct formula for calculating child position based on Vector2D alignment?

**Findings**:
- **Current Implementation (VerticalLayout)**: Uses formula `x = contentArea.Origin.X + (int)((contentArea.Size.X - w) * alignFrac)` where `alignFrac = (align + 1.0f) * 0.5f`
- **Mathematical Correctness**: This converts -1..1 range to 0..1 fraction for interpolation
  - Alignment -1 (left/top): `((-1) + 1) * 0.5 = 0` → offset = 0 (start of content area)
  - Alignment 0 (center): `(0 + 1) * 0.5 = 0.5` → offset = 50% (centered)
  - Alignment 1 (right/bottom): `(1 + 1) * 0.5 = 1` → offset = 100% (end of content area)

**Decision**: Use formula `position = contentArea.Origin + (contentArea.Size - childSize) * ((alignment + 1) / 2)`
**Rationale**:
- Mathematically correct conversion from -1..1 to 0..1 range
- Already proven in VerticalLayout implementation
- Handles edge cases (child larger than content area) gracefully by using negative offset

**Implementation**:
```csharp
// HorizontalLayout - Y position based on Alignment.Y
var alignFrac = (Alignment.Y + 1.0f) * 0.5f;
var y = contentArea.Origin.Y + (int)((contentArea.Size.Y - childHeight) * alignFrac);

// VerticalLayout - X position based on Alignment.X  
var alignFrac = (Alignment.X + 1.0f) * 0.5f;
var x = contentArea.Origin.X + (int)((contentArea.Size.X - childWidth) * alignFrac);
```

**Alternatives Considered**: None - this is mathematically correct

---

### 5. StretchChildren Interaction with Alignment

**Research Question**: How should StretchChildren interact with the Alignment property?

**Findings**:
- **Current Implementation**: When `_stretchChildren = true`, child size is set to content area size and alignment is effectively ignored
- **Logical Behavior**: Stretching overrides alignment because the child fills the entire content area
- **Spec Requirements**: FR-010 and FR-011 specify that when StretchChildren=true, child size equals content area size regardless of alignment

**Decision**: StretchChildren overrides alignment for the relevant axis
**Rationale**:
- Consistent with current behavior
- Logically correct: can't align a child that fills the entire space
- Spec explicitly requires this behavior

**Implementation**:
```csharp
// HorizontalLayout - when stretching, child height = content height (Y alignment ignored)
var h = _stretchChildren ? contentArea.Size.Y : measured.Y;
var y = _stretchChildren ? contentArea.Origin.Y : /* use alignment calculation */;

// VerticalLayout - when stretching, child width = content width (X alignment ignored)
var w = _stretchChildren ? contentArea.Size.X : measured.X;
var x = _stretchChildren ? contentArea.Origin.X : /* use alignment calculation */;
```

**Alternatives Considered**: None - spec is explicit about this requirement

---

### 6. Edge Case: Alignment Values Outside -1 to 1 Range

**Research Question**: What should happen if alignment values are outside the expected -1 to 1 range?

**Findings**:
- **Mathematical Behavior**: Formula `(align + 1) * 0.5` produces values outside 0-1 range if align is outside -1..1
  - Alignment -2: `((-2) + 1) * 0.5 = -0.5` → child positioned before content area start
  - Alignment 2: `(2 + 1) * 0.5 = 1.5` → child positioned beyond content area end
- **Use Case**: Could be useful for advanced layouts (e.g., parallax effects, overflow positioning)
- **Risk**: Unexpected behavior if values are accidentally set incorrectly

**Decision**: Allow values outside -1 to 1 range without clamping
**Rationale**:
- Provides flexibility for advanced use cases
- Mathematical formula handles it naturally
- Developers can clamp if needed
- Follows principle of least surprise: don't silently modify user input

**Documentation Note**: Document that while -1 to 1 is the standard range, other values are supported and will position children outside the normal content area bounds.

**Alternatives Considered**:
- **Clamp to -1..1**: Rejected because it limits flexibility
- **Throw exception**: Rejected because it's overly restrictive
- **Warning log**: Considered but rejected to avoid performance impact in hot path

---

## Summary of Decisions

1. **Alignment Type**: Vector2D<float> with -1 to 1 range (aligns with existing Align class)
2. **Property Attributes**: Use both [ComponentProperty] and [TemplateProperty] (proven pattern)
3. **Breaking Change**: Accept breaking change with clear migration guidance
4. **Position Formula**: `position = origin + (size - childSize) * ((alignment + 1) / 2)`
5. **StretchChildren**: Overrides alignment for the relevant axis
6. **Range Validation**: No clamping - allow values outside -1 to 1 for flexibility

## Migration Path

### For Developers Using HorizontalLayout

```csharp
// BEFORE
var layout = new HorizontalLayout(descriptorManager);
layout.SetAlignment(Align.Top);

// AFTER (Option 1: Explicit Vector2D)
var layout = new HorizontalLayout(descriptorManager);
layout.SetAlignment(new Vector2D<float>(0, Align.Top));

// AFTER (Option 2: Predefined constant)
var layout = new HorizontalLayout(descriptorManager);
layout.SetAlignment(Align.TopCenter); // Only Y component (Top) matters
```

### For Developers Using VerticalLayout

```csharp
// BEFORE
var layout = new VerticalLayout(descriptorManager);
layout.SetAlignment(Align.Left);

// AFTER (Option 1: Explicit Vector2D)
var layout = new VerticalLayout(descriptorManager);
layout.SetAlignment(new Vector2D<float>(Align.Left, 0));

// AFTER (Option 2: Predefined constant)
var layout = new VerticalLayout(descriptorManager);
layout.SetAlignment(Align.TopLeft); // Only X component (Left) matters
```

## Open Questions

None - all research areas have been resolved with clear decisions.
