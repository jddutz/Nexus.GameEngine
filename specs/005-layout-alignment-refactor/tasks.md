---

description: "Task list for Layout Alignment Refactor implementation"
---

# Tasks: Layout Alignment Refactor

**Input**: Design documents from `/specs/005-layout-alignment-refactor/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Following documentation-first TDD approach per constitution.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/GameEngine/`
- **Tests**: `src/Tests/GameEngine/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify build environment and document update

- [ ] T001 Build solution to verify clean baseline using `dotnet build Nexus.GameEngine.sln --configuration Debug`
- [ ] T002 Update feature documentation in `specs/005-layout-alignment-refactor/README.md` (if needed) to reflect implementation start
- [ ] T003 Review existing `src/GameEngine/GUI/Align.cs` to confirm Vector2D<float> constants are available

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure changes needed before any user story implementation

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T004 Review source generators to confirm Vector2D<float> support in `src/SourceGenerators/ComponentPropertyGenerator.cs` and `src/SourceGenerators/TemplateGenerator.cs`
- [ ] T005 Review `src/GameEngine/GUI/Layout/Container.cs` to understand content area calculation and existing Vector2D<float> usage in `_spacing` property

**Checkpoint**: Foundation verified - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Unified Alignment API (Priority: P1) 🎯 MVP

**Goal**: Replace separate single-axis alignment properties with unified Vector2D<float> Alignment property for both HorizontalLayout and VerticalLayout

**Independent Test**: Can verify by creating layout instances, setting Alignment property to various Vector2D values, and confirming property storage works correctly

### Tests for User Story 1 (Red Phase)

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T006 [P] [US1] Add failing test `Alignment_Property_AcceptsVector2D` in `src/Tests/GameEngine/GUI/Layout/HorizontalLayout.Tests.cs`
- [ ] T007 [P] [US1] Add failing test `Alignment_Property_AcceptsVector2D` in `src/Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs`
- [ ] T008 [P] [US1] Add failing test `Alignment_DefaultValue_IsMiddleCenter` in `src/Tests/GameEngine/GUI/Layout/HorizontalLayout.Tests.cs`
- [ ] T009 [P] [US1] Add failing test `Alignment_DefaultValue_IsMiddleCenter` in `src/Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs`
- [ ] T010 [P] [US1] Add failing test `SetAlignment_WithVector2D_UpdatesProperty` in `src/Tests/GameEngine/GUI/Layout/HorizontalLayout.Tests.cs`
- [ ] T011 [P] [US1] Add failing test `SetAlignment_WithVector2D_UpdatesProperty` in `src/Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs`

### Implementation for User Story 1 (Green Phase)

- [ ] T012 [P] [US1] Change `_alignment` field from `float` to `Vector2D<float>` with default `new(0, 0)` in `src/GameEngine/GUI/Layout/HorizontalLayout.cs`
- [ ] T013 [P] [US1] Change `_alignment` field from `float` to `Vector2D<float>` with default `new(0, 0)` in `src/GameEngine/GUI/Layout/VerticalLayout.cs`
- [ ] T014 [US1] Build solution to verify source generators handle Vector2D<float> correctly using `dotnet build Nexus.GameEngine.sln --configuration Debug`
- [ ] T015 [US1] Run tests to verify User Story 1 tests now pass using `dotnet test src/Tests/Tests.csproj --filter "FullyQualifiedName~HorizontalLayout.Tests&FullyQualifiedName~Alignment"`
- [ ] T016 [US1] Run tests to verify User Story 1 tests now pass using `dotnet test src/Tests/Tests.csproj --filter "FullyQualifiedName~VerticalLayout.Tests&FullyQualifiedName~Alignment"`

**Checkpoint**: At this point, the Alignment property API change is complete and testable independently. Layouts still use old positioning logic.

---

## Phase 4: User Story 2 - HorizontalLayout Child Positioning (Priority: P2)

**Goal**: Update HorizontalLayout to use Alignment.Y component for vertical child positioning with proper formula

**Independent Test**: Can verify by creating HorizontalLayout with children, setting various Alignment.Y values (-1, 0, 1), and confirming Y positions are calculated correctly

### Tests for User Story 2 (Red Phase)

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T017 [P] [US2] Update existing test `TopAlignment_PositionsCorrectly` to use `new Vector2D<float>(0, Align.Top)` in `src/Tests/GameEngine/GUI/Layout/HorizontalLayout.Tests.cs`
- [ ] T018 [P] [US2] Update existing test `SingleChild_CentersVertically` to use `new Vector2D<float>(0, Align.Middle)` in `src/Tests/GameEngine/GUI/Layout/HorizontalLayout.Tests.cs`
- [ ] T019 [P] [US2] Update existing test `BottomAlignment_PositionsCorrectly` to use `new Vector2D<float>(0, Align.Bottom)` in `src/Tests/GameEngine/GUI/Layout/HorizontalLayout.Tests.cs`
- [ ] T020 [P] [US2] Add new test `Alignment_CustomYValue_PositionsCorrectly` to verify formula with Alignment.Y = 0.5f in `src/Tests/GameEngine/GUI/Layout/HorizontalLayout.Tests.cs`
- [ ] T021 [P] [US2] Add new test `StretchChildren_IgnoresAlignmentY` to verify stretching overrides alignment in `src/Tests/GameEngine/GUI/Layout/HorizontalLayout.Tests.cs`
- [ ] T022 [US2] Run tests to verify they fail using `dotnet test src/Tests/Tests.csproj --filter "FullyQualifiedName~HorizontalLayout.Tests"`

### Implementation for User Story 2 (Green Phase)

- [ ] T023 [US2] Update `UpdateLayout()` method in `src/GameEngine/GUI/Layout/HorizontalLayout.cs` to replace switch statement with formula: `y = contentArea.Origin.Y + (int)((contentArea.Size.Y - childHeight) * ((Alignment.Y + 1) / 2))`
- [ ] T024 [US2] Update `UpdateLayout()` method in `src/GameEngine/GUI/Layout/HorizontalLayout.cs` to handle StretchChildren case: `var y = _stretchChildren ? contentArea.Origin.Y : /* alignment formula */`
- [ ] T025 [US2] Build solution using `dotnet build Nexus.GameEngine.sln --configuration Debug`
- [ ] T026 [US2] Run tests to verify User Story 2 tests now pass using `dotnet test src/Tests/Tests.csproj --filter "FullyQualifiedName~HorizontalLayout.Tests"`

**Checkpoint**: At this point, HorizontalLayout correctly positions children using the new Vector2D Alignment.Y component

---

## Phase 5: User Story 3 - VerticalLayout Child Positioning (Priority: P2)

**Goal**: Update VerticalLayout to use Alignment.X component for horizontal child positioning (already uses correct formula, just needs to use X component)

**Independent Test**: Can verify by creating VerticalLayout with children, setting various Alignment.X values (-1, 0, 1), and confirming X positions are calculated correctly

### Tests for User Story 3 (Red Phase)

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [ ] T027 [P] [US3] Add test `LeftAlignment_PositionsCorrectly` using `new Vector2D<float>(Align.Left, 0)` in `src/Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs`
- [ ] T028 [P] [US3] Add test `CenterAlignment_PositionsCorrectly` using `new Vector2D<float>(Align.Center, 0)` in `src/Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs`
- [ ] T029 [P] [US3] Add test `RightAlignment_PositionsCorrectly` using `new Vector2D<float>(Align.Right, 0)` in `src/Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs`
- [ ] T030 [P] [US3] Add test `Alignment_CustomXValue_PositionsCorrectly` to verify formula with Alignment.X = -0.5f in `src/Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs`
- [ ] T031 [P] [US3] Add test `StretchChildren_IgnoresAlignmentX` to verify stretching overrides alignment in `src/Tests/GameEngine/GUI/Layout/VerticalLayout.Tests.cs`
- [ ] T032 [US3] Run tests to verify they fail using `dotnet test src/Tests/Tests.csproj --filter "FullyQualifiedName~VerticalLayout.Tests"`

### Implementation for User Story 3 (Green Phase)

- [ ] T033 [US3] Update `UpdateLayout()` method in `src/GameEngine/GUI/Layout/VerticalLayout.cs` to use `Alignment.X` instead of `Alignment` in the existing formula
- [ ] T034 [US3] Update `UpdateLayout()` method in `src/GameEngine/GUI/Layout/VerticalLayout.cs` to handle StretchChildren case: `var x = _stretchChildren ? contentArea.Origin.X : /* alignment formula */`
- [ ] T035 [US3] Build solution using `dotnet build Nexus.GameEngine.sln --configuration Debug`
- [ ] T036 [US3] Run tests to verify User Story 3 tests now pass using `dotnet test src/Tests/Tests.csproj --filter "FullyQualifiedName~VerticalLayout.Tests"`

**Checkpoint**: At this point, VerticalLayout correctly positions children using the new Vector2D Alignment.X component. All core functionality is complete.

---

## Phase 6: User Story 4 - Migration Support (Priority: P3)

**Goal**: Update documentation and examples to reflect new API, deprecate old alignment helper classes

**Independent Test**: Can verify by reviewing documentation and ensuring migration examples are clear and complete

### Documentation Updates for User Story 4

- [ ] T037 [P] [US4] Add XML documentation comments to Alignment property in generated code showing Y component usage in `src/GameEngine/GUI/Layout/HorizontalLayout.cs`
- [ ] T038 [P] [US4] Add XML documentation comments to Alignment property in generated code showing X component usage in `src/GameEngine/GUI/Layout/VerticalLayout.cs`
- [ ] T039 [P] [US4] Add `[Obsolete]` attribute to `HorizontalAlignment` static class in `src/GameEngine/GUI/Align.cs` with migration message
- [ ] T040 [P] [US4] Add `[Obsolete]` attribute to `VerticalAlignment` static class in `src/GameEngine/GUI/Align.cs` with migration message
- [ ] T041 [P] [US4] Update TestApp examples (if any) to use new Vector2D alignment in `src/TestApp/` directory
- [ ] T042 [US4] Build solution to verify deprecation warnings appear correctly using `dotnet build Nexus.GameEngine.sln --configuration Debug`

**Checkpoint**: All user stories are now independently functional with proper documentation

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [ ] T043 [P] Update `specs/005-layout-alignment-refactor/quickstart.md` with any runtime insights from testing
- [ ] T044 [P] Run full test suite to ensure no regressions using `dotnet test src/Tests/Tests.csproj`
- [ ] T045 [P] Check code coverage for layout tests using `dotnet test src/Tests/Tests.csproj --collect:"XPlat Code Coverage"`
- [ ] T046 Build solution in Release mode to verify no warnings using `dotnet build Nexus.GameEngine.sln --configuration Release`
- [ ] T047 Run integration tests if any exist in TestApp using `dotnet run --project src/TestApp/TestApp.csproj`
- [ ] T048 Review and commit all changes with descriptive commit message
- [ ] T049 Update feature status in `specs/005-layout-alignment-refactor/plan.md` to "Complete"

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User Story 1 (P1): Must complete first (API change)
  - User Story 2 (P2): Depends on US1 (needs Vector2D Alignment to exist)
  - User Story 3 (P2): Depends on US1 (needs Vector2D Alignment to exist), PARALLEL to US2
  - User Story 4 (P3): Depends on US1-3 (documents completed implementation)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Depends on US1 completion (needs Vector2D Alignment property)
- **User Story 3 (P2)**: Depends on US1 completion (needs Vector2D Alignment property), can run in parallel with US2
- **User Story 4 (P3)**: Depends on US1-3 completion (documents complete implementation)

### Within Each User Story

- Tests MUST be written and FAIL before implementation (Red-Green-Refactor TDD)
- Property changes (US1) before layout algorithm changes (US2, US3)
- Implementation before documentation updates (US4)
- Build verification after code changes
- Test execution to validate changes

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel
- All Foundational tasks marked [P] can run in parallel (within Phase 2)
- Within US1: Both layout test files can be updated in parallel (T006-T011)
- Within US1: Both layout implementation files can be updated in parallel (T012-T013)
- Within US2: All test updates can be written in parallel (T017-T021)
- Within US3: All test additions can be written in parallel (T027-T031)
- US2 and US3 implementation can proceed in parallel after US1 is complete
- Within US4: All documentation updates can be done in parallel (T037-T042)
- All Polish tasks marked [P] can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all test updates for User Story 1 together:
Task: "Add failing test Alignment_Property_AcceptsVector2D in HorizontalLayout.Tests.cs"
Task: "Add failing test Alignment_Property_AcceptsVector2D in VerticalLayout.Tests.cs"
Task: "Add failing test Alignment_DefaultValue_IsMiddleCenter in HorizontalLayout.Tests.cs"
Task: "Add failing test Alignment_DefaultValue_IsMiddleCenter in VerticalLayout.Tests.cs"
Task: "Add failing test SetAlignment_WithVector2D_UpdatesProperty in HorizontalLayout.Tests.cs"
Task: "Add failing test SetAlignment_WithVector2D_UpdatesProperty in VerticalLayout.Tests.cs"

# Launch both implementation changes together (after tests fail):
Task: "Change _alignment field to Vector2D<float> in HorizontalLayout.cs"
Task: "Change _alignment field to Vector2D<float> in VerticalLayout.cs"
```

