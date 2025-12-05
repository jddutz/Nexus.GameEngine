---

description: "Task list for Performance Profiling and Optimization implementation"
---

# Tasks: Performance Profiling and Optimization

**Input**: Design documents from `/specs/012-performance/`
**Prerequisites**: plan.md, spec.md, research-timing.md

**Tests**: Unit tests are included per TDD workflow requirements. Integration tests use existing TestApp infrastructure.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/GameEngine/` for core engine, `src/TestApp/` for integration tests
- **Tests**: `src/Tests/GameEngine/` mirroring source structure
- **Docs**: `.docs/` for system documentation

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and documentation structure

- [X] T001 Verify clean build of Nexus.GameEngine.sln in Debug configuration
- [X] T002 [P] Create Performance subsystem directory structure at src/GameEngine/Performance/
- [X] T003 [P] Create unit test directory at src/Tests/GameEngine/Performance/
- [X] T004 [P] Create documentation file at .docs/Performance Profiling.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core profiling infrastructure that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T005 Create IProfiler interface contract in src/GameEngine/Performance/IProfiler.cs
- [X] T006 Create PerformanceSample value type in src/GameEngine/Performance/PerformanceSample.cs
- [X] T007 Create PerformanceScope ref struct for RAII-style measurement in src/GameEngine/Performance/PerformanceScope.cs
- [X] T008 Create unit tests for PerformanceScope in src/Tests/GameEngine/Performance/PerformanceScope.Tests.cs
- [X] T009 Create FrameProfile class for per-frame data aggregation in src/GameEngine/Performance/FrameProfile.cs
- [X] T010 Create unit tests for FrameProfile in src/Tests/GameEngine/Performance/FrameProfile.Tests.cs
- [X] T011 Implement Profiler service class in src/GameEngine/Performance/Profiler.cs
- [X] T012 Create unit tests for Profiler in src/Tests/GameEngine/Performance/Profiler.Tests.cs
- [X] T013 Register IProfiler service in Application startup (src/GameEngine/Components/Application.cs)
- [X] T014 Run unit tests for Performance subsystem
- [X] T015 Build solution and verify zero warnings/errors

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Identify Performance Bottlenecks (Priority: P1) 🎯 MVP

**Goal**: Enable developers to collect and analyze timing data for all major subsystems to identify bottlenecks

**Independent Test**: Run TestApp with profiling enabled and verify timing data is collected for rendering, updates, resource management, and input processing. Profiler can identify top 5 slowest operations.

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T016 [P] [US1] Create integration test for profiling activation in src/TestApp/Testing/ProfilingActivationTest.cs
- [X] T017 [P] [US1] Create integration test for subsystem timing collection in src/TestApp/Testing/SubsystemProfilingTest.cs
- [X] T018 [P] [US1] Create integration test for bottleneck identification in src/TestApp/Testing/BottleneckIdentificationTest.cs

### Implementation for User Story 1

- [X] T019 [P] [US1] Add profiling markers to IRenderer.Render() in src/GameEngine/Graphics/IRenderer.cs
- [X] T020 [P] [US1] Add profiling markers to IRuntimeComponent.Update() in src/GameEngine/Components/IRuntimeComponent.cs
- [X] T021 [P] [US1] Add profiling markers to IResourceManager resource loading in src/GameEngine/Resources/IResourceManager.cs
- [X] T022 [P] [US1] Add profiling markers to input processing in src/GameEngine/Input/InputManager.cs (SKIPPED - no InputManager exists)
- [X] T023 [US1] Create PerformanceReport class for aggregated analysis in src/GameEngine/Performance/PerformanceReport.cs
- [X] T024 [US1] Implement top N slowest operations identification in PerformanceReport
- [X] T025 [US1] Implement frame time threshold violation detection in PerformanceReport
- [X] T026 [US1] Create unit tests for PerformanceReport in src/Tests/GameEngine/Performance/PerformanceReport.Tests.cs
- [X] T027 [US1] Integrate profiling into Application.Run() main loop in src/GameEngine/Components/Application.cs
- [X] T028 [US1] Add profiling enable/disable runtime controls to Application (Satisfied by IProfiler.Enable/Disable)
- [X] T029 [US1] Run integration tests for User Story 1
- [X] T030 [US1] Build solution and address any warnings/errors
- [X] T031 [US1] Document profiling markers and usage in .docs/Performance Profiling.md

**Checkpoint**: At this point, User Story 1 should be fully functional - developers can collect timing data and identify bottlenecks

---

## Phase 4: User Story 2 - Monitor Frame Performance in Real-Time (Priority: P2)

**Goal**: Provide real-time performance visualization via on-screen overlay showing FPS, frame time, and subsystem breakdowns

**Note**: Rendering integration deferred - PerformanceMonitor component provides data via component properties for future data binding.

**Independent Test**: Run TestApp with performance overlay enabled and verify current FPS, frame time, and subsystem timings are displayed and update in real-time. Performance warnings appear when degradation occurs.

### Tests for User Story 2

- [ ] T032 [P] [US2] Create integration test for overlay rendering in src/TestApp/Testing/PerformanceOverlayRenderTest.cs (DEFERRED - awaiting data binding system)
- [ ] T033 [P] [US2] Create integration test for overlay data updates in src/TestApp/Testing/OverlayDataUpdateTest.cs (DEFERRED - awaiting data binding system)
- [ ] T034 [P] [US2] Create integration test for performance warnings in src/TestApp/Testing/PerformanceWarningTest.cs (DEFERRED - awaiting data binding system)

### Implementation for User Story 2

- [X] T035 [US2] Create PerformanceMonitor component in src/GameEngine/Performance/PerformanceMonitor.cs
- [X] T036 [US2] Implement IProfiler facade pattern with component properties for data exposure
- [X] T037 [US2] Add FPS calculation logic to PerformanceMonitor (rolling average over configurable frames)
- [X] T038 [US2] Add frame time display properties (current/average/min/max) to PerformanceMonitor
- [X] T039 [US2] Add subsystem timing breakdown properties to PerformanceMonitor
- [X] T040 [US2] Implement performance warning indicators (threshold-based) in PerformanceMonitor
- [X] T041 [US2] Create unit tests for PerformanceMonitor in src/Tests/GameEngine/Performance/PerformanceMonitor.Tests.cs
- [ ] T042 [US2] Integrate PerformanceMonitor with rendering system (DEFERRED - awaiting data binding to TextRenderer)
- [ ] T043 [US2] Add overlay toggle controls to TestApp in src/TestApp/Program.cs (DEFERRED)
- [ ] T044 [US2] Run integration tests for User Story 2 (DEFERRED)
- [X] T045 [US2] Build solution and address any warnings/errors
- [X] T046 [US2] Document PerformanceMonitor in .docs/Performance Profiling.md

**Checkpoint**: PerformanceMonitor component complete with data collection and exposure via component properties. Rendering integration deferred pending data binding system implementation.

---

## Phase 5: User Story 3 - Optimize Identified Bottlenecks (Priority: P3)

**Goal**: Apply targeted optimizations to restore TestApp performance to 150 FPS baseline

**Independent Test**: Run TestApp with optimizations applied and verify FPS reaches or exceeds 150 FPS with minimal scene complexity. Frame time remains consistently under 6.67ms target.

### Tests for User Story 3

- [ ] T047 [P] [US3] Create performance regression test in src/TestApp/Testing/PerformanceRegressionTest.cs
- [ ] T048 [P] [US3] Create frame time consistency test in src/TestApp/Testing/FrameTimeConsistencyTest.cs

### Implementation for User Story 3

- [ ] T049 [US3] Run profiling session on TestApp and capture baseline PerformanceReport
- [ ] T050 [US3] Analyze PerformanceReport to identify top 3 bottlenecks
- [ ] T051 [US3] Document identified bottlenecks in specs/012-performance/optimization-targets.md
- [ ] T052 [US3] Apply optimization #1 based on profiling data (specific file TBD after T050)
- [ ] T053 [US3] Verify optimization #1 impact via PerformanceReport comparison
- [ ] T054 [US3] Apply optimization #2 based on profiling data (specific file TBD after T050)
- [ ] T055 [US3] Verify optimization #2 impact via PerformanceReport comparison
- [ ] T056 [US3] Apply optimization #3 based on profiling data (specific file TBD after T050)
- [ ] T057 [US3] Verify optimization #3 impact via PerformanceReport comparison
- [ ] T058 [US3] Run comprehensive performance regression tests
- [ ] T059 [US3] Validate 150 FPS target achievement with minimal scene
- [ ] T060 [US3] Validate frame time variance reduction (90% target)
- [ ] T061 [US3] Build solution and verify all tests pass
- [ ] T062 [US3] Document applied optimizations in .docs/Performance Profiling.md

**Checkpoint**: All user stories should now be independently functional - performance is restored to target 150 FPS

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and final validation

- [ ] T063 [P] Add profiling overhead validation test in src/Tests/GameEngine/Performance/ProfilingOverhead.Tests.cs
- [ ] T064 [P] Verify profiling overhead is <5% of frame time per FR-008 requirement
- [ ] T065 [P] Add persistent profiling data export functionality to Profiler.cs
- [ ] T066 [P] Add profiling data comparison utilities to PerformanceReport.cs
- [ ] T067 Code cleanup and refactoring for Performance subsystem
- [ ] T068 Final unit test coverage verification (target: 80% per project guidelines)
- [ ] T069 Run all integration tests in src/TestApp/Testing/
- [ ] T070 Final documentation review and updates in .docs/Performance Profiling.md
- [ ] T071 Update src/GameEngine/README.md with profiling system overview
- [ ] T072 Build solution in Release configuration and validate performance
- [ ] T073 Run TestApp manual validation per quickstart.md scenarios (when created)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - User stories can proceed in parallel (if staffed) OR sequentially in priority order (P1 → P2 → P3)
  - US2 (Overlay) benefits from US1 (Profiling) but is independently testable
  - US3 (Optimization) requires US1 (Profiling) to identify bottlenecks, but US2 (Overlay) is optional
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Uses profiling data from US1 but independently testable
- **User Story 3 (P3)**: Requires US1 profiling infrastructure to identify optimization targets - US2 overlay helpful for validation but optional

### Within Each User Story

- Tests MUST be written and FAIL before implementation (Red phase)
- Models/types before services
- Services before integration
- Unit tests run after implementation (Green phase)
- Integration tests validate story completion
- Build and address warnings/errors before checkpoint

### Parallel Opportunities

#### Phase 1 (Setup)
- All Setup tasks (T002, T003, T004) can run in parallel

#### Phase 2 (Foundational)
- After interfaces created: PerformanceSample (T006), PerformanceScope (T007), FrameProfile (T009) can be developed in parallel
- After implementations: All unit tests (T008, T010, T012) can run in parallel

#### Phase 3 (User Story 1)
- All integration tests (T016, T017, T018) can be written in parallel
- All profiling marker additions (T019, T020, T021, T022) can be done in parallel (different files)

#### Phase 4 (User Story 2)
- All integration tests (T032, T033, T034) can be written in parallel

#### Phase 5 (User Story 3)
- Integration tests (T047, T048) can be written in parallel
- Optimizations (T052, T054, T056) are sequential - each must be verified before next

#### Phase 6 (Polish)
- Overhead validation (T063, T064), export functionality (T065), comparison utilities (T066) can be developed in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together (Red phase):
Task T016: "Create integration test for profiling activation in src/TestApp/Testing/ProfilingActivationTest.cs"
Task T017: "Create integration test for subsystem timing collection in src/TestApp/Testing/SubsystemProfilingTest.cs"
Task T018: "Create integration test for bottleneck identification in src/TestApp/Testing/BottleneckIdentificationTest.cs"

# Verify tests fail (Red phase confirmed)

# Launch all profiling marker additions together (Green phase):
Task T019: "Add profiling markers to IRenderer.Render() in src/GameEngine/Graphics/IRenderer.cs"
Task T020: "Add profiling markers to IRuntimeComponent.Update() in src/GameEngine/Components/IRuntimeComponent.cs"
Task T021: "Add profiling markers to IResourceManager resource loading in src/GameEngine/Resources/IResourceManager.cs"
Task T022: "Add profiling markers to input processing in src/GameEngine/Input/InputManager.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (4 tasks)
2. Complete Phase 2: Foundational (11 tasks) - CRITICAL - blocks all stories
3. Complete Phase 3: User Story 1 (16 tasks)
4. **STOP and VALIDATE**: Test User Story 1 independently - can developers identify bottlenecks?
5. If validated, this is a functional MVP - profiling infrastructure is operational

### Incremental Delivery

1. Complete Setup + Foundational → Profiling infrastructure ready (15 tasks)
2. Add User Story 1 → Profiling data collection and analysis → Test independently → **MVP Deployed** (31 tasks total)
3. Add User Story 2 → Real-time overlay visualization → Test independently → Enhanced profiling deployed (46 tasks total)
4. Add User Story 3 → Apply optimizations → Validate 150 FPS target → Performance restored (62 tasks total)
5. Polish (Phase 6) → Production-ready profiling system (73 tasks total)

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together (Phase 1 + 2)
2. Once Foundational is done:
   - **Developer A**: User Story 1 (Profiling Infrastructure)
   - **Developer B**: User Story 2 (Overlay - can start in parallel, integrates with US1 when ready)
   - **Developer C**: Documentation and Polish tasks
3. Once US1 complete:
   - **Developer A**: User Story 3 (Optimization - requires US1 profiling data)
   - **Developer B**: Continue US2 or assist with US3 validation
   - **Developer C**: Polish and final testing

---

## Success Metrics

### User Story 1 Success Criteria
- [ ] Profiling can be enabled/disabled at runtime
- [ ] Timing data collected for rendering, updates, resource loading, input processing
- [ ] PerformanceReport identifies top 5 slowest operations
- [ ] Frame time threshold violations detected
- [ ] Unit tests achieve 80% code coverage for Performance subsystem

### User Story 2 Success Criteria
- [ ] Performance overlay renders without crashing
- [ ] FPS displays and updates at least once per second
- [ ] Frame time shows current/average/min/max values
- [ ] Subsystem timing breakdown visible
- [ ] Performance warnings appear when frame time exceeds threshold
- [ ] Overlay can be toggled on/off at runtime

### User Story 3 Success Criteria
- [ ] TestApp achieves sustained 150 FPS (±5%) with minimal scene
- [ ] Frame time consistently below 6.67ms target
- [ ] Frame time variance reduced by 90%
- [ ] All optimizations documented
- [ ] No functionality regressions introduced
- [ ] All unit and integration tests pass

### Overall Success Criteria (FR Requirements)
- [ ] FR-001: Per-frame timing data collected for all major subsystems
- [ ] FR-002: FPS and frame time calculated and displayed
- [ ] FR-003: Threshold violations identified
- [ ] FR-004: Multi-frame aggregation with averages/min/max
- [ ] FR-005: Runtime enable/disable without restart
- [ ] FR-006: On-screen overlay functional
- [ ] FR-007: Custom profiling markers supported
- [ ] FR-008: Profiling overhead <5% of frame time
- [ ] FR-009: Profiling data persistence (Phase 6)
- [ ] FR-010: Baseline comparison capabilities (Phase 6)
- [ ] FR-011: Performance regression detection
- [ ] FR-012: Optimizations preserve functionality

---

## Notes

- [P] tasks = different files, no dependencies within phase
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- **TDD workflow**: Write tests first (Red), implement (Green), refactor, build, verify
- Verify tests fail before implementing (Red phase critical for TDD)
- Commit after each task or logical group
- Stop at each checkpoint to validate story independently
- Use `.temp/agent/` for temporary work files during optimization analysis
- DO NOT change code without explicit approval - present options first
- User Story 3 task specifics (T052, T054, T056) depend on profiling results from T050

---

## Total Task Count

- **Setup**: 4 tasks
- **Foundational**: 11 tasks
- **User Story 1**: 16 tasks
- **User Story 2**: 15 tasks
- **User Story 3**: 16 tasks
- **Polish**: 11 tasks
- **Grand Total**: 73 tasks

### Task Count by User Story
- **US1 (Identify Bottlenecks)**: 16 tasks
- **US2 (Real-Time Monitoring)**: 15 tasks
- **US3 (Optimization)**: 16 tasks

### Parallel Opportunities Identified
- **Setup**: 3 parallel tasks (T002, T003, T004)
- **Foundational**: 6 parallel opportunities (types, tests)
- **US1**: 7 parallel opportunities (tests, markers)
- **US2**: 3 parallel opportunities (tests)
- **US3**: 2 parallel opportunities (tests)
- **Polish**: 4 parallel opportunities (validation, export, comparison)
- **Total Parallel Opportunities**: ~25 tasks can be executed in parallel at various stages

### Suggested MVP Scope
**Minimum Viable Product (MVP) = Phase 1 + Phase 2 + Phase 3 (User Story 1)**
- Total: 31 tasks
- Delivers: Functional profiling infrastructure with bottleneck identification
- Value: Developers can collect timing data and identify performance issues
- Testable: Integration tests verify profiling works end-to-end
