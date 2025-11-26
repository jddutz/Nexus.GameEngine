# Tasks: Fix UserInterfaceElement Implementation

**Feature Branch**: `010-fix-ui-element`
**Status**: In Progress

## Phase 1: Setup
*Goal: Prepare geometry and shaders for the new coordinate system.*

- [x] T001 Update `TexturedQuad` geometry to use 0..1 coordinates in `src/GameEngine/Resources/Geometry/Definitions/TexturedQuad.cs`
- [x] T002 Verify and update `ui.vert` shader to support 0..1 geometry and Pivot in `src/GameEngine/Shaders/ui.vert`

## Phase 2: Foundational
*Goal: Implement robust bounds calculation and caching in the base component.*

- [x] T003 Implement bounds caching (`_cachedBounds`, `_isBoundsDirty`) in `src/GameEngine/Components/RectTransform.cs`
- [x] T004 Implement partial methods (`OnPositionChanged`, etc.) to invalidate bounds in `src/GameEngine/Components/RectTransform.cs`
- [x] T005 Create unit tests for `RectTransform` bounds calculation in `src/Tests/GameEngine/Components/RectTransform.Tests.cs`

## Phase 3: User Story 1 - Correct Bounds Calculation
*Goal: Ensure UserInterfaceElement uses the new RectTransform system correctly.*

- [x] T006 [US1] Update `UserInterfaceElement` to inherit from `RectTransform` in `src/GameEngine/GUI/UserInterfaceElement.cs`
- [x] T007 [US1] Remove duplicate properties (`_size`, `_pivot`) and legacy `_anchorPoint` from `src/GameEngine/GUI/UserInterfaceElement.cs`
- [x] T008 [US1] Create unit tests for `UserInterfaceElement` bounds verification in `src/Tests/GameEngine/GUI/UserInterfaceElement.Tests.cs`

## Phase 4: User Story 2 - Transform Support
*Goal: Verify dynamic updates work as expected.*

- [x] T009 [US2] Add test cases for dynamic property updates (Position, Rotation, Scale) in `src/Tests/GameEngine/Components/RectTransform.Tests.cs`
- [x] T010 [US2] Verify `UserInterfaceElement` respects transform changes in `src/Tests/GameEngine/GUI/UserInterfaceElement.Tests.cs`

## Phase 5: Polish
*Goal: Cleanup and final verification.*

- [x] T011 Remove any remaining legacy coordinate system code in `src/GameEngine/GUI/UserInterfaceElement.cs`
- [x] T012 Run all tests and ensure 100% pass rate for UI components

## Dependencies

- US1 depends on Foundational tasks (RectTransform implementation).
- US2 depends on US1 (UserInterfaceElement must inherit RectTransform).

## Implementation Strategy

1.  **Geometry & Shader**: Fix the low-level rendering assumptions first.
2.  **RectTransform**: Harden the base class with caching and correct math.
3.  **UserInterfaceElement**: Switch the inheritance and clean up the API.
4.  **Verification**: Add comprehensive tests to ensure no regressions in layout or input detection.
