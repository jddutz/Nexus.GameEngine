# Research: Directional Layout Components (VerticalLayout & HorizontalLayout)

**Feature**: VerticalLayout and HorizontalLayout with property-based design  
**Date**: November 16-17, 2025  
**Status**: Updated for property-based design

## Overview

This document consolidates research findings for implementing VerticalLayout and HorizontalLayout components using a property-based design with four core properties (ItemHeight/ItemWidth, Spacing, ItemSpacing) plus inherited Alignment for flexible spacing and sizing control. The design evolved from an initial enum-based approach to composable properties that provide greater flexibility and industry alignment with CSS Flexbox patterns.

## Research Areas

### 1. Existing VerticalLayout Implementation

**Current State**: A basic VerticalLayout already exists in `src/GameEngine/GUI/Layout/VerticalLayout.cs` with the following characteristics:

- Extends Container base class
- Has an `ItemHeight` property (fixed height mode)
- Implements `UpdateLayout()` override that arranges children vertically from top to bottom
- Positions children using `SetSizeConstraints()` 
- Applies spacing between children using `Spacing.Y` from Container

**Decision**: Enhance the existing VerticalLayout rather than creating a new component.

**Rationale**: The existing implementation provides the foundation for vertical stacking. Adding a VerticalLayoutMode enum and expanding UpdateLayout() logic is more maintainable than duplicating code.

**Alternatives Considered**:
- Creating separate components (StackedLayout, JustifiedLayout, etc.) - Rejected because it duplicates common vertical layout logic and complicates the component hierarchy
- Creating a generic Layout component with both vertical and horizontal modes - Rejected because it's more complex than needed for this feature scope

### 2. SpacingMode Enumeration Design

**Decision**: Create a `SpacingMode` enum with two values for spacing distribution when ItemSpacing is null:

```csharp
public enum SpacingMode
{
    Justified,    // Space between items only (first at start, last at end)
    Distributed   // Space before, between, and after items (equal everywhere)
}
```

**Rationale**: 
- Minimal enum with clear, self-documenting names
- Enum type integrates seamlessly with source-generated [ComponentProperty] system
- No interpolation needed (discrete mode changes)
- Covers the two most common spacing distribution patterns (space-between vs space-evenly)

**Alternatives Considered**:
- Using string constants - Rejected for type safety and compile-time validation
- Including more spacing modes - Rejected as these two cover >90% of use cases
- No enum, only ItemSpacing - Rejected as automatic spacing calculation is valuable

### 3. Content Area Calculation Pattern

**Decision**: Use Container's existing pattern for calculating the content area:

```csharp
var contentArea = new Rectangle<int>(
    (int)(TargetPosition.X - (1.0f + AnchorPoint.X) * TargetSize.X * 0.5f) + Padding.Left,
    (int)(TargetPosition.Y - (1.0f + AnchorPoint.Y) * TargetSize.Y * 0.5f) + Padding.Top,
    Math.Max(0, TargetSize.X - Padding.Left - Padding.Right),
    Math.Max(0, TargetSize.Y - Padding.Top - Padding.Bottom)
);
```

This calculation:
- Respects the Container's TargetPosition and TargetSize (deferred updates)
- Respects AnchorPoint for positioning
- Applies Padding to create the content area
- Uses Math.Max(0, ...) to prevent negative dimensions

**Rationale**: Existing Container.UpdateLayout() demonstrates this pattern works correctly with the deferred property system and handles edge cases.

**Alternatives Considered**:
- Direct access to Position/Size properties - Rejected because TargetPosition/TargetSize handle deferred updates correctly
- Separate calculation for each layout mode - Rejected as content area is mode-independent

### 4. Child Measurement and Positioning

**Decision**: Use the existing IUserInterfaceElement.Measure() and SetSizeConstraints() pattern:

1. Call `child.Measure(availableSize)` to get child's desired size
2. Calculate child's constraints rectangle based on layout mode
3. Call `child.SetSizeConstraints(constraints)` to position child
4. Child positions itself within constraints using its AnchorPoint/Alignment

**Rationale**: 
- Measure() already exists and is implemented by Element base class
- SetSizeConstraints() is the established communication mechanism between layouts and children
- Children maintain control over their horizontal positioning (Alignment property)
- Pattern is consistent with Container base class

