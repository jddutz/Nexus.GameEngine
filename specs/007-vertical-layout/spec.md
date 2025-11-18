# Feature Specification: Directional Layout Components (VerticalLayout & HorizontalLayout)

**Feature Branch**: `007-vertical-layout`  
**Created**: November 16, 2025  
**Updated**: November 17, 2025  
**Status**: Draft  
**Input**: User description: "position and size within a Container is now working. But Container is relatively simple. Now we want to get VerticalLayout working. I should be able to position a VerticalLayout within a container easily enough. VerticalLayout needs to calculate the content area of its children with options to control the behavior using VerticalLayoutMode, which should have options like StackedTop, StackedMiddle, StackedBottom, SpacedEqually, and Justified."

**Evolution**: Initial specification focused on VerticalLayoutMode enum with five modes. Design evolved through decomposition into atomic layout properties, eliminating enum-based design in favor of composable behavior. Expanded to include both VerticalLayout and HorizontalLayout using identical property patterns (vertical vs horizontal axes).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic Directional Stacking (Priority: P1)

UI developers need to arrange UI elements (buttons, labels, images) vertically or horizontally without manually calculating positions for each child element, with control over spacing and alignment.

**Why this priority**: This is the foundational use case for directional layouts. Delivers immediate value by eliminating manual position calculations for common UI patterns like vertical menus, horizontal toolbars, and navigation bars.

**Independent Test**: Can be fully tested by creating a VerticalLayout or HorizontalLayout with 3+ child elements and verifying they stack with configurable spacing (ItemSpacing) and alignment, delivering working menus, toolbars, or navigation bars.

**Acceptance Scenarios**:

1. **Given** a VerticalLayout with ItemSpacing set to 10 pixels and Alignment.Y = -1 (top), **When** the layout is rendered, **Then** children are positioned top-to-bottom with exactly 10 pixels between each child and remaining space at the bottom
2. **Given** a HorizontalLayout with ItemSpacing set to 15 pixels and Alignment.X = 0 (center), **When** the layout is rendered, **Then** children are positioned left-to-right with 15 pixels between each child and remaining space split equally before and after
3. **Given** a VerticalLayout within a Container, **When** the Container's size changes, **Then** the VerticalLayout repositions its children maintaining spacing and alignment

---

### User Story 2 - Calculated Spacing Distribution (Priority: P2)

UI developers need to distribute children evenly with automatic spacing calculation (space-between or space-evenly patterns) instead of fixed gaps.

**Why this priority**: Enables professional-looking layouts with balanced spacing that automatically adapts to container size changes. Common requirement for navigation menus, toolbars, and evenly distributed content sections.

**Independent Test**: Can be tested independently by creating a VerticalLayout with ItemSpacing=null and Spacing=Justified (space-between) or Distributed (space-evenly), verifying automatic spacing calculation delivers balanced layouts.

**Acceptance Scenarios**:

1. **Given** a VerticalLayout with ItemSpacing=null and Spacing=SpacingMode.Justified and three children (total height: 150px), **When** the container has 300px content height, **Then** there is 75px spacing calculated between children (first at top, last at bottom)
2. **Given** a HorizontalLayout with ItemSpacing=null and Spacing=SpacingMode.Distributed and four children (total width: 200px), **When** the container has 400px content width, **Then** there is 40px spacing before, between, and after children (evenly distributed)

---

### User Story 3 - Fixed Item Sizing (Priority: P2)

UI developers need to override child sizes uniformly (e.g., all list items same height, all toolbar buttons same width) without setting size on each child.

**Why this priority**: Simplifies template creation for uniform layouts like lists, toolbars, and button groups. Eliminates repetitive size specifications across children.

**Independent Test**: Can be tested independently by creating a VerticalLayout with ItemHeight=60 and verifying all children are forced to 60px height regardless of their measured size, delivering uniform list items.

**Acceptance Scenarios**:

1. **Given** a VerticalLayout with ItemHeight=60 and varying child elements, **When** the layout is rendered, **Then** all children are rendered at exactly 60px height
2. **Given** a HorizontalLayout with ItemWidth=100 and three buttons of different content widths, **When** the layout is rendered, **Then** all buttons are rendered at exactly 100px width

---

### Edge Cases

- What happens when the combined size of children exceeds the container's content area?
  - Layout still positions children according to properties, with overflow extending beyond visible area (clipping/scrolling handled by parent container or viewport)
- How does the system handle a layout with no children?
  - Layout renders successfully with empty content area, no errors or crashes
- What happens when a child element changes size dynamically?
  - Layout invalidates and recalculates positions on the next update cycle, maintaining current property configuration
- What happens when only 1 child exists with Spacing=SpacingMode.Justified?
  - Child positioned at top/left with no spacing (single child edge case)
- What happens when only 1 child exists with Spacing=SpacingMode.Distributed?
  - Child centered in content area (equal space before and after)
