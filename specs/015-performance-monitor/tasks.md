---
description: "Task list for PerformanceMonitor UI Template implementation"
---

# Tasks: PerformanceMonitor UI Template

**Input**: Design documents from `/specs/015-performance-monitor/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/component-contracts.md

**Tests**: This feature follows documentation-first TDD approach. All tests MUST be written before implementation (Red-Green-Refactor).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Repository structure: `src/GameEngine/`, `src/Tests/GameEngine/`, `src/TestApp/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and documentation updates

- [x] T001 Update `.github/copilot-instructions.md` with LayoutController architectural guidance
- [x] T002 [P] Update `src/GameEngine/README.md` with PerformanceMonitor template section
- [x] T003 [P] Verify VulkanSDK installation and shader compilation tools available

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core layout controller infrastructure that MUST be complete before user story implementation

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### VerticalLayoutController Implementation

- [x] T004 Create `src/Tests/GameEngine/GUI/Layout/VerticalLayoutController.Tests.cs` with failing tests for UpdateLayout behavior
- [x] T005 Create `src/GameEngine/GUI/Layout/VerticalLayoutController.cs` implementing LayoutController base class
- [x] T006 Implement `ItemSpacing` property with [TemplateProperty] and [ComponentProperty] attributes in VerticalLayoutController
- [x] T007 Implement `Alignment` property with [TemplateProperty] and [ComponentProperty] attributes in VerticalLayoutController
- [x] T008 Implement `Spacing` property with [TemplateProperty] and [ComponentProperty] attributes in VerticalLayoutController
- [x] T009 Implement `UpdateLayout(UserInterfaceElement container)` method for SpacingMode.Stacked in VerticalLayoutController
- [x] T010 Implement SpacingMode.Justified positioning logic in VerticalLayoutController
- [x] T011 Implement SpacingMode.Distributed positioning logic in VerticalLayoutController
- [x] T012 Add validation for ItemSpacing >= 0 and Alignment in [-1.0, 1.0] in VerticalLayoutController
- [x] T013 Add zero-height child exclusion logic in VerticalLayoutController.UpdateLayout
- [x] T014 Run tests for VerticalLayoutController and verify all pass (Green phase)

### HorizontalLayoutController Implementation

- [x] T015 [P] Create `src/Tests/GameEngine/GUI/Layout/HorizontalLayoutController.Tests.cs` with failing tests for UpdateLayout behavior
- [x] T016 [P] Create `src/GameEngine/GUI/Layout/HorizontalLayoutController.cs` implementing LayoutController base class
- [x] T017 [P] Implement `ItemSpacing` property with [TemplateProperty] and [ComponentProperty] attributes in HorizontalLayoutController
- [x] T018 [P] Implement `Alignment` property with [TemplateProperty] and [ComponentProperty] attributes in HorizontalLayoutController
- [x] T019 [P] Implement `Spacing` property with [TemplateProperty] and [ComponentProperty] attributes in HorizontalLayoutController
- [x] T020 [P] Implement `UpdateLayout(UserInterfaceElement container)` method for SpacingMode.Stacked in HorizontalLayoutController
- [x] T021 [P] Implement SpacingMode.Justified positioning logic in HorizontalLayoutController
- [x] T022 [P] Implement SpacingMode.Distributed positioning logic in HorizontalLayoutController
- [x] T023 [P] Add validation for ItemSpacing >= 0 and Alignment in [-1.0, 1.0] in HorizontalLayoutController
- [x] T024 [P] Add zero-width child exclusion logic in HorizontalLayoutController.UpdateLayout
- [x] T025 [P] Run tests for HorizontalLayoutController and verify all pass (Green phase)

### Build and Verification

- [x] T026 Run `dotnet build Nexus.GameEngine.sln --configuration Debug` and verify no errors
- [x] T027 Run `dotnet test Tests/Tests.csproj` and verify all layout controller tests pass

**Checkpoint**: Foundation ready - layout controller infrastructure complete, user story implementation can now begin