**Key Insight**: The VerticalLayout is responsible for:
- Vertical positioning (Y coordinate of constraints rectangle)
- Height allocation (height of constraints rectangle)
- Width is always the content area width (children handle their own horizontal layout)

**Alternatives Considered**:
- Direct manipulation of child Position/Size - Rejected because it bypasses the child's own layout logic and breaks SizeMode support
- Custom callback interface - Rejected as SetSizeConstraints() already provides needed functionality

### 5. Property-Based Layout Algorithms

**Decision**: Implement layout logic based on four core properties using conditional branching in UpdateLayout().

**Core Properties**:
- `ItemSpacing`: Fixed spacing between children (nullable uint)
- `Spacing`: SpacingMode enum for automatic spacing when ItemSpacing is null
- `ItemHeight`: Fixed height override for children (nullable uint)  
- `Alignment.Y`: Distribution of remaining space when ItemSpacing is set (-1=top, 0=center, 1=bottom)

**Algorithm Flow**:
```csharp
void UpdateLayout()
{
    var children = GetChildren<IUserInterfaceElement>();
    if (children.Count == 0) return;
    
    var contentArea = CalculateContentArea();
    
    // Determine spacing strategy
    if (ItemSpacing.HasValue)
    {
        // Fixed spacing: use ItemSpacing, distribute remaining space with Alignment.Y
        LayoutWithFixedSpacing(children, contentArea, ItemSpacing.Value);
    }
    else
    {
        // Automatic spacing: use Spacing mode
        LayoutWithAutomaticSpacing(children, contentArea, Spacing);
    }
}
```

#### Fixed Spacing Algorithm (ItemSpacing set)
```
Algorithm:
1. Calculate total child height:
   totalHeight = sum(DetermineChildHeight(child) for each child)
2. Calculate total spacing:
   totalSpacing = ItemSpacing * (childCount - 1)
3. Calculate remaining space:
   remainingSpace = contentArea.Height - totalHeight - totalSpacing
4. Calculate starting Y based on Alignment.Y:
   - -1: startY = contentArea.Origin.Y (top-aligned)
   - 0: startY = contentArea.Origin.Y + remainingSpace / 2 (centered)
   - 1: startY = contentArea.Origin.Y + remainingSpace (bottom-aligned)
5. Position children:
   Y = startY
   For each child:
     h = DetermineChildHeight(child)
     constraints = Rectangle(contentArea.X, Y, contentArea.Width, h)
     child.SetSizeConstraints(constraints)
     Y += h + ItemSpacing
```

#### Automatic Spacing - Justified (SpacingMode.Justified)
```
Algorithm:
1. Calculate total child height:
   childHeight = sum(DetermineChildHeight(child) for each child)
2. Calculate available space:
   availableSpace = contentArea.Height - childHeight
3. Calculate spacing (space-between):
   spacing = availableSpace / (childCount - 1)  // Only between items
4. Position children:
   Y = contentArea.Origin.Y
   For each child:
     h = DetermineChildHeight(child)
     constraints = Rectangle(contentArea.X, Y, contentArea.Width, h)
     child.SetSizeConstraints(constraints)
     Y += h + spacing
```

#### Automatic Spacing - Distributed (SpacingMode.Distributed)
```
Algorithm:
1. Calculate total child height:
   childHeight = sum(DetermineChildHeight(child) for each child)
2. Calculate available space:
   availableSpace = contentArea.Height - childHeight
3. Calculate spacing (space-evenly):
   spacing = availableSpace / (childCount + 1)  // Before, between, and after
4. Position children:
   Y = contentArea.Origin.Y + spacing
   For each child:
     h = DetermineChildHeight(child)
     constraints = Rectangle(contentArea.X, Y, contentArea.Width, h)
     child.SetSizeConstraints(constraints)
     Y += h + spacing
```

**Child Height Determination**:
```csharp
uint DetermineChildHeight(IUserInterfaceElement child)
{
    // ItemHeight override
    if (ItemHeight.HasValue)
        return ItemHeight.Value;
    
    // Measure child
    var measured = child.Measure(contentArea.Size);
    if (measured.Y > 0)
        return (uint)measured.Y;
    
    // Fallback for zero-size children
    return 50; // DefaultChildHeight
}
```

