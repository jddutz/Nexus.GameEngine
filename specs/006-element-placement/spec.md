# Feature Specification: Element Placement System

**Feature Branch**: `006-element-placement`  
**Created**: November 16, 2025  
**Status**: Draft  
**Input**: User description: "We need to design and implement a better system for Element placement within containers."

## Core Rendering Concepts

### Current Implementation

**Position (Transformable property)**:
- Part of the SRT (Scale-Rotation-Translation) transformation in WorldMatrix
- Applied during rendering via the world transformation matrix
- Controls WHERE the element's coordinate system is located in screen space
- Included in the Model matrix passed to the shader via push constants

**AnchorPoint (Element property)**:
- Normalized coordinates in element space (-1 to 1)
- Passed to the shader via push constants alongside Size
- Applied IN THE SHADER to transform quad vertices: `vec2 xy = (inPos - anchor) * size * 0.5`
- Determines which point of the element rectangle aligns with the Position
- Example: AnchorPoint=(-1,-1) means top-left of element is at Position; AnchorPoint=(0,0) means center is at Position

**Size (Element property)**:
- Pixel dimensions (width, height) of the element
- Passed to shader via push constants
- Applied IN THE SHADER to scale the normalized quad geometry to pixel dimensions
- NOT included in the WorldMatrix transformation

**Shader Pipeline**:
```glsl
// Shader computes local position from normalized quad vertices
vec2 xy = (inPos - anchor) * size * 0.5;
vec4 p = vec4(xy, 0, 1);
// Then applies world transform (Position, Rotation, Scale from Transformable)
gl_Position = camera.viewProjection * world * p;
```

### Constraint

**CRITICAL**: We CANNOT change how Position, AnchorPoint, or Size interact with the shader without breaking rendering. These properties are tightly coupled to the shader implementation. Any positioning system design must work within this rendering model.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Simplified Container Layout API (Priority: P1)

Developers configure how container layouts position children using a simple alignment property. Currently, HorizontalLayout and VerticalLayout give each child the full cross-axis space and let children position themselves using their AnchorPoint. The improved system should allow containers to control child positioning more directly while respecting the rendering constraints (Position, AnchorPoint, Size must work with shader).

**Why this priority**: Current layout system has unclear responsibilities - containers provide space, but children self-position using AnchorPoint. This makes it hard to create predictable layouts. Clarifying how containers control positioning (within shader constraints) is the foundation.

**Independent Test**: Can be fully tested by creating a layout container with alignment settings, adding children with specific sizes, and verifying children appear in expected positions. Delivers value by making layout behavior more predictable and easier to reason about.

**Acceptance Scenarios**:

1. **Given** a HorizontalLayout with alignment configuration, **When** adding children of varying heights, **Then** children are positioned according to container's alignment rules
2. **Given** a VerticalLayout with alignment configuration, **When** adding children of varying widths, **Then** children are positioned according to container's alignment rules
3. **Given** a container layout, **When** alignment setting changes, **Then** all children reposition to match new alignment
4. **Given** a child element with specific AnchorPoint, **When** placed in a container, **Then** [NEEDS CLARIFICATION: Should container override child AnchorPoint, or use it as a positioning hint?]

---

### User Story 2 - Consistent Positioning Model (Priority: P2)

The system provides clear documentation and examples showing how Position, AnchorPoint, and Size interact during layout. Developers understand when to use AnchorPoint on elements versus when containers control positioning.

**Why this priority**: Current confusion stems from unclear mental model of how properties interact. Even if we don't change implementation significantly, clear documentation and consistent patterns would help developers.

**Independent Test**: Can be fully tested by providing example code showing different positioning scenarios and verifying rendered output matches examples. Delivers value through improved developer understanding.

**Acceptance Scenarios**:

1. **Given** documentation explaining Position/AnchorPoint/Size relationship, **When** developer reads it, **Then** they can predict where elements will render
2. **Given** examples of container-based layouts, **When** developer follows patterns, **Then** layouts behave as expected
3. **Given** elements with different AnchorPoint values, **When** placed at same constraints, **Then** developer understands why they render at different screen positions

---

### Edge Cases

