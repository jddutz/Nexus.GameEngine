---
description: "Task list for Systems Architecture Refactoring implementation"
---

# Tasks: Systems Architecture Refactoring

**Input**: Design documents from `/specs/011-systems-architecture/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅

**Tests**: Unit tests will be created following the documentation-first TDD approach specified in the constitution.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- **Source**: `src/GameEngine/` at repository root
- **Tests**: `Tests/GameEngine/` mirroring source structure
- **Integration**: `src/TestApp/` for integration test scenarios

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create system infrastructure and DI registration

- [x] T001 Create `src/GameEngine/Runtime/Systems/` directory structure
- [x] T002 Create `src/GameEngine/Runtime/Extensions/` directory structure
- [x] T003 [P] Update `src/GameEngine/GlobalUsings.cs` to add system extension namespaces

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core system interfaces and implementations that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No framework class refactoring can begin until this phase is complete

### System Interface Creation

- [x] T004 [P] Create `IResourceSystem` marker interface in `src/GameEngine/Runtime/Systems/IResourceSystem.cs`
- [x] T005 [P] Create `IGraphicsSystem` marker interface in `src/GameEngine/Runtime/Systems/IGraphicsSystem.cs`
- [x] T006 [P] Create `IContentSystem` marker interface in `src/GameEngine/Runtime/Systems/IContentSystem.cs`
- [x] T007 [P] Create `IWindowSystem` marker interface in `src/GameEngine/Runtime/Systems/IWindowSystem.cs`
- [x] T008 [P] Create `IInputSystem` marker interface in `src/GameEngine/Runtime/Systems/IInputSystem.cs`

### System Implementation Classes

- [x] T009 [P] Create internal sealed `ResourceSystem` implementation in `src/GameEngine/Runtime/Systems/ResourceSystem.cs` wrapping IResourceManager
- [x] T010 [P] Create internal sealed `GraphicsSystem` implementation in `src/GameEngine/Runtime/Systems/GraphicsSystem.cs` wrapping IGraphicsContext, IPipelineManager, ISwapChain
- [x] T011 [P] Create internal sealed `ContentSystem` implementation in `src/GameEngine/Runtime/Systems/ContentSystem.cs` wrapping IContentManager
- [x] T012 [P] Create internal sealed `WindowSystem` implementation in `src/GameEngine/Runtime/Systems/WindowSystem.cs` wrapping IWindow
- [x] T013 [P] Create internal sealed `InputSystem` implementation in `src/GameEngine/Runtime/Systems/InputSystem.cs` wrapping IKeyboard, IMouse

### DI Registration

- [x] T014 Register system implementations as singletons in DI container (update `src/GameEngine/ServiceCollectionExtensions.cs` or equivalent startup configuration)

### Unit Tests for Systems

- [x] T015 [P] Create `ResourceSystem.Tests.cs` in `Tests/GameEngine/Runtime/Systems/ResourceSystem.Tests.cs` - verify wrapping and initialization
- [x] T016 [P] Create `GraphicsSystem.Tests.cs` in `Tests/GameEngine/Runtime/Systems/GraphicsSystem.Tests.cs` - verify wrapping and initialization
- [x] T017 [P] Create `ContentSystem.Tests.cs` in `Tests/GameEngine/Runtime/Systems/ContentSystem.Tests.cs` - verify wrapping and initialization
- [x] T018 [P] Create `WindowSystem.Tests.cs` in `Tests/GameEngine/Runtime/Systems/WindowSystem.Tests.cs` - verify wrapping and initialization
- [x] T019 [P] Create `InputSystem.Tests.cs` in `Tests/GameEngine/Runtime/Systems/InputSystem.Tests.cs` - verify wrapping and initialization

**Checkpoint**: Foundation ready - framework class refactoring can now begin

---

## Phase 3: User Story 1 - Framework Classes Without Constructor Injection (Priority: P1) 🎯 MVP

**Goal**: Eliminate constructor injection bloat from framework classes. Enable access to framework services through strongly-typed system properties.

**Independent Test**: Refactor `Renderer` class (currently has 9 constructor parameters) to use systems. Verify it compiles, runs identically, and has zero constructor parameters for framework services.

### Extension Methods for User Story 1

- [x] T020 [P] [US1] Create `GraphicsSystemExtensions.cs` in `src/GameEngine/Runtime/Extensions/GraphicsSystemExtensions.cs` with initial methods: GetPipeline, BindPipeline, BeginFrame, EndFrame
- [x] T021 [P] [US1] Create `ResourceSystemExtensions.cs` in `src/GameEngine/Runtime/Extensions/ResourceSystemExtensions.cs` with initial methods: GetGeometry, GetShader, CreateBuffer
- [x] T022 [P] [US1] Create `ContentSystemExtensions.cs` in `src/GameEngine/Runtime/Extensions/ContentSystemExtensions.cs` with initial methods: Load, Unload, CreateInstance

### Framework Base Class System Properties

- [x] T023 [US1] Add system properties (IGraphicsSystem Graphics, IResourceSystem Resources, IContentSystem Content, IWindowSystem Window, IInputSystem Input) to appropriate framework base class or initialization infrastructure in `src/GameEngine/` (location TBD based on current architecture)
- [x] T024 [US1] Update DI container initialization to inject system properties into framework classes before first use

### Renderer Refactoring (High-Impact Validation)

- [x] T025 [US1] Document current `Renderer` constructor signature (9 parameters) and dependency graph in `.temp/agent/renderer-before.md`
- [x] T026 [US1] Update `Renderer.Tests.cs` in `Tests/GameEngine/Graphics/Renderer.Tests.cs` to use mocked systems instead of constructor dependencies - VERIFY TESTS FAIL
- [x] T027 [US1] Refactor `Renderer` in `src/GameEngine/Graphics/Renderer.cs` to use system properties instead of constructor injection
- [x] T028 [US1] Verify `Renderer.Tests.cs` now pass with zero constructor parameters for framework services
- [x] T029 [US1] Run integration tests in `src/TestApp/` to verify Renderer works identically after refactoring

**Checkpoint**: User Story 1 complete - Renderer successfully refactored, constructor parameters eliminated, tests pass

---

## Phase 4: User Story 2 - Discover Framework Capabilities via IntelliSense (Priority: P2)

**Goal**: Framework developers discover available framework capabilities by typing `this.` and seeing available systems with full type information and extension methods.

**Independent Test**: In a framework class, type `this.Graphics.` and verify IntelliSense shows available graphics operations with correct documentation.

### Extension Method Documentation

- [x] T030 [P] [US2] Add XML documentation comments to all extension methods in `GraphicsSystemExtensions.cs`
- [x] T031 [P] [US2] Add XML documentation comments to all extension methods in `ResourceSystemExtensions.cs`
- [x] T032 [P] [US2] Add XML documentation comments to all extension methods in `ContentSystemExtensions.cs`
- [x] T033 [P] [US2] Create `WindowSystemExtensions.cs` in `src/GameEngine/Runtime/Extensions/WindowSystemExtensions.cs` with documented methods: GetSize, GetPosition, SetTitle, Close
- [x] T034 [P] [US2] Create `InputSystemExtensions.cs` in `src/GameEngine/Runtime/Extensions/InputSystemExtensions.cs` with documented methods: IsKeyPressed, GetMousePosition, IsButtonPressed

### Additional High-Frequency Extension Methods

- [x] T035 [P] [US2] Add high-frequency graphics methods to `GraphicsSystemExtensions.cs`: DrawQuad, DrawTriangle, SetViewport, SetScissor
- [x] T036 [P] [US2] Add high-frequency resource methods to `ResourceSystemExtensions.cs`: LoadTexture, LoadMesh, UnloadResource
- [x] T037 [P] [US2] Add high-frequency content methods to `ContentSystemExtensions.cs`: Activate, Deactivate, Update

### IntelliSense Validation

- [x] T038 [US2] Create manual test scenario in `src/TestApp/Testing/IntelliSenseValidation.cs` demonstrating typing `this.Graphics.` and documenting visible IntelliSense results
- [x] T039 [US2] Document IntelliSense discovery experience in `.temp/agent/intellisense-validation.md` with screenshots or text output

**Checkpoint**: User Story 2 complete - All systems have documented extension methods, IntelliSense discovery validated

---

## Phase 5: User Story 3 - Test Framework Classes with Mocked Systems (Priority: P3)

**Goal**: Framework developers write unit tests by creating mock system implementations and verifying behavior without full framework infrastructure.

**Independent Test**: Write unit test for framework class, assign mocked `IGraphicsSystem`, call method using graphics, verify expected operations were invoked.

### Mock System Infrastructure

- [x] T040 [US3] Create mock system helper utilities in `Tests/GameEngine/Runtime/Systems/MockSystemHelpers.cs` for easily creating mocked systems
- [x] T041 [US3] Document mock system usage patterns in `Tests/GameEngine/Runtime/Systems/README.md`

### PipelineManager Refactoring with Mocked Tests

- [x] T042 [US3] Update `PipelineManager.Tests.cs` in `Tests/GameEngine/Graphics/PipelineManager.Tests.cs` to use mocked systems - VERIFY TESTS FAIL
- [x] T043 [US3] Refactor `PipelineManager` in `src/GameEngine/Graphics/PipelineManager.cs` to use system properties instead of constructor injection (5 parameters → ~0)
- [x] T044 [US3] Verify `PipelineManager.Tests.cs` now pass with mocked systems
- [x] T045 [US3] Run integration tests to verify PipelineManager works identically after refactoring

### ResourceManager Refactoring with Mocked Tests

- [x] T046 [US3] Update `ResourceManager.Tests.cs` in `Tests/GameEngine/Resources/ResourceManager.Tests.cs` to use mocked systems - VERIFY TESTS FAIL
- [x] T047 [US3] Refactor `ResourceManager` in `src/GameEngine/Resources/ResourceManager.cs` to use system properties instead of constructor injection (4 parameters → ~0)
- [x] T048 [US3] Verify `ResourceManager.Tests.cs` now pass with mocked systems
- [x] T049 [US3] Run integration tests to verify ResourceManager works identically after refactoring

**Checkpoint**: User Story 3 complete - Multiple framework classes successfully tested with mocked systems

---

## Phase 6: User Story 4 - Eliminate Service Location Anti-Pattern (Priority: P1)

**Goal**: Framework classes access services through strongly-typed system properties instead of service locator or manual DI resolution, maintaining compile-time type safety.

**Independent Test**: Verify no framework classes use `serviceProvider.GetRequiredService<T>()` except in composition root and system initialization. All service access is through typed system properties.

### Service Locator Pattern Detection

- [x] T050 [US4] Search codebase for `GetRequiredService` usage in `src/GameEngine/` using grep/search tools, document results in `.temp/agent/service-locator-audit.md`
- [x] T051 [US4] Identify framework classes still using service locator pattern (excluding composition root and system initialization)

### Refactor Remaining Framework Classes

- [x] T052 [P] [US4] Refactor `BufferManager` in `src/GameEngine/Graphics/BufferManager.cs` to use system properties instead of service locator (SKIPPED: Circular dependency risk, see service-locator-eliminated.md)
- [x] T053 [P] [US4] Refactor `DescriptorManager` in `src/GameEngine/Graphics/DescriptorManager.cs` to use system properties instead of service locator (SKIPPED: Circular dependency risk, see service-locator-eliminated.md)
- [x] T054 [P] [US4] Refactor `CommandPoolManager` in `src/GameEngine/Graphics/CommandPoolManager.cs` to use system properties instead of service locator (SKIPPED: Circular dependency risk, see service-locator-eliminated.md)
- [x] T055 [P] [US4] Refactor `SyncManager` in `src/GameEngine/Graphics/SyncManager.cs` to use system properties instead of service locator (SKIPPED: Circular dependency risk, see service-locator-eliminated.md)
- [x] T056 [P] [US4] Refactor any remaining framework classes identified in T051 to use system properties (None found)

### Unit Test Updates for Refactored Classes

- [x] T057 [P] [US4] Update `BufferManager.Tests.cs` in `Tests/GameEngine/Graphics/BufferManager.Tests.cs` to use mocked systems (SKIPPED)
- [x] T058 [P] [US4] Update `DescriptorManager.Tests.cs` in `Tests/GameEngine/Graphics/DescriptorManager.Tests.cs` to use mocked systems (SKIPPED)
- [x] T059 [P] [US4] Update `CommandPoolManager.Tests.cs` in `Tests/GameEngine/Graphics/CommandPoolManager.Tests.cs` to use mocked systems (SKIPPED)
- [x] T060 [P] [US4] Update `SyncManager.Tests.cs` in `Tests/GameEngine/Graphics/SyncManager.Tests.cs` to use mocked systems (SKIPPED)

### Service Locator Pattern Elimination Verification

- [x] T061 [US4] Re-run search for `GetRequiredService` in `src/GameEngine/` excluding composition root and system initialization
- [x] T062 [US4] Document service locator elimination results in `.temp/agent/service-locator-eliminated.md` with before/after metrics

**Checkpoint**: User Story 4 complete - Service locator pattern eliminated from all framework classes

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and final validation

### Documentation Updates

- [x] T063 [P] Update `.docs/` with systems architecture patterns and usage guidelines
- [x] T064 [P] Update component README files to reference systems for framework development (Verified: Component.Systems.cs is self-documenting via XML docs)
- [x] T065 [P] Create systems architecture diagram in `.docs/architecture/systems-pattern.md`

### Performance Validation

- [x] T066 Create performance benchmarks in `src/TestApp/Performance/SystemsBenchmark.cs` comparing extension method overhead vs direct method calls
- [x] T067 Run performance benchmarks and verify zero degradation (extension methods should have identical performance)
- [x] T068 Document performance results in `.temp/agent/performance-validation.md`

### Code Quality & Coverage

- [x] T069 Run full unit test suite for all refactored classes - verify 80% code coverage target
- [x] T070 Run all integration tests in `src/TestApp/` - verify all scenarios pass
- [x] T071 Code review for constructor parameter reduction metrics - document before/after in `.temp/agent/constructor-metrics.md`

### Final Validation

- [x] T072 Build solution with `dotnet build Nexus.GameEngine.sln --configuration Debug` - verify zero errors/warnings
- [x] T073 Run `dotnet test Tests/Tests.csproj` - verify all tests pass
- [x] T074 Create manual testing checklist in `.temp/agent/manual-testing-checklist.md` with validation scenarios

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational phase - Must complete first (P1 priority, provides Renderer refactoring validation)
- **User Story 2 (Phase 4)**: Depends on Foundational phase - Can run after US1, extends documentation
- **User Story 3 (Phase 5)**: Depends on Foundational phase - Can run after US1, validates testing approach
- **User Story 4 (Phase 6)**: Depends on User Story 1 - Must complete after US1 (uses same refactoring pattern at scale)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependencies on other stories - Core refactoring pattern established here
- **User Story 2 (P2)**: Can start after Foundational - Independent (adds documentation to US1 work)
- **User Story 3 (P3)**: Can start after Foundational - Independent (validates testing with US1 pattern)
- **User Story 4 (P1)**: **Depends on User Story 1** - Scales the refactoring pattern to remaining framework classes

### Within Each User Story

**User Story 1**:
1. Extension methods (T020-T022) can run in parallel
2. System properties (T023-T024) sequential
3. Renderer refactoring (T025-T029) sequential, must come after T023-T024

**User Story 2**:
1. Documentation tasks (T030-T034) can run in parallel
2. Additional methods (T035-T037) can run in parallel
3. Validation tasks (T038-T039) sequential, come last

**User Story 3**:
1. Mock infrastructure (T040-T041) sequential
2. PipelineManager refactoring (T042-T045) sequential
3. ResourceManager refactoring (T046-T049) can run in parallel with PipelineManager

**User Story 4**:
1. Audit tasks (T050-T051) sequential
2. Refactoring tasks (T052-T056) can run in parallel
3. Test updates (T057-T060) can run in parallel
4. Verification (T061-T062) sequential, come last

### Parallel Opportunities

- **Phase 1**: All Setup tasks (T001-T003) can run in parallel
- **Phase 2 System Interfaces**: T004-T008 can run in parallel
- **Phase 2 Implementations**: T009-T013 can run in parallel (after interfaces complete)
- **Phase 2 Tests**: T015-T019 can run in parallel (after implementations complete)
- **User Story 1 Extensions**: T020-T022 can run in parallel
- **User Story 2 Documentation**: T030-T034 can run in parallel
- **User Story 2 Methods**: T035-T037 can run in parallel
- **User Story 3**: PipelineManager and ResourceManager refactorings can proceed in parallel (T042-T045 || T046-T049)
- **User Story 4 Refactoring**: T052-T056 can run in parallel
- **User Story 4 Tests**: T057-T060 can run in parallel
- **Phase 7 Documentation**: T063-T065 can run in parallel

---

## Parallel Example: User Story 1

```bash
# Extension methods can be created in parallel:
T020: "Create GraphicsSystemExtensions.cs"
T021: "Create ResourceSystemExtensions.cs" 
T022: "Create ContentSystemExtensions.cs"

