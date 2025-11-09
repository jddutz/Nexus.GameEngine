# Feature Specification: UI Layout System

**Feature Branch**: `003-ui-layout-system`  
**Created**: November 4, 2025  
**Status**: Draft  
**Input**: User description: "Design and implement a system for laying out UI components (Elements). Update existing Layout components to work with current architecture, add new ones based on standard practice from other game engines. Enable accurate placement regardless of screen size with dynamic and responsive behavior. Consider high vs low resolution screens and various aspect ratios."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Responsive Container Layout (Priority: P1)

Game developers need UI elements to automatically resize and reposition when the window is resized, screen orientation changes (mobile/tablet rotation), or when running on different screen resolutions (1920x1080 vs 3840x2160 or 1280x720), maintaining relative proportions and alignment without hardcoding pixel coordinates for each resolution.

**Why this priority**: Core foundation for resolution-independent UI. Without this, all UI must be manually adjusted for each target resolution or window state, making cross-platform development and responsive design impractical.

**Independent Test**: Create a colored Element (red rectangle) anchored to center (0, 0) of viewport. Use pixel sampling to verify element is centered. Programmatically change viewport dimensions. Re-sample to verify element remains centered at new dimensions with correct size.

**Acceptance Scenarios**:

1. **Given** a UI element with relative positioning (e.g., "50% from left, 50% from top"), **When** the window is resized from 1920x1080 to 1280x720, **Then** the element remains at the relative position (center of the new viewport)
2. **Given** a full-screen background element, **When** the application runs on a 4K monitor (3840x2160) vs HD monitor (1920x1080), **Then** the background scales to fill the entire viewport without distortion or gaps
3. **Given** a UI panel with child elements using percentage-based sizing, **When** the panel is resized, **Then** all children resize proportionally to maintain layout relationships
4. **Given** a UI layout with minimum and maximum size constraints, **When** the window becomes very small or very large, **Then** elements respect their min/max bounds rather than becoming unusably tiny or excessively large
5. **Given** a mobile/tablet game UI in portrait orientation, **When** the device is rotated to landscape, **Then** all UI elements recalculate their positions and sizes to fit the new screen dimensions
6. **Given** a windowed application, **When** the user maximizes, minimizes, or restores the window, **Then** all layouts recalculate smoothly without visual glitches or temporary misalignment
7. **Given** multiple rapid resize events (user dragging window edge), **When** multiple size changes occur within a single frame, **Then** the system batches them into a single layout recalculation to prevent performance issues

---

### User Story 2 - Aspect Ratio Handling (Priority: P2)

Game developers need UI to adapt gracefully to different aspect ratios (16:9, 21:9 ultrawide, 4:3, 16:10) without stretching, cropping critical content, or breaking layouts, ensuring consistent user experience across display types.

**Why this priority**: Critical for supporting ultrawide monitors and non-standard displays. Without this, UI can appear stretched, cropped, or poorly positioned on widescreen displays.

**Independent Test**: Create layout with colored Elements anchored to edges (red=top-left, green=top-right, blue=bottom-center). Render at 16:9 (1920x1080), then 21:9 (2560x1080), then 4:3 (1024x768). Use pixel sampling to verify each element maintains correct anchor position relative to screen edges at each aspect ratio.

**Acceptance Scenarios**:

1. **Given** a game menu designed for 16:9 aspect ratio, **When** displayed on a 21:9 ultrawide monitor, **Then** the menu maintains proper proportions (no stretching) and remains usable
2. **Given** critical UI elements (health bars, buttons) with safe-area positioning, **When** displayed on any aspect ratio, **Then** these elements remain fully visible and functional
3. **Given** decorative UI elements designed to fill screen edges, **When** aspect ratio changes, **Then** these elements adapt to fill appropriately without leaving gaps or overlapping critical content
4. **Given** text elements within a constrained layout, **When** aspect ratio changes significantly, **Then** text remains readable and doesn't overflow its container

---

### User Story 3 - Anchor-Based Positioning (Priority: P1)

Game developers need to position UI elements relative to specific screen edges or corners (top-left for HUD, bottom-center for action bar, top-right for mini-map) and have them stay anchored when screen dimensions change.

**Why this priority**: Essential for standard game UI patterns. HUD elements must stay at screen edges regardless of resolution, which is impossible with absolute positioning alone.