**Rationale**: 
- Property-based design allows flexible combinations
- Clear precedence: ItemHeight > Measure() > fallback
- Alignment.Y provides remaining space distribution for fixed spacing
- SpacingMode covers automatic spacing patterns
- No switch statements on enums; conditional logic based on property values

**Alternatives Considered**:
- Strategy pattern - Rejected as properties create too many combinations for separate classes
- Enum-based modes - Rejected as it limits flexibility and violates Open/Closed Principle

### 6. Invalidation and Update Lifecycle

**Decision**: Leverage Container's existing invalidation system with source-generated invalidation for new properties:

- Container subscribes to ChildCollectionChanged events
- Container.Invalidate() sets _isLayoutInvalid flag
- Container.OnUpdate() checks flag and calls UpdateLayout()
- Property changes trigger invalidation via source-generated partial methods

**Key Implementation Details**:
```csharp
[ComponentProperty] [TemplateProperty]
private uint? _itemSpacing;
partial void OnItemSpacingChanged(uint? oldValue) => Invalidate();

[ComponentProperty] [TemplateProperty] 
private SpacingMode _spacing = SpacingMode.Justified;
partial void OnSpacingChanged(SpacingMode oldValue) => Invalidate();

[ComponentProperty] [TemplateProperty]
private uint? _itemHeight;
partial void OnItemHeightChanged(uint? oldValue) => Invalidate();
```

**Rationale**: Source generators provide automatic invalidation for property changes, ensuring layout updates on next frame.

**Alternatives Considered**:
- Manual invalidation in property setters - Rejected because source generators reduce boilerplate
- Immediate layout updates - Rejected because it conflicts with deferred property system

### 7. Integration with Source Generators

**Decision**: Use existing source generator attributes for all layout properties:

```csharp
[ComponentProperty] [TemplateProperty] private uint? _itemSpacing;
[ComponentProperty] [TemplateProperty] private SpacingMode _spacing = SpacingMode.Justified;
[ComponentProperty] [TemplateProperty] private uint? _itemHeight;
```

**Rationale**:
- `[TemplateProperty]` enables declarative configuration in templates
- `[ComponentProperty]` provides deferred updates and change notifications
- Partial method hooks allow automatic invalidation on property changes
- Default values provide sensible defaults (SpacingMode.Justified for space-between behavior)

**Alternatives Considered**:
- Animation support for SpacingMode - Rejected because enum interpolation is not meaningful
- Manual property implementation - Rejected because source generators reduce boilerplate

### 8. Testing Strategy

**Decision**: Implement comprehensive testing for property combinations:

**Unit Tests** (`Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs`):
- Test property combinations: ItemSpacing with different Alignment.Y values
- Test SpacingMode values (Justified vs Distributed)
- Test ItemHeight override behavior
- Test edge cases: empty children, single child, zero-size children
- Mock IDescriptorManager and child elements using Moq
- Target: 80% code coverage

**Integration Tests** (`TestApp/Tests/VerticalLayoutTests.cs`):
- Visual validation using frame-based testing pattern
- Test user story scenarios with property combinations
- Verify layout responds to container resizing
- Test nested layouts
- Validate with different child SizeModes

**Rationale**: Property-based design requires testing combinations rather than discrete modes.

**Alternatives Considered**:
- Testing all possible property combinations exhaustively - Rejected as combinatorial explosion
- Integration tests only - Rejected because unit tests provide faster feedback

### 9. Performance Considerations

**Decision**: No special performance optimizations needed beyond existing patterns:

- Layout calculations are O(n) in number of children
- Measure() called once per child per layout update
- SetSizeConstraints() called once per child per layout update
- No allocations in hot path (uses existing Rectangle struct)

**Rationale**: For typical UI layouts (1-100 children), the algorithmic complexity is acceptable. The specification requires 1 frame update cycle for 100+ children, which is easily achievable with O(n) complexity.

**Performance Validation**:
- Integration tests will include 100+ child scenarios
- Frame timing can be measured via TestApp infrastructure
- If performance issues arise, caching strategies can be added later

**Alternatives Considered**:
- Caching child measurements - Rejected as premature optimization; invalidation makes this complex
- Incremental layout updates - Rejected as unnecessary for typical UI update frequencies

### 10. Backward Compatibility

