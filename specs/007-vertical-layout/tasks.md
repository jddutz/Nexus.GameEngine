---
description: "Task list for Directional Layout Components (VerticalLayout & HorizontalLayout)"
---

# Tasks: Directional Layout Components (VerticalLayout & HorizontalLayout)

**Input**: Design documents from `/specs/007-vertical-layout/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contract.md, quickstart.md

**Tests**: Included - This feature follows documentation-first TDD approach per constitution

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Single project structure:
- Source: `src/GameEngine/`
- Tests: `Tests/GameEngine/`
- Integration Tests: `TestApp/Tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and documentation updates

- [X] T001 Build solution to verify clean build state using `dotnet build Nexus.GameEngine.sln --configuration Debug`
- [X] T002 [P] Update research.md to reflect property-based design (remove enum modes, add four properties + SpacingMode)
- [X] T003 [P] Update data-model.md to add HorizontalLayout, SpacingMode enum, remove VerticalLayoutMode enum
- [X] T004 [P] Update quickstart.md with property-based examples for both VerticalLayout and HorizontalLayout
- [X] T005 [P] Update contracts/api-contract.md to document SpacingMode enum and dual layout APIs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core enums and base patterns that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 Create SpacingMode enum with Justified and Distributed values in src/GameEngine/GUI/Layout/SpacingMode.cs
- [X] T007 Write failing unit tests for SpacingMode enum in Tests/GameEngine/GUI/Layout/SpacingMode.Tests.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Basic Directional Stacking (Priority: P1) 🎯 MVP

**Goal**: Enable UI developers to arrange elements vertically or horizontally with configurable spacing and alignment, eliminating manual position calculations for common patterns like menus, toolbars, and navigation bars.

**Independent Test**: Create VerticalLayout or HorizontalLayout with 3+ children and verify they stack with ItemSpacing=10 and various Alignment values, delivering working menus/toolbars/navigation.

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T008 [P] [US1] Write failing unit tests for VerticalLayout.ItemSpacing property in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [X] T009 [P] [US1] Write failing unit tests for VerticalLayout with Alignment.Y=-1 (top-aligned) in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [X] T010 [P] [US1] Write failing unit tests for VerticalLayout with Alignment.Y=0 (center-aligned) in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [X] T011 [P] [US1] Write failing unit tests for VerticalLayout with Alignment.Y=1 (bottom-aligned) in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [X] T012 [P] [US1] Write failing unit tests for HorizontalLayout.ItemSpacing property in Tests/GameEngine/GUI/Layout/HorizontalLayout.Tests.cs
- [X] T013 [P] [US1] Write failing unit tests for HorizontalLayout with Alignment.X=-1, 0, 1 in Tests/GameEngine/GUI/Layout/HorizontalLayout.Tests.cs

### Implementation for User Story 1

- [X] T014 [P] [US1] Add ItemSpacing property with [ComponentProperty] and [TemplateProperty] attributes to VerticalLayout in src/GameEngine/GUI/Layout/VerticalLayout.cs
- [X] T015 [P] [US1] Create HorizontalLayout class extending Container in src/GameEngine/GUI/Layout/HorizontalLayout.cs
- [X] T016 [US1] Implement UpdateLayout() for VerticalLayout with ItemSpacing logic and Alignment.Y support in src/GameEngine/GUI/Layout/VerticalLayout.cs
- [X] T017 [US1] Implement UpdateLayout() for HorizontalLayout with ItemSpacing logic and Alignment.X support in src/GameEngine/GUI/Layout/HorizontalLayout.cs
- [X] T018 [US1] Add OnItemSpacingChanged() partial method to invalidate layout in src/GameEngine/GUI/Layout/VerticalLayout.cs
- [X] T019 [US1] Add OnItemSpacingChanged() partial method to invalidate layout in src/GameEngine/GUI/Layout/HorizontalLayout.cs
- [X] T020 [US1] Run unit tests and verify they pass (Green phase)
- [ ] T021 [P] [US1] Create integration test for vertical menu with ItemSpacing in TestApp/Tests/VerticalLayoutTests.cs
- [ ] T022 [P] [US1] Create integration test for horizontal toolbar with ItemSpacing in TestApp/Tests/HorizontalLayoutTests.cs

**Checkpoint**: At this point, User Story 1 should be fully functional - directional stacking with fixed spacing works independently

---

## Phase 4: User Story 2 - Calculated Spacing Distribution (Priority: P2)

**Goal**: Enable UI developers to distribute children evenly with automatic spacing calculation (space-between or space-evenly patterns) instead of fixed gaps, for professional balanced layouts.

**Independent Test**: Create VerticalLayout with ItemSpacing=null and Spacing=Justified or Distributed, verify automatic spacing calculation delivers balanced layouts.

### Tests for User Story 2

