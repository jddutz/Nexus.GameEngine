# Feature Specification: Layout Alignment Refactor

**Feature Branch**: `005-layout-alignment-refactor`  
**Created**: November 14, 2025  
**Status**: Draft  
**Input**: User description: "Now that we have Container.cs working properly, I want to update VerticalLayout.cs and HorizontalLayout.cs. These need to provide the proper constraints on subcomponents. I also want to remove the separate HorizontalLayout and VerticalLayout properties and combine them into one Layout property (Vector2D<float>)"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Unified Alignment API (Priority: P1)

Developers use a single `Alignment` property (Vector2D<float>) on both HorizontalLayout and VerticalLayout components, replacing the current separate axis-specific alignment properties. The alignment vector uses the standard -1 to 1 range where -1 represents start (left/top), 0 represents center, and 1 represents end (right/bottom).

**Why this priority**: This is the core API change that simplifies the layout system and provides consistency across both layout types. It must be implemented first as it affects all other functionality.

**Independent Test**: Can be fully tested by creating a layout instance, setting the Alignment property to various Vector2D values, and verifying the property is stored correctly. Delivers immediate value by providing a cleaner, more intuitive API.

**Acceptance Scenarios**:

1. **Given** a HorizontalLayout component, **When** developer sets `Alignment = new Vector2D<float>(0, -1)`, **Then** the alignment property stores the vector (center horizontal, top vertical)
2. **Given** a VerticalLayout component, **When** developer sets `Alignment = new Vector2D<float>(1, 0)`, **Then** the alignment property stores the vector (right horizontal, center vertical)
3. **Given** either layout type, **When** developer queries the Alignment property, **Then** it returns the current Vector2D<float> alignment value
4. **Given** a layout template with Alignment specified, **When** the layout is loaded from template, **Then** the Alignment property is correctly initialized

---

### User Story 2 - HorizontalLayout Child Positioning (Priority: P2)

When a HorizontalLayout arranges its children horizontally, each child receives proper size constraints based on measured size and alignment. The vertical alignment component (Y) of the Alignment vector determines how children are positioned within the layout's content area height.

**Why this priority**: This implements the core positioning logic for HorizontalLayout using the new unified alignment system. Depends on P1 being complete.

**Independent Test**: Can be fully tested by creating a HorizontalLayout with multiple children, setting various Alignment.Y values (-1, 0, 1), and verifying that children are positioned correctly (top, center, bottom). Delivers value by ensuring HorizontalLayout works correctly with the new API.

**Acceptance Scenarios**:

1. **Given** a HorizontalLayout with `Alignment.Y = -1` (top), **When** layout arranges children, **Then** each child is positioned at the top of the content area (Y = contentArea.Origin.Y)
2. **Given** a HorizontalLayout with `Alignment.Y = 0` (center), **When** layout arranges children, **Then** each child is vertically centered (Y = contentArea.Origin.Y + (contentArea.Height - child.Height) / 2)
3. **Given** a HorizontalLayout with `Alignment.Y = 1` (bottom), **When** layout arranges children, **Then** each child is positioned at the bottom (Y = contentArea.Origin.Y + contentArea.Height - child.Height)
4. **Given** a HorizontalLayout with `StretchChildren = true`, **When** layout arranges children, **Then** each child height equals content area height regardless of Alignment.Y value
5. **Given** a HorizontalLayout with multiple children, **When** layout arranges children, **Then** each child's X position is calculated as previous X + previous width + Spacing.X

---

### User Story 3 - VerticalLayout Child Positioning (Priority: P2)

When a VerticalLayout arranges its children vertically, each child receives proper size constraints based on measured size and alignment. The horizontal alignment component (X) of the Alignment vector determines how children are positioned within the layout's content area width.

**Why this priority**: This implements the core positioning logic for VerticalLayout using the new unified alignment system. Depends on P1 being complete and is parallel to P2.

**Independent Test**: Can be fully tested by creating a VerticalLayout with multiple children, setting various Alignment.X values (-1, 0, 1), and verifying that children are positioned correctly (left, center, right). Delivers value by ensuring VerticalLayout works correctly with the new API.

**Acceptance Scenarios**:

1. **Given** a VerticalLayout with `Alignment.X = -1` (left), **When** layout arranges children, **Then** each child is positioned at the left of the content area (X = contentArea.Origin.X)
2. **Given** a VerticalLayout with `Alignment.X = 0` (center), **When** layout arranges children, **Then** each child is horizontally centered using the formula: X = contentArea.Origin.X + (contentArea.Width - child.Width) * ((Alignment.X + 1) / 2)
3. **Given** a VerticalLayout with `Alignment.X = 1` (right), **When** layout arranges children, **Then** each child is positioned at the right (X = contentArea.Origin.X + contentArea.Width - child.Width)
4. **Given** a VerticalLayout with `StretchChildren = true`, **When** layout arranges children, **Then** each child width equals content area width and X position ignores Alignment.X value (uses contentArea.Origin.X)
5. **Given** a VerticalLayout with multiple children, **When** layout arranges children, **Then** each child's Y position is calculated as previous Y + previous height + Spacing.Y