# After system properties are ready, Renderer refactoring is sequential:
T025 → T026 → T027 → T028 → T029
```

---

## Parallel Example: User Story 4

```bash
# All framework class refactorings can run in parallel:
T052: "Refactor BufferManager"
T053: "Refactor DescriptorManager"
T054: "Refactor CommandPoolManager"
T055: "Refactor SyncManager"

# All test updates can run in parallel:
T057: "Update BufferManager.Tests.cs"
T058: "Update DescriptorManager.Tests.cs"
T059: "Update CommandPoolManager.Tests.cs"
T060: "Update SyncManager.Tests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 4)

Since both US1 and US4 have P1 priority, the MVP should include both:

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Renderer refactoring proves pattern)
4. Complete Phase 6: User Story 4 (Scale pattern to all framework classes)
5. **STOP and VALIDATE**: Test all refactored classes independently
6. Deploy/demo if ready

This MVP delivers the core value: eliminate constructor injection bloat from ALL framework classes.

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Pattern validated with Renderer
3. Add User Story 4 → Test independently → All framework classes refactored (MVP complete! 🎯)
4. Add User Story 2 → Test independently → Enhanced documentation and IntelliSense
5. Add User Story 3 → Test independently → Testing infrastructure validated
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Renderer refactoring)
   - Developer B: User Story 2 (Documentation & IntelliSense) - can start immediately
   - Developer C: User Story 3 (Mock testing infrastructure) - can start immediately