- [X] T023 [P] [US2] Write failing unit tests for VerticalLayout with Spacing=SpacingMode.Justified in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [X] T024 [P] [US2] Write failing unit tests for VerticalLayout with Spacing=SpacingMode.Distributed in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [X] T025 [P] [US2] Write failing unit tests for HorizontalLayout with Spacing=SpacingMode.Justified in Tests/GameEngine/GUI/Layout/HorizontalLayout.Tests.cs
- [X] T026 [P] [US2] Write failing unit tests for HorizontalLayout with Spacing=SpacingMode.Distributed in Tests/GameEngine/GUI/Layout/HorizontalLayout.Tests.cs
- [X] T027 [P] [US2] Write edge case tests for single child with both SpacingMode values in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs

### Implementation for User Story 2

- [X] T028 [P] [US2] Add Spacing property (SpacingMode type) with [ComponentProperty] and [TemplateProperty] to VerticalLayout in src/GameEngine/GUI/Layout/VerticalLayout.cs
- [X] T029 [P] [US2] Add Spacing property (SpacingMode type) with [ComponentProperty] and [TemplateProperty] to HorizontalLayout in src/GameEngine/GUI/Layout/HorizontalLayout.cs
- [X] T030 [US2] Implement SpacingMode.Justified algorithm (space-between) in VerticalLayout.UpdateLayout() in src/GameEngine/GUI/Layout/VerticalLayout.cs
- [X] T031 [US2] Implement SpacingMode.Distributed algorithm (space-evenly) in VerticalLayout.UpdateLayout() in src/GameEngine/GUI/Layout/VerticalLayout.cs
- [X] T032 [US2] Implement SpacingMode.Justified algorithm in HorizontalLayout.UpdateLayout() in src/GameEngine/GUI/Layout/HorizontalLayout.cs
- [X] T033 [US2] Implement SpacingMode.Distributed algorithm in HorizontalLayout.UpdateLayout() in src/GameEngine/GUI/Layout/HorizontalLayout.cs
- [X] T034 [US2] Add OnSpacingChanged() partial method to invalidate layout in src/GameEngine/GUI/Layout/VerticalLayout.cs
- [X] T035 [US2] Add OnSpacingChanged() partial method to invalidate layout in src/GameEngine/GUI/Layout/HorizontalLayout.cs
- [X] T036 [US2] Run unit tests and verify they pass (Green phase)
- [ ] T037 [P] [US2] Create integration test for navigation menu with calculated spacing in TestApp/Tests/VerticalLayoutTests.cs
- [ ] T038 [P] [US2] Create integration test for toolbar with distributed spacing in TestApp/Tests/HorizontalLayoutTests.cs

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - fixed and calculated spacing modes

---

## Phase 5: User Story 3 - Fixed Item Sizing (Priority: P2)

**Goal**: Enable UI developers to override child sizes uniformly (all list items same height, all toolbar buttons same width) without setting size on each child, simplifying template creation.

**Independent Test**: Create VerticalLayout with ItemHeight=60 and verify all children are forced to 60px height regardless of their measured size, delivering uniform list items.

### Tests for User Story 3

- [X] T039 [P] [US3] Write failing unit tests for VerticalLayout.ItemHeight property overriding child sizes in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [X] T040 [P] [US3] Write failing unit tests for HorizontalLayout.ItemWidth property overriding child sizes in Tests/GameEngine/GUI/Layout/HorizontalLayout.Tests.cs
- [X] T041 [P] [US3] Write tests for ItemHeight/ItemWidth with varying child sizes in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [X] T042 [P] [US3] Write tests verifying ItemHeight takes precedence over child.Measure() in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs

### Implementation for User Story 3

- [X] T043 [P] [US3] Rename existing ItemHeight property or verify it exists with correct attributes in src/GameEngine/GUI/Layout/VerticalLayout.cs
- [X] T044 [P] [US3] Add ItemWidth property with [ComponentProperty] and [TemplateProperty] to HorizontalLayout in src/GameEngine/GUI/Layout/HorizontalLayout.cs
- [X] T045 [US3] Update VerticalLayout.UpdateLayout() to check ItemHeight before child.Measure() in src/GameEngine/GUI/Layout/VerticalLayout.cs
- [X] T046 [US3] Update HorizontalLayout.UpdateLayout() to check ItemWidth before child.Measure() in src/GameEngine/GUI/Layout/HorizontalLayout.cs
- [X] T047 [US3] Add OnItemHeightChanged() partial method to invalidate layout in src/GameEngine/GUI/Layout/VerticalLayout.cs
- [X] T048 [US3] Add OnItemWidthChanged() partial method to invalidate layout in src/GameEngine/GUI/Layout/HorizontalLayout.cs
- [X] T049 [US3] Run unit tests and verify they pass (Green phase)
- [ ] T050 [P] [US3] Create integration test for uniform-height list items in TestApp/Tests/VerticalLayoutTests.cs
- [ ] T051 [P] [US3] Create integration test for uniform-width toolbar buttons in TestApp/Tests/HorizontalLayoutTests.cs