- What happens when container sets constraints but child has AnchorPoint that moves it outside constraints? Current behavior: child can render outside. Desired: [NEEDS CLARIFICATION]
- How do containers calculate Position for children given that AnchorPoint affects rendering location? Current: `posX = constraints.Center.X + AnchorPoint.X * constraints.HalfSize.X`. Is this correct? [NEEDS CLARIFICATION]
- Should containers be allowed to override child AnchorPoint values to enforce layout rules? [NEEDS CLARIFICATION]
- When element moves between container and direct window placement, should its AnchorPoint behavior change? [NEEDS CLARIFICATION]
- How should nested containers propagate positioning? Each level uses SetSizeConstraints, but how does AnchorPoint propagate through hierarchy?

## Requirements *(mandatory)*

### Functional Requirements

**Note**: These requirements are DRAFT and depend on answers to clarification questions below. They may be significantly revised after clarification.

- **FR-001**: Container layouts (HorizontalLayout, VerticalLayout) MUST call SetSizeConstraints(Rectangle<int>) on each child to define available space
- **FR-002**: Element.OnSizeConstraintsChanged() MUST respect the shader rendering model where Position, AnchorPoint, and Size are used together to determine final render location
- **FR-003**: [NEEDS CLARIFICATION: How should containers calculate the Position to pass via SetSizeConstraints given that AnchorPoint will affect final render location?]
- **FR-004**: [NEEDS CLARIFICATION: Should containers set a specific AnchorPoint value on children, or should they work with whatever AnchorPoint the child has?]
- **FR-005**: Documentation MUST clearly explain the relationship between Position (world transform), AnchorPoint (shader offset), and Size (shader scaling)
- **FR-006**: Documentation MUST provide examples showing common layout patterns and how to achieve them given the rendering constraints
- **FR-007**: HorizontalLayout MUST arrange children horizontally with spacing between them
- **FR-008**: VerticalLayout MUST arrange children vertically with spacing between them
- **FR-009**: Both layout types MUST support alignment configuration for the cross-axis (vertical for HorizontalLayout, horizontal for VerticalLayout)
- **FR-010**: [NEEDS CLARIFICATION: What is the specific formula/algorithm for calculating child Position given constraints rectangle, child size, and alignment value?]

### Key Entities

- **Position (Vector3D<float>)**: Element's location in screen space; part of Transformable SRT transform; included in WorldMatrix sent to shader; determines where element's coordinate system origin is located
- **AnchorPoint (Vector2D<float>)**: Normalized coordinates (-1 to 1) defining which point of the element rectangle aligns with Position; passed to shader; applied as `(inPos - anchor) * size * 0.5` in vertex shader
- **Size (Vector2D<int>)**: Pixel dimensions of element; passed to shader; scales normalized quad vertices to pixel dimensions in shader
- **WorldMatrix (Matrix4X4<float>)**: Complete transformation including Position, Rotation, Scale from Transformable; sent to shader as Model matrix in push constants; does NOT include Size or AnchorPoint
- **Container**: Layout component that calls SetSizeConstraints() on children; must calculate appropriate constraints.Origin and constraints.Size for each child
- **Alignment**: Container property controlling how children are positioned within available space; [NEEDS CLARIFICATION: How does this interact with child AnchorPoint?]
- **SetSizeConstraints(Rectangle<int>)**: Method called by parent on child; provides Rectangle with Origin (x,y) and Size (width, height); child's OnSizeConstraintsChanged() must interpret this

## Success Criteria *(mandatory)*

### Measurable Outcomes

**Note**: Success criteria depend on clarifications and may be revised.

- **SC-001**: Developers can create common layout patterns (centered buttons, aligned text, distributed items) with clear, predictable code
- **SC-002**: Layout positioning calculations complete within one frame (16ms) for containers with up to 100 children
- **SC-003**: Zero regressions in existing rendering behavior (all current tests pass)
- **SC-004**: Documentation includes at least 5 clear examples showing different layout patterns with Position/AnchorPoint/Size explanations
- **SC-005**: [PENDING CLARIFICATION: Specific positioning accuracy criteria depend on chosen approach]