**Independent Test**: Create four colored Elements: red anchored top-left (-1,-1), green anchored top-right (1,-1), blue anchored bottom-left (-1,1), yellow anchored bottom-right (1,1). Sample pixels at expected element centers. Resize viewport. Re-sample to verify each element moved with its anchor point.

**Acceptance Scenarios**:

1. **Given** a mini-map element anchored to top-right corner, **When** the window is resized, **Then** the mini-map maintains a fixed distance from the top-right edge
2. **Given** a health bar anchored to top-left with 10-pixel margins, **When** resolution changes, **Then** the health bar remains 10 pixels from the top-left corner
3. **Given** multiple elements with different anchor points (top-left, top-right, bottom-center), **When** displayed simultaneously, **Then** each element positions correctly relative to its designated anchor
4. **Given** a centered dialog box with anchor point at (0, 0), **When** window dimensions change, **Then** the dialog remains perfectly centered

---

### User Story 4 - Automatic Child Arrangement (Priority: P2)

Game developers need layout containers that automatically arrange multiple child elements (horizontal lists, vertical menus, grids) with consistent spacing, alignment, and sizing without manually calculating each child's position.

**Why this priority**: Dramatically simplifies creation of lists, menus, and grids. Manual positioning is error-prone and doesn't adapt to dynamic content (different text lengths, varying item counts).

**Independent Test**: Create VerticalLayout with 3 colored Elements (red, green, blue) each 100x50 pixels with 10px spacing. Use pixel sampling to verify each element's vertical position matches expected layout (accounting for spacing). Dynamically add 4th yellow element. Re-sample to verify all 4 elements are correctly positioned with maintained spacing.

**Acceptance Scenarios**:

1. **Given** a VerticalLayout container with 5 children, **When** a 6th child is added, **Then** all children are automatically repositioned with even spacing
2. **Given** a HorizontalLayout with varying child widths, **When** alignment is set to "center", **Then** all children are vertically centered within the layout
3. **Given** a GridLayout configured for 3 columns, **When** 7 children are added, **Then** children automatically arrange into 3 rows (3-3-1 distribution)
4. **Given** a layout container with padding of 20 pixels, **When** children are arranged, **Then** all children respect the padding and are positioned 20 pixels from container edges

---

### User Story 5 - Small Screen Usability (Priority: P3)

Game developers need UI to remain usable on small screen resolutions (mobile phones, small tablets) and extreme aspect ratios (2:1 ultrawide phones) where fixed-size UI elements would be too large, overlap, or extend off-screen, making the application unusable.

**Why this priority**: Important for mobile/tablet support but less critical than core layout mechanics. Can be addressed through minimum size constraints and responsive sizing after P1/P2 features are working.

**Independent Test**: Create layout with multiple Elements designed for 1920x1080. Render at small phone resolution (e.g., 640x360). Use pixel sampling to verify elements scale down appropriately, remain on-screen, and don't overlap. All critical UI elements should be visible and distinguishable.

**Acceptance Scenarios**:

1. **Given** a UI layout designed for 1920x1080, **When** displayed on a 640x360 phone screen, **Then** UI elements scale down proportionally and remain usable (not cut off or overlapping)
2. **Given** multiple UI elements in a small viewport, **When** their combined preferred sizes exceed available space, **Then** elements respect minimum size constraints and container provides appropriate sizing (shrink-to-fit or clip)
3. **Given** text elements in a layout, **When** rendered at very small screen sizes, **Then** text remains readable at minimum font size threshold or layout adjusts to prevent tiny text
4. **Given** a 2:1 aspect ratio phone screen (very tall), **When** UI is displayed, **Then** layout adapts to use vertical space effectively without leaving excessive empty space or cramming content

---

### Edge Cases

- What happens when an element's minimum size exceeds available space in its container? (Container grows or enables scrolling, or element clips)
- How does system handle percentage-based sizing when parent has zero or undefined size? (Falls back to intrinsic size or minimum constraints)
- What happens with deeply nested layouts (layout within layout within layout)? (System recalculates from root to leaves, respecting constraint propagation)
- How does system handle circular dependencies in layout relationships? (Detects and breaks cycles, logs warnings, uses last valid layout)
- What happens when anchor points conflict with sizing constraints? (Constraints take precedence, anchor affects positioning within constraints)
- How does system handle aspect ratio preservation when both width and height are constrained? (Letterboxing/pillarboxing or overflow depending on configuration)
- What happens to layout when children are added/removed dynamically during runtime? (Layout invalidates and recalculates on next update frame)
- How does system handle elements with zero size? (Treated as valid but invisible, still participates in layout calculations)
- What happens when text content changes dynamically, altering element size? (Element size updates, parent layout recalculates if child size affects layout)
- What happens when multiple resize events occur in rapid succession (user dragging window edge)? (Events are coalesced and processed once per frame to prevent layout thrashing)
- How does system handle screen rotation on devices that support it? (Detects orientation change, updates viewport dimensions, triggers full layout recalculation)
- What happens during window state transitions (minimize/maximize/restore)? (Each transition triggers viewport update and layout recalculation; minimize may pause rendering but preserve layout state)
- How does system behave when window is dragged between monitors with different DPIs? (If DPI changes, global scale factor updates and all layouts recalculate with new scale)