- What if child.Measure() returns 0 for width/height?
  - Child excluded from layout entirely (zero-size children collapsed)

## Requirements *(mandatory)*

### Functional Requirements

**Core Layout Components**:
- **FR-001**: System MUST provide VerticalLayout and HorizontalLayout components that inherit from Container and arrange child elements along their respective axes
- **FR-002**: Both layouts MUST use identical property patterns (ItemHeight/ItemWidth, Spacing, ItemSpacing) with axis-specific behavior
- **FR-003**: Both layouts MUST respect the Container's padding property when calculating the content area for child positioning

**Property Behavior**:
- **FR-004**: ItemHeight/ItemWidth (uint?) MUST override child.Measure() when set, forcing all children to the specified size along the layout axis
- **FR-005**: ItemSpacing (uint?) MUST apply fixed spacing between children when set; when null, spacing is calculated based on SpacingMode
- **FR-006**: Spacing (SpacingMode enum) MUST determine spacing distribution when ItemSpacing is null: Justified (space-between) or Distributed (space-evenly)
- **FR-007**: Alignment (inherited Vector2D<float>) MUST determine remaining space distribution when ItemSpacing is set, using axis-aligned component (Y for vertical, X for horizontal)

**Layout Algorithm**:
- **FR-008**: Layouts MUST exclude children with zero measured size along the layout axis from positioning calculations
- **FR-009**: Layouts MUST recalculate child positions when invalidated (container size changes, children added/removed, property changes)
- **FR-010**: When ItemSpacing is set, remaining space MUST be distributed using formula: `spaceBefore = remainingSpace × (Alignment.AxisValue + 1) / 2`
- **FR-011**: When ItemSpacing is null and Spacing=Justified, spacing MUST be calculated as: `availableSpace / (childCount - 1)` with first child at start edge and last at end edge
- **FR-012**: When ItemSpacing is null and Spacing=Distributed, spacing MUST be calculated as: `availableSpace / (childCount + 1)` with equal space before, between, and after children

**Integration**:
- **FR-013**: Layouts MUST work correctly when nested within other Containers or directional layouts
- **FR-014**: Layouts MUST support dynamic child addition and removal with automatic layout recalculation
- **FR-015**: Layouts MUST integrate with the existing ILayout interface and invalidation system
- **FR-016**: Each child element MUST be positioned using SetSizeConstraints() to define its available space
- **FR-017**: Layouts MUST preserve cross-axis positioning behavior (children use their own alignment/size settings for the non-layout axis)

### Key Entities

- **VerticalLayout**: Layout container arranging children vertically (top-to-bottom). Properties: ItemHeight (uint?), Spacing (SpacingMode), ItemSpacing (uint?), Alignment.Y (float, inherited).
- **HorizontalLayout**: Layout container arranging children horizontally (left-to-right). Properties: ItemWidth (uint?), Spacing (SpacingMode), ItemSpacing (uint?), Alignment.X (float, inherited).
- **SpacingMode**: Enumeration defining spacing distribution: Justified (space-between pattern) or Distributed (space-evenly pattern).
- **Child Elements**: IUserInterfaceElement instances positioned by the layout. Each child has its own size, size mode, and alignment properties.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: UI developers can create vertical and horizontal layouts (menus, toolbars, navigation bars) without manually calculating positions for any child element
- **SC-002**: Both layouts correctly implement all three property behaviors (ItemHeight/ItemWidth, Spacing modes, ItemSpacing) with axis-specific positioning
- **SC-003**: Layouts respond to container resize events and reposition children within 1 frame update cycle
- **SC-004**: Child elements added or removed from layouts are automatically positioned correctly on the next layout update
- **SC-005**: Layouts correctly calculate child positions for 1 to 100+ child elements without performance degradation
- **SC-006**: Nested directional layouts (e.g., VerticalLayout within HorizontalLayout) position and size correctly
- **SC-007**: Unit tests achieve 80% code coverage for both VerticalLayout and HorizontalLayout components
- **SC-008**: Integration tests verify all property combinations (ItemSpacing with alignment, calculated spacing modes) with multiple child configurations

## Assumptions

- Both layouts inherit the existing invalidation and update lifecycle from Container
- The ILayout interface and OnChildSizeChanged callback are already implemented and functional
- Children expose their size via the Size property and can be positioned via SetSizeConstraints()
- The deferred property update system handles changes to layout properties without manual intervention
- Cross-axis positioning of children (horizontal for VerticalLayout, vertical for HorizontalLayout) is independent and controlled by each child's own alignment/size properties
- Overflow handling (clipping, scrolling) is the responsibility of parent containers or viewport, not the directional layouts themselves
- SpacingMode.Justified is the default when ItemSpacing is null (space-between is most common)
- Zero-size children (child.Measure() returns 0 along layout axis) are excluded from layout entirely