---

## Parallel Example: User Stories 2 & 3

```bash
# After User Story 1 is complete, these can proceed in parallel:

# Developer A works on User Story 2 (HorizontalLayout positioning):
Task: "Update TopAlignment_PositionsCorrectly test in HorizontalLayout.Tests.cs"
Task: "Update SingleChild_CentersVertically test in HorizontalLayout.Tests.cs"
# ... rest of US2 tasks

# Developer B works on User Story 3 (VerticalLayout positioning):
Task: "Add LeftAlignment_PositionsCorrectly test in VerticalLayout.Tests.cs"
Task: "Add CenterAlignment_PositionsCorrectly test in VerticalLayout.Tests.cs"
# ... rest of US3 tasks
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (API change to Vector2D)
4. **STOP and VALIDATE**: Test that Alignment property accepts Vector2D<float>
5. Layouts will compile but still use old positioning logic (acceptable for MVP API validation)

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test API independently → API change complete (MVP!)
3. Add User Story 2 → Test HorizontalLayout positioning → HorizontalLayout complete
4. Add User Story 3 → Test VerticalLayout positioning → VerticalLayout complete
5. Add User Story 4 → Documentation and deprecation → Migration support complete
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Team completes User Story 1 together (blocking for US2/US3)
3. Once US1 is done:
   - Developer A: User Story 2 (HorizontalLayout positioning)
   - Developer B: User Story 3 (VerticalLayout positioning)
4. Team completes User Story 4 together (documentation)
5. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies, can run in parallel
- [Story] label maps task to specific user story for traceability
- Each user story should be independently testable
- Follow TDD workflow: Red (failing tests) → Green (implementation) → Refactor (cleanup)
- Verify tests fail before implementing
- Build after each implementation change
- Run affected tests after each change
- Commit after each user story completion
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, breaking test independence