---

## Phase 3: User Story 1 - Display Real-Time Performance Overlay (Priority: P1) 🎯 MVP

**Goal**: Create a visual on-screen overlay showing FPS, frame time, and subsystem timings during development

**Independent Test**: Add PerformanceMonitor template to test scene, verify metrics appear and update in real-time

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T028 [P] [US1] Create integration test in `src/Tests/IntegrationTests/PerformanceMonitorTemplateTests.cs` for template instantiation
- [x] T029 [P] [US1] Add test case for property binding synchronization (PerformanceMonitor → TextRenderer.Text) in PerformanceMonitorTemplateTests
- [x] T030 [P] [US1] Add test case for vertical layout positioning of text elements in PerformanceMonitorTemplateTests
- [x] T031 [P] [US1] Add test case for performance warning indicator visibility binding in PerformanceMonitorTemplateTests
- [x] T032 [P] [US1] Run integration tests and verify all FAIL (Red phase)

### Implementation for User Story 1

- [x] T033 [US1] Create `src/GameEngine/Templates/PerformanceMonitorTemplate.cs` with UserInterfaceElement root structure
- [x] T034 [US1] Add PerformanceMonitor child component to template with Enabled, UpdateIntervalSeconds, and WarningThresholdMs properties
- [x] T035 [US1] Add VerticalLayoutController child component to template with ItemSpacing=2.0, Alignment=-1.0, Spacing=Stacked
- [x] T036 [US1] Add TextRenderer child for CurrentFps binding with StringFormatConverter("FPS: {0:F1}") in PerformanceMonitorTemplate
- [x] T037 [US1] Add TextRenderer child for AverageFps binding with StringFormatConverter("Avg: {0:F1}") in PerformanceMonitorTemplate
- [x] T038 [US1] Add TextRenderer child for CurrentFrameTimeMs binding with StringFormatConverter("Frame: {0:F2}ms") in PerformanceMonitorTemplate
- [x] T039 [US1] Add TextRenderer child for UpdateTimeMs binding in PerformanceMonitorTemplate
- [x] T040 [US1] Add TextRenderer child for RenderTimeMs binding in PerformanceMonitorTemplate
- [x] T041 [US1] Add TextRenderer child for performance warning indicator with Visible binding to PerformanceWarning property
- [x] T042 [US1] Configure Position=(10,10), Alignment=TopLeft for root UserInterfaceElement in template
- [x] T043 [US1] Run integration tests for PerformanceMonitorTemplate and verify all pass (Green phase)

### Example Implementation

- [x] T044 [US1] Create example usage in `src/TestApp/Templates/PerformanceMonitorExample.cs` demonstrating template instantiation
- [x] T045 [US1] Update TestApp startup to include PerformanceMonitor overlay for visual testing

### Build and Verification

- [x] T046 [US1] Run `dotnet build Nexus.GameEngine.sln --configuration Debug` and address any warnings/errors
- [x] T047 [US1] Run `dotnet test Tests/Tests.csproj` and verify all User Story 1 tests pass
- [x] T048 [US1] Run TestApp and manually verify performance overlay displays FPS and frame time metrics

**Checkpoint**: User Story 1 complete - performance overlay displays and updates metrics independently

---

## Phase 4: User Story 2 - Toggle Overlay Visibility (Priority: P2)

**Goal**: Enable show/hide of performance overlay without affecting data collection

**Independent Test**: Toggle Visible property at runtime and verify overlay appears/disappears while metrics continue updating

### Tests for User Story 2

- [x] T049 [P] [US2] Add test case for visibility toggle in `src/Tests/IntegrationTests/PerformanceMonitorTemplateTests.cs`
- [x] T050 [P] [US2] Add test case verifying data collection continues when Visible=false in PerformanceMonitorTemplateTests
- [x] T051 [P] [US2] Run User Story 2 tests and verify they FAIL (Red phase)

### Implementation for User Story 2