## Requirements *(mandatory)*

### Functional Requirements

#### Core Layout Positioning (P1 - MVP)

- **FR-001**: System MUST support relative positioning where UI elements specify their position as percentages of parent container dimensions (e.g., 50% width, 25% from top)
- **FR-002**: System MUST support anchor-point based positioning where elements specify which point of themselves aligns with a position (top-left=-1,-1, center=0,0, bottom-right=1,1)
- **FR-003**: System MUST propagate size constraints from parent containers to child elements through `SetSizeConstraints()` method
- **FR-004**: Elements MUST recalculate their layout when size constraints change via `OnSizeConstraintsChanged()` callback
- **FR-005**: System MUST support absolute pixel positioning as an alternative to relative positioning for fixed-position elements
- **FR-006**: Layout containers MUST invalidate and recalculate when children are added, removed, or when their properties change

#### Responsive Sizing (P1 - MVP)

- **FR-007**: Elements MUST support minimum and maximum size constraints to prevent becoming too small or too large
- **FR-008**: Elements MUST support percentage-based width and height relative to parent container (e.g., "75% of parent width")
- **FR-009**: Elements MUST support intrinsic sizing where size is determined by content (e.g., text measurement, texture dimensions)
- **FR-010**: System MUST support "stretch to fill" mode where elements expand to fill available space within constraints
- **FR-011**: Root-level elements MUST receive size constraints equal to the current viewport/window dimensions

#### Aspect Ratio Management (P2)

- **FR-012**: Elements MUST support aspect ratio locking where changing one dimension automatically adjusts the other to maintain ratio
- **FR-013**: Background elements MUST support multiple scaling modes: fill (crop to fill), fit (letterbox), stretch (distort), native (1:1 pixels)
- **FR-014**: System MUST support safe area definitions for different aspect ratios to ensure critical UI remains visible

#### Layout Containers (P2)

- **FR-015**: VerticalLayout MUST arrange children vertically with configurable spacing, padding, and horizontal alignment (left, center, right, stretch)
- **FR-016**: HorizontalLayout MUST arrange children horizontally with configurable spacing, padding, and vertical alignment (top, center, bottom, stretch)
- **FR-017**: GridLayout MUST arrange children in a grid pattern with configurable rows, columns, spacing, and cell alignment
- **FR-018**: Layout containers MUST support Padding (inner margins) to offset children from container edges
- **FR-019**: Layout containers MUST support both "shrink-wrap" mode (size to content) and "fill-constraint" mode (size to parent constraints)
- **FR-020**: Layout containers MUST automatically trigger child re-layout when container size changes

#### Small Screen Adaptation (P3)

- **FR-021**: Elements MUST support minimum size constraints to prevent becoming unusably small on low-resolution displays
- **FR-022**: Layouts MUST handle cases where child elements' combined minimum sizes exceed container space (clip, overflow, or adjust sizing strategy)
- **FR-023**: System MUST provide responsive sizing modes that scale element sizes relative to viewport dimensions (e.g., "10% of viewport width")
- **FR-024**: Text elements MUST enforce minimum readable font sizes when viewport becomes very small

#### Dynamic Layout Updates (P2)

- **FR-025**: Layouts MUST recalculate automatically when window/viewport is resized
- **FR-026**: System MUST detect and respond to screen orientation changes (portrait to landscape rotation) by recalculating all layouts
- **FR-027**: System MUST handle window maximize/minimize/restore events by triggering layout recalculation
- **FR-028**: System MUST respond to display resolution changes (e.g., user changes system resolution) by updating viewport constraints and recalculating layouts
- **FR-029**: Layouts MUST support runtime addition/removal of children without breaking existing layout
- **FR-030**: Layout recalculation MUST be deferred until next update frame to batch multiple changes
- **FR-031**: System MUST prevent layout thrashing by coalescing multiple size change events within a single frame into one layout pass