**Decision**: Maintain backward compatibility while adding new properties:

- Keep ItemHeight property and behavior (fixed height override)
- Add new properties with sensible defaults
- Existing VerticalLayout usage continues working without changes

**Migration Path**: 
- Existing code using ItemHeight works unchanged
- New features available via ItemSpacing, Spacing, and Alignment properties
- ItemHeight remains functional but may be deprecated in future versions

**Rationale**: Gradual migration allows existing code to continue working while new features are adopted.

**Alternatives Considered**:
- Breaking changes to remove ItemHeight - Rejected for backward compatibility
- Making ItemHeight work differently - Rejected as it would break existing code

## Summary of Decisions

1. **Enhance existing VerticalLayout** rather than creating new components
2. **SpacingMode enum** with 2 values for automatic spacing distribution
3. **Four core properties**: ItemSpacing, Spacing, ItemHeight, plus inherited Alignment.Y
4. **Property-based algorithms** using conditional logic instead of enum switches
5. **Reuse Container's content area calculation** pattern
6. **Use Measure() and SetSizeConstraints()** for child positioning
7. **Leverage Container's invalidation system** with source-generated invalidation
8. **Source-generated properties** for all layout properties
9. **Two-level testing**: Unit tests for property combinations, integration tests for scenarios
10. **No premature optimization**: O(n) layout is sufficient for typical UI
11. **Maintain backward compatibility** with ItemHeight property

## Critical Finding: Property-Based Design Evolution (Nov 17, 2025)

### Original Enum-Based Design

The initial specification proposed five layout modes via `VerticalLayoutMode` enum:
- StackedTop, StackedMiddle, StackedBottom, SpacedEqually, Justified

**Problem**: Enum-based design is rigid and doesn't allow flexible combinations.

### Iterative Decomposition

Through analysis, layout behavior was decomposed into atomic properties:
1. **Spacing Strategy**: Fixed spacing vs automatic distribution
2. **Distribution Mode**: Space-between vs space-evenly (when automatic)
3. **Remaining Space**: How to distribute extra space (when fixed spacing)
4. **Height Override**: Fixed height for all children

### Final Property-Based Design

**VerticalLayout Properties**:
```csharp
[ComponentProperty] public uint? ItemSpacing { get; set; }    // Fixed spacing between children
[ComponentProperty] public SpacingMode Spacing { get; set; }  // Distribution when ItemSpacing is null
[ComponentProperty] public uint? ItemHeight { get; set; }     // Fixed height override
// Inherited: Alignment.Y (float -1 to 1) - Remaining space distribution
```

**SpacingMode Enum**:
```csharp
public enum SpacingMode
{
    Justified,    // Space between (first/last at edges)
    Distributed   // Space evenly (margins around all items)
}
```

### Benefits of Property-Based Design

1. **Composability**: Mix behaviors (e.g., fixed height + centered + automatic spacing)
2. **Flexibility**: Properties create dozens of combinations vs 5 rigid modes
3. **Industry Alignment**: Matches CSS Flexbox justify-content/align-items
4. **Maintainability**: Clear precedence rules, no complex switch statements
5. **Extensibility**: Easy to add new properties without breaking existing code

### Mapping Original Modes to Properties

| Original Mode | ItemSpacing | Spacing | Alignment.Y | ItemHeight |
|---------------|-------------|---------|-------------|------------|
| StackedTop | 10 | (ignored) | -1 | null |
| StackedMiddle | 10 | (ignored) | 0 | null |
| StackedBottom | 10 | (ignored) | 1 | null |
| SpacedEqually | null | Distributed | (ignored) | null |
| Justified | null | Justified | (ignored) | null |

**Plus many new combinations** not possible with original enum design!

## Open Questions

None. All technical unknowns have been resolved through code inspection and architectural review.

## References

- Existing Implementation: `src/GameEngine/GUI/Layout/VerticalLayout.cs`
- Base Class: `src/GameEngine/GUI/Layout/Container.cs`
- Interface: `src/GameEngine/GUI/Layout/ILayout.cs`
- Element Base: `src/GameEngine/GUI/Element.cs`
- Size Modes: `src/GameEngine/GUI/SizeMode.cs`
- Constitution: `.specify/memory/constitution.md`
- Feature Spec: `specs/007-vertical-layout/spec.md`