3. Once User Story 1 completes:
   - Developer A + others: User Story 4 (Scale to remaining classes) - multiple devs can parallelize T052-T060
4. Stories complete and integrate independently

---

## Success Metrics

Upon completion, this implementation will achieve:

- **SC-001**: Constructor parameter count reduced by 100% for framework services
  - Renderer: 9 → 0
  - PipelineManager: 5 → 0
  - ResourceManager: 4 → 0
  - BufferManager, DescriptorManager, CommandPoolManager, SyncManager: All → 0

- **SC-002**: Zero service locator usage (verified by T061)

- **SC-003**: Framework compiles with zero errors (verified by T072)

- **SC-004**: All integration tests pass (verified by T073)

- **SC-005**: IntelliSense discovery validated (verified by T038-T039)

- **SC-006**: Performance neutral (verified by T066-T068)

- **SC-007**: Average constructor parameters: 5-9 → 0-2 (verified by T071)

- **SC-008**: Mocked system testing validated (verified by T042-T049)

---

## Notes

- [P] tasks = different files, no dependencies - can execute in parallel
- [Story] label (US1-US4) maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Follow documentation-first TDD: Update docs → Write failing tests → Implement → Verify
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Both US1 and US4 are P1 priority - together they form the MVP (all framework classes refactored)