### Key Entities

- **Element**: Base UI component with Position, AnchorPoint, Size, Scale properties inherited from Transformable; responds to size constraints
- **Layout**: Abstract container component that arranges child Elements according to layout algorithm; manages size constraint propagation
- **SizeConstraints (Rectangle<int>)**: Defines available space for an element (origin + dimensions); propagates from parent to child
- **AnchorPoint (Vector2D<float>)**: Normalized coordinate (-1 to 1) defining which point of element aligns with Position; affects layout calculation
- **Padding**: Inner margins within layout containers (left, top, right, bottom values in pixels)
- **Alignment (HorizontalAlignment, VerticalAlignment)**: How children position within their allocated space (left/center/right, top/center/bottom, stretch)
- **LayoutMode**: Configuration determining how elements size themselves (Fill, Shrink, Fixed, Percentage)
- **AspectRatio**: Width-to-height ratio that can be locked to maintain proportions during resizing
- **DPIScale**: Global multiplier converting logical pixels to physical pixels for high-DPI display support
- **Viewport**: The visible rendering area (window dimensions); source of root-level size constraints

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: UI layouts adapt correctly when window is resized between 1280x720 and 3840x2160 resolutions without code changes
- **SC-002**: UI elements maintain correct positions on displays with aspect ratios from 4:3 to 21:9 (ultrawide)
- **SC-003**: Layout recalculation completes within 1 millisecond for typical UI hierarchies (up to 50 elements)
- **SC-004**: Developers can position elements using anchor points without manually calculating pixel offsets for different resolutions
- **SC-005**: 90% reduction in hardcoded pixel coordinates for typical UI layouts compared to absolute positioning
- **SC-006**: UI designed for 1920x1080 remains usable when rendered at mobile phone resolution (640x360) with all critical elements visible
- **SC-007**: Layout containers automatically arrange children without manual position calculations for at least 95% of common use cases
- **SC-008**: Zero layout glitches or misalignment when dynamically adding/removing UI elements at runtime
- **SC-009**: UI hierarchies with up to 5 levels of nested layouts calculate correctly without performance degradation
- **SC-010**: Critical UI elements (health bars, action buttons, dialogs) remain fully visible and functional on all supported aspect ratios from 4:3 to 21:9
- **SC-011**: Screen orientation changes (portrait to landscape) complete layout recalculation within 2 frames (33ms at 60fps)
- **SC-012**: Window maximize/minimize/restore operations trigger layout updates without visual artifacts or temporary incorrect sizing
- **SC-013**: System handles 10+ rapid resize events per second (user dragging window) without frame rate drops below 60fps

## Testing Strategy

### Integration Testing Approach

Integration tests will use frame-based pixel sampling similar to existing UIElementTests and TextElementTests:

1. **Test Components**: Use colored Element instances (no textures) with distinct TintColor values for identification
2. **Layout Validation**: Define specific layout configurations (VerticalLayout, HorizontalLayout, GridLayout) with known Element children
3. **Pixel Sampling**: Sample specific screen coordinates to verify element positions and sizes by detecting expected colors
4. **Test Pattern**: 
   - Define layout template with colored Elements (Red, Green, Blue, Yellow, etc.)
   - Calculate expected pixel positions based on layout algorithm
   - Sample pixels at element centers and boundaries
   - Verify sampled colors match expected element TintColor values
5. **Resolution Testing**: Run same layout at different viewport sizes to verify responsive behavior
6. **Resize Testing**: Programmatically change viewport size mid-test and verify layout recalculates correctly

### Test Scenarios

- **Anchor Point Tests**: Elements with different anchor points (-1,-1, 0,0, 1,1) positioned at known screen locations
- **Layout Arrangement Tests**: VerticalLayout/HorizontalLayout with multiple colored children, verify spacing and alignment
- **Grid Layout Tests**: GridLayout with N columns, verify child positioning in correct cells
- **Responsive Resize Tests**: Change viewport dimensions, verify elements reposition/resize correctly
- **Constraint Propagation Tests**: Nested layouts, verify size constraints flow correctly parent → child
- **Aspect Ratio Tests**: Change viewport aspect ratio, verify layouts adapt appropriately

### Example Test Structure