**Checkpoint**: All three user stories complete - basic stacking, calculated spacing, and fixed sizing deliver full directional layout functionality

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories, edge case handling, and final validation

- [ ] T052 [P] Write tests for zero-size children (child.Measure() returns 0) being excluded from layout in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [ ] T053 [P] Write tests for empty children collection in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [ ] T054 [P] Write tests for single child edge cases in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [ ] T055 [P] Write tests for nested layouts (VerticalLayout within HorizontalLayout) in TestApp/Tests/VerticalLayoutTests.cs
- [ ] T056 [P] Write tests for dynamic child addition/removal in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [ ] T057 [P] Write tests for container resize triggering layout invalidation in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [ ] T058 [P] Write tests for property changes triggering layout invalidation in Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs
- [ ] T059 [P] Create integration test with 100+ children to verify performance in TestApp/Tests/VerticalLayoutTests.cs
- [ ] T060 Add zero-size exclusion logic to VerticalLayout.UpdateLayout() in src/GameEngine/GUI/Layout/VerticalLayout.cs
- [ ] T061 Add zero-size exclusion logic to HorizontalLayout.UpdateLayout() in src/GameEngine/GUI/Layout/HorizontalLayout.cs
- [ ] T062 Add XML documentation comments to VerticalLayout properties and methods in src/GameEngine/GUI/Layout/VerticalLayout.cs
- [ ] T063 Add XML documentation comments to HorizontalLayout properties and methods in src/GameEngine/GUI/Layout/HorizontalLayout.cs
- [ ] T064 Add XML documentation comments to SpacingMode enum in src/GameEngine/GUI/Layout/SpacingMode.cs
- [ ] T065 Build solution and verify no warnings or errors using `dotnet build Nexus.GameEngine.sln --configuration Debug`
- [ ] T066 Run all unit tests and verify 80%+ code coverage using `dotnet test Tests/Tests.csproj`
- [ ] T067 Run quickstart.md validation - manually test all examples from quickstart.md
- [ ] T068 Update plan.md with final implementation summary and any design adjustments

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - User stories can proceed in parallel (if staffed) or sequentially by priority
  - Each story is independently testable
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1) - Basic Stacking**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2) - Calculated Spacing**: Can start after Foundational (Phase 2) - Independent from US1, but builds on same UpdateLayout() method
- **User Story 3 (P2) - Fixed Sizing**: Can start after Foundational (Phase 2) - Independent from US1/US2, but modifies same UpdateLayout() method

**Note**: While user stories are independent from a testing perspective, they all modify the same `UpdateLayout()` method, so parallel development requires careful coordination or sequential implementation by priority.

### Within Each User Story

1. Tests MUST be written and FAIL before implementation (Red phase)
2. Add properties to component classes
3. Implement layout algorithm in UpdateLayout()
4. Add invalidation hooks (partial methods)
5. Run unit tests - verify they pass (Green phase)
6. Create integration tests
7. Story complete before moving to next priority

### Parallel Opportunities

**Setup Phase (Phase 1)**:
- T002, T003, T004, T005 can all run in parallel (different documentation files)

**Within Each User Story - Tests**:
- All unit test creation tasks marked [P] within a story can run in parallel
- Example US1: T008, T009, T010, T011, T012, T013 can all be written in parallel

**Within Each User Story - Implementation**:
- Property additions marked [P] can run in parallel (VerticalLayout vs HorizontalLayout)
- Example US1: T014 (VerticalLayout) and T015 (HorizontalLayout) can run in parallel
- Integration tests marked [P] can run in parallel
- Example US1: T021 and T022 can run in parallel

**Polish Phase (Phase 6)**:
- T052, T053, T054, T055, T056, T057, T058, T059 (all test tasks) can run in parallel
- T062, T063, T064 (documentation tasks) can run in parallel

**Cross-Story Parallelization**:
- If team has 2+ developers, different user stories can be worked on in parallel
- Requires careful merge coordination since all modify UpdateLayout()
- Recommended: Sequential by priority (P1 → P2 → P2) to minimize conflicts

---

## Parallel Example: User Story 1