- [x] T052 [US2] Add key binding (F10) to toggle PerformanceMonitor overlay visibility in `src/TestApp/Templates/PerformanceMonitorExample.cs`
- [x] T053 [US2] Verify UserInterfaceElement.Visible property propagates to all child components
- [x] T054 [US2] Run User Story 2 tests and verify they pass (Green phase)

### Manual Testing

- [x] T055 [US2] Run TestApp and verify F10 key toggles overlay visibility
- [x] T056 [US2] Verify metrics continue updating when overlay is hidden

**Checkpoint**: User Story 2 complete - visibility toggle works independently without breaking US1

---

## Phase 5: User Story 3 - Customize Overlay Position and Appearance (Priority: P3)

**Goal**: Allow developers to position overlay and adjust appearance to suit workflow preferences

**Independent Test**: Modify Position, Alignment, Padding properties and verify overlay relocates correctly

### Tests for User Story 3

- [x] T057 [P] [US3] Add test case for Position and Alignment property changes in `src/Tests/IntegrationTests/PerformanceMonitorTemplateTests.cs`
- [x] T058 [P] [US3] Add test case for ItemSpacing changes in VerticalLayoutController in PerformanceMonitorTemplateTests
- [x] T059 [P] [US3] Add test case for window resize maintaining relative position in PerformanceMonitorTemplateTests
- [x] T060 [P] [US3] Run User Story 3 tests and verify they FAIL (Red phase)

### Implementation for User Story 3

- [x] T061 [US3] Add customization example in `src/TestApp/Templates/PerformanceMonitorExample.cs` showing different Position/Alignment configurations
- [x] T062 [US3] Add example for top-right corner positioning (Position=(10,10), Alignment=TopRight)
- [x] T063 [US3] Add example for bottom-left corner positioning (Position=(10,10), Alignment=BottomLeft)
- [x] T064 [US3] Add example for increased ItemSpacing (5.0f) for more readable layout
- [x] T065 [US3] Run User Story 3 tests and verify they pass (Green phase)

### Manual Testing

- [x] T066 [US3] Run TestApp and verify overlay appears at configured positions
- [x] T067 [US3] Resize window and verify overlay maintains relative position based on Alignment

**Checkpoint**: All user stories complete - overlay is fully customizable and independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and finalize documentation

- [x] T068 [P] Update `README.md` in repository root with PerformanceMonitor template feature announcement
- [x] T069 [P] Create documentation in `src/GameEngine/Templates/README.md` explaining PerformanceMonitor template usage
- [x] T070 [P] Add code coverage analysis and verify 80% coverage target met for LayoutController classes
- [x] T071 [P] Add XML documentation comments to VerticalLayoutController and HorizontalLayoutController
- [x] T072 [P] Add XML documentation comments to PerformanceMonitorTemplate
- [x] T073 Refactor common layout logic between Vertical/HorizontalLayoutController to reduce duplication
- [x] T074 Performance profiling: verify overlay overhead < 1ms per frame
- [x] T075 Run quickstart.md validation: follow quickstart.md steps and verify they produce expected results

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Integrates with US1 but independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Integrates with US1 but independently testable

### Within Each User Story

1. Tests MUST be written and FAIL before implementation (Red phase)
2. Implementation tasks complete (Green phase)
3. Build and verify all tests pass
4. Manual testing and validation
5. Story complete before moving to next priority

### Parallel Opportunities

**Setup Phase (Phase 1)**:
- T002 (README update) and T003 (VulkanSDK verification) can run in parallel

**Foundational Phase (Phase 2)**:
- T004-T014 (VerticalLayoutController) can run in parallel with T015-T025 (HorizontalLayoutController)
- Both layout controllers are independent implementations

**User Story 1 (Phase 3)**:
- T028-T032 (all test creation) can run in parallel before implementation
- T036-T041 (TextRenderer child additions) can run in parallel after T033-T035 complete

**User Story 2 (Phase 4)**:
- T049-T051 (all test creation) can run in parallel