```
ColoredRectangleLayoutTest {
    Background: Black (#000000)
    Layout: VerticalLayout with spacing=10, padding=20
    Children:
        - Red Element (#FF0000) - 100x50 pixels
        - Green Element (#00FF00) - 100x50 pixels  
        - Blue Element (#0000FF) - 100x50 pixels
    
    Sample Points:
        - (70, 45) → Expect Red (first element center)
        - (70, 115) → Expect Green (second element center)
        - (70, 185) → Expect Blue (third element center)
        - (50, 50) → Expect Black (padding area)
}
```

This leverages existing IPixelSampler infrastructure and RenderableTest base class used throughout the codebase.

## Assumptions

1. **Existing Architecture**: The current Element class with Position/AnchorPoint/Size/Scale model remains the foundation
2. **Limited Element Types**: Testing uses Element (colored rectangles) and TextElement only; no complex UI components required for validation
3. **Pixel Sampling Infrastructure**: Existing IPixelSampler service and RenderableTest pattern provide sufficient validation mechanism
4. **Constraint Propagation**: Parent-to-child size constraint flow through `SetSizeConstraints()` is the primary layout mechanism
5. **Frame-Based Updates**: Layout recalculations occur during the update phase, not immediately on property changes
6. **Single Viewport**: UI renders to a single viewport/window; multi-window support is out of scope
7. **2D UI Only**: Layout system applies to 2D UI elements only; 3D world-space UI is out of scope
8. **Left-to-Right Layout**: All layouts use left-to-right, top-to-bottom flow (no RTL localization in MVP)
9. **Pixel-Based Coordinates**: Internal calculations use integer pixel coordinates; sub-pixel positioning is out of scope
10. **No Animation During Layout**: Layout calculations assume static state; animated property changes happen after layout stabilizes
11. **Manual Refresh**: Viewport size changes are detected by application and propagated to root elements; no automatic OS event binding
12. **No High-DPI Upscaling**: System doesn't provide automatic high-DPI display scaling (e.g., 200% scaling for 4K monitors); OS-level scaling or fixed internal resolution assumed
13. **Downscaling Focus**: Performance priority is on scaling down to small screens (mobile), not upscaling for high-DPI (which is performance-expensive)
14. **Static Layout Algorithms**: Layout calculation methods are deterministic and don't involve randomness or learning
15. **No Scrolling**: Layout containers don't provide scrolling when content exceeds bounds (clipping only)
16. **Immediate Child Access**: Layout containers can efficiently enumerate immediate children without complex queries
17. **Z-Order Independent**: Layout calculation doesn't consider z-index or rendering order
18. **Property Animations Supported**: Layout-related properties (Position, Size, AnchorPoint, etc.) use ComponentProperty system with built-in animation support via source generation

## Out of Scope

The following are explicitly excluded from this feature:

1. **Constraint-Based Layout**: Complex constraint systems (like iOS Auto Layout) are not included; only simple parent-to-child constraint propagation
2. **Flexbox-Style Layout**: Advanced flex algorithms with flex-grow/flex-shrink/flex-basis properties are not implemented
3. **Scrolling Support**: Layout containers don't provide scroll bars or scroll views when content overflows
4. **Text Wrapping in Layouts**: Dynamic text wrapping and reflow within layout constraints is out of scope (text sizing is static)
5. **RTL Localization**: Right-to-left language support for layouts is not included
7. **Custom Layout Curves**: Non-linear spacing or distribution curves (e.g., easing functions for element positioning) not supported
8. **Layout Serialization**: Saving/loading layout configurations from external files is not included (templates only)
9. **Visual Layout Editor**: No GUI tool for designing layouts; all layout is code-based through templates
10. **Performance Profiling**: Built-in layout performance metrics and profiling tools are not included
11. **Multi-Window UI**: Layout system assumes single window/viewport; multi-window applications not supported
12. **3D UI Panels**: World-space UI panels in 3D environments are out of scope
13. **Accessibility Features**: Screen reader support, focus navigation, and accessibility APIs are not included
14. **Input Event Routing**: Layout system doesn't handle click detection, hover states, or input routing to children
15. **Theme System Integration**: Dynamic styling, themes, and style cascading are not part of the layout system
16. **High-DPI Upscaling**: Automatic rendering scale for high-DPI displays (Retina, 4K at 200%) is out of scope; use OS-level scaling or fixed internal resolution instead
17. **Multi-Resolution Asset Management**: System doesn't manage multiple texture resolutions (@1x, @2x, @3x) for different display densities