```bash
# Launch all unit tests for User Story 1 together:
Task: T008 - "Write failing unit tests for VerticalLayout.ItemSpacing property"
Task: T009 - "Write failing unit tests for VerticalLayout with Alignment.Y=-1"
Task: T010 - "Write failing unit tests for VerticalLayout with Alignment.Y=0"
Task: T011 - "Write failing unit tests for VerticalLayout with Alignment.Y=1"
Task: T012 - "Write failing unit tests for HorizontalLayout.ItemSpacing property"
Task: T013 - "Write failing unit tests for HorizontalLayout with Alignment.X=-1, 0, 1"

# Launch property additions together (different classes):
Task: T014 - "Add ItemSpacing property to VerticalLayout"
Task: T015 - "Create HorizontalLayout class extending Container"

# Launch integration tests together:
Task: T021 - "Create integration test for vertical menu"
Task: T022 - "Create integration test for horizontal toolbar"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (documentation updates)
2. Complete Phase 2: Foundational (SpacingMode enum)
3. Complete Phase 3: User Story 1 (Basic stacking with ItemSpacing and Alignment)
4. **STOP and VALIDATE**: Test User Story 1 independently with real UI scenarios
5. Demo/validate with stakeholders if ready

**MVP Delivers**: Vertical and horizontal stacking with fixed spacing and alignment - covers most common UI patterns (menus, toolbars, navigation bars).

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → **MVP Complete** (basic stacking works)
3. Add User Story 2 → Test independently → Calculated spacing enabled
4. Add User Story 3 → Test independently → Uniform sizing enabled
5. Polish phase → Edge cases, documentation, performance validation
6. Each story adds value without breaking previous stories

### Sequential Team Strategy (Recommended)

Single developer working sequentially by priority:
1. Complete Setup + Foundational
2. Implement US1 (P1) - Basic stacking
3. Implement US2 (P2) - Calculated spacing
4. Implement US3 (P2) - Fixed sizing
5. Polish and validate

**Advantage**: No merge conflicts, clear progress tracking, immediate validation after each story

### Parallel Team Strategy (Advanced)

With 2 developers working in parallel:
1. **Both**: Complete Setup + Foundational together
2. **Split by layout type**:
   - Developer A: VerticalLayout implementation across all stories
   - Developer B: HorizontalLayout implementation across all stories
3. **Coordination**: Frequent syncs to ensure identical API patterns
4. **Integration**: Merge and test together

**Advantage**: Faster completion if team has capacity
**Risk**: Merge conflicts in UpdateLayout() methods, requires careful coordination

---

## Task Count Summary

- **Total Tasks**: 68
- **Completed Tasks**: 49
- **Remaining Tasks**: 19
- **Phase 1 (Setup)**: 5 tasks ✅ Complete
- **Phase 2 (Foundational)**: 2 tasks ✅ Complete
- **Phase 3 (US1 - Basic Stacking)**: 15 tasks (13 complete, 2 integration tests remain)
- **Phase 4 (US2 - Calculated Spacing)**: 16 tasks (14 complete, 2 integration tests remain)
- **Phase 5 (US3 - Fixed Sizing)**: 13 tasks (11 complete, 2 integration tests remain)
- **Phase 6 (Polish)**: 17 tasks (0 complete)

### Parallel Opportunities Identified

- **Setup Phase**: 4 parallel tasks (documentation updates)
- **User Story 1**: 6 parallel tests, 2 parallel properties, 2 parallel integration tests
- **User Story 2**: 5 parallel tests, 2 parallel properties, 2 parallel integration tests
- **User Story 3**: 4 parallel tests, 2 parallel properties, 2 parallel integration tests
- **Polish Phase**: 8 parallel tests, 3 parallel documentation tasks

**Total Parallel Tasks**: 40 tasks marked [P] (59% of all tasks)

### Independent Test Criteria

- **User Story 1**: Create layout with 3+ children, ItemSpacing=10, verify stacking and alignment
- **User Story 2**: Create layout with ItemSpacing=null, Spacing=Justified/Distributed, verify automatic spacing
- **User Story 3**: Create layout with ItemHeight/ItemWidth=60, verify uniform sizing

### Suggested MVP Scope

**MVP = User Story 1 only** (Basic Directional Stacking)
- Delivers: VerticalLayout and HorizontalLayout with ItemSpacing and Alignment support
- Enables: Menus, toolbars, navigation bars with fixed spacing
- Task Count: 22 tasks (Setup + Foundational + US1)
- Estimated Effort: 1-2 days for experienced developer
- Value: Eliminates manual position calculations for most common UI patterns

---

## Notes

- All tasks follow the strict checklist format: `- [ ] [ID] [P?] [Story?] Description with file path`
- [P] tasks target different files or independent components to enable parallelization
- [Story] labels map tasks to specific user stories for traceability and independent delivery
- Tests are written FIRST (Red phase) before implementation (TDD workflow)
- Each user story should be independently completable and testable
- Commit after logical groups or completed stories
- Stop at any checkpoint to validate story independently
- Format validated: All tasks include checkbox, sequential ID, optional [P] marker, story label for US tasks, and exact file paths