**User Story 3 (Phase 5)**:
- T057-T060 (all test creation) can run in parallel
- T062-T064 (example implementations) can run in parallel after T061

**Polish Phase (Phase 6)**:
- T068-T072 (documentation tasks) can all run in parallel
- T073-T074 (refactoring and profiling) depend on all user stories

---

## Parallel Example: Foundational Phase

```bash
# Developer A:
Task T004-T014: "Implement VerticalLayoutController in src/GameEngine/GUI/Layout/VerticalLayoutController.cs"

# Developer B (in parallel):
Task T015-T025: "Implement HorizontalLayoutController in src/GameEngine/GUI/Layout/HorizontalLayoutController.cs"

# Both can work simultaneously - different files, no dependencies
```

---

## Parallel Example: User Story 1

```bash
# Launch all test creation tasks together:
Task T028: "Integration test for template instantiation"
Task T029: "Test for property binding synchronization"
Task T030: "Test for vertical layout positioning"
Task T031: "Test for warning indicator visibility"

# After tests fail (Red phase), implement TextRenderer children in parallel:
Task T036: "Add CurrentFps TextRenderer binding"
Task T037: "Add AverageFps TextRenderer binding"
Task T038: "Add FrameTime TextRenderer binding"
Task T039: "Add UpdateTimeMs TextRenderer binding"
Task T040: "Add RenderTimeMs TextRenderer binding"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: Foundational (T004-T027) - CRITICAL
3. Complete Phase 3: User Story 1 (T028-T048)
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready - developers now have basic performance overlay

### Incremental Delivery

1. **Foundation**: Complete Setup + Foundational (T001-T027) → Layout infrastructure ready
2. **MVP**: Add User Story 1 (T028-T048) → Test independently → Deploy/Demo - basic overlay working!
3. **Quality of Life**: Add User Story 2 (T049-T056) → Test independently → Deploy/Demo - toggle visibility
4. **Customization**: Add User Story 3 (T057-T067) → Test independently → Deploy/Demo - full positioning control
5. **Polish**: Complete Phase 6 (T068-T075) → Production ready

Each story adds value without breaking previous stories.

### Parallel Team Strategy

With multiple developers:

1. **Team completes Setup + Foundational together** (T001-T027)
   - Developer A: VerticalLayoutController (T004-T014)
   - Developer B: HorizontalLayoutController (T015-T025)
2. **Once Foundational is done:**
   - Developer A: User Story 1 (T028-T048)
   - Developer B: User Story 2 (T049-T056)
   - Developer C: User Story 3 (T057-T067)
3. Stories complete and integrate independently

---

## Notes

- **[P] tasks**: Different files, no dependencies - can run in parallel
- **[Story] label**: Maps task to specific user story for traceability
- **Each user story**: Independently completable and testable
- **TDD workflow**: Verify tests fail (Red) before implementing (Green), then refactor
- **Commit frequency**: After each task or logical group
- **Checkpoints**: Stop at any checkpoint to validate story independently
- **Build verification**: Run `dotnet build` and `dotnet test` after each phase
- **Manual testing**: Use TestApp for visual verification and integration testing

---

## Summary

- **Total Tasks**: 75
- **Phase 1 (Setup)**: 3 tasks
- **Phase 2 (Foundational)**: 24 tasks (VerticalLayoutController: 11, HorizontalLayoutController: 11, Build: 2)
- **Phase 3 (User Story 1)**: 21 tasks (Tests: 5, Implementation: 13, Examples: 2, Build: 3)
- **Phase 4 (User Story 2)**: 8 tasks (Tests: 3, Implementation: 2, Manual: 2)
- **Phase 5 (User Story 3)**: 11 tasks (Tests: 4, Implementation: 4, Manual: 2)
- **Phase 6 (Polish)**: 8 tasks
- **Parallel Opportunities**: 35+ tasks marked [P] can run in parallel
- **MVP Scope**: Phase 1-3 (48 tasks) delivers basic performance overlay
- **Independent Test Criteria**: Each user story has clear test validation and manual verification steps