---

### User Story 4 - Migration from Old API (Priority: P3)

Existing code using the old separate `Alignment` properties (float) continues to work through backward compatibility, allowing gradual migration to the new Vector2D<float> API.

**Why this priority**: This provides a smooth migration path for existing code. It's lower priority because new development can use the new API immediately, and migration can happen incrementally.

**Independent Test**: Can be fully tested by using old property setters (if provided) or by verifying that existing templates/code continue to function. Delivers value by preventing breaking changes in existing projects.

**Acceptance Scenarios**:

1. **Given** existing code using HorizontalLayout with old `SetAlignment(Align.Center)`, **When** code runs, **Then** layout behavior remains unchanged
2. **Given** existing code using VerticalLayout with old `SetAlignment(Align.Left)`, **When** code runs, **Then** layout behavior remains unchanged
3. **Given** a template using old single-axis alignment values, **When** template is loaded, **Then** alignment is converted to appropriate Vector2D<float> value

---

### Edge Cases

- What happens when `Alignment` vector contains values outside the -1 to 1 range? (Should clamp or use as-is?)
- How does the system handle layouts with zero content area (all space consumed by padding)?
- What happens when a child's measured size exceeds the available content area size?
- How should StretchChildren interact with Alignment when children have conflicting size requirements?
- What happens when Spacing values cause children to extend beyond the content area?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: HorizontalLayout MUST provide an `Alignment` property of type `Vector2D<float>` where X component is unused and Y component controls vertical alignment of children
- **FR-002**: VerticalLayout MUST provide an `Alignment` property of type `Vector2D<float>` where Y component is unused and X component controls horizontal alignment of children
- **FR-003**: Both layout types MUST use the -1 to 1 range for alignment values where -1 = start (left/top), 0 = center, 1 = end (right/bottom)
- **FR-004**: HorizontalLayout MUST calculate child Y position based on `Alignment.Y` using the formula: `contentArea.Origin.Y + max(0, (contentArea.Height - childHeight) * ((Alignment.Y + 1) / 2))`
- **FR-005**: VerticalLayout MUST calculate child X position based on `Alignment.X` using the formula: `contentArea.Origin.X + max(0, (contentArea.Width - childWidth) * ((Alignment.X + 1) / 2))`
- **FR-006**: HorizontalLayout MUST arrange children horizontally with each child's X position calculated as: `contentArea.Origin.X + sum(previous child widths) + (child index * Spacing.X)`
- **FR-007**: VerticalLayout MUST arrange children vertically with each child's Y position calculated as: `contentArea.Origin.Y + sum(previous child heights) + (child index * Spacing.Y)`
- **FR-008**: Both layout types MUST call `child.Measure(contentArea.Size)` before positioning each child to determine the child's preferred size
- **FR-009**: Both layout types MUST call `child.SetSizeConstraints(new Rectangle<int>(x, y, width, height))` to apply the calculated position and size to each child
- **FR-010**: When `StretchChildren = true`, HorizontalLayout MUST set child height to `contentArea.Height` regardless of measured height or Alignment.Y
- **FR-011**: When `StretchChildren = true`, VerticalLayout MUST set child width to `contentArea.Width` regardless of measured width or Alignment.X
- **FR-012**: Both layout types MUST mark the `Alignment` property with `[ComponentProperty]` and `[TemplateProperty]` attributes for source generation
- **FR-013**: The default value for `Alignment` MUST be `new Vector2D<float>(0, 0)` (center) for both layout types
- **FR-014**: Both layout types MUST update child constraints during `UpdateLayout()` which is called when layout is invalidated or during the update cycle
- **FR-015**: System MUST remove the old single-axis `_alignment` field (float) from both HorizontalLayout and VerticalLayout classes

### Key Entities

- **Alignment (Vector2D<float>)**: Two-dimensional alignment vector controlling child positioning within layout content area. X component: -1 (left) to 1 (right), Y component: -1 (top) to 1 (bottom). For HorizontalLayout, only Y is used; for VerticalLayout, only X is used.
- **ContentArea (Rectangle<int>)**: Available space for child elements, calculated by Container base class as layout bounds minus padding
- **ChildConstraints (Rectangle<int>)**: Position and size bounds provided to each child via SetSizeConstraints(), defining where and how large the child should render
- **MeasuredSize (Vector2D<int>)**: Preferred size returned by child's Measure() method, used to calculate final child dimensions before positioning

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can configure child alignment for both layout types using a single `Alignment` property instead of separate axis-specific properties (reduces API surface by 50%)
- **SC-002**: All existing unit tests for HorizontalLayout and VerticalLayout continue to pass after refactoring (100% test compatibility)
- **SC-003**: Layout children are positioned correctly with 100% accuracy for all alignment values in the -1 to 1 range (verified by unit tests covering at least 9 combinations: -1/0/1 for each axis)
- **SC-004**: Child components receive correct size constraints on every layout update (verified by integration tests that sample rendered pixels at expected positions)
- **SC-005**: Layout performance remains unchanged or improves compared to the current implementation (measured by TestApp integration test execution time)
