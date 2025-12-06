# Tasks: Property Binding System

**Input**: Design documents from `/specs/013-property-binding/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Unit tests included per TDD workflow specified in copilot-instructions.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

All paths relative to repository root `C:\Users\jddut\Source\Github\Nexus.GameEngine\`:
- Source code: `src/GameEngine/`
- Source generators: `src/SourceGenerators/`
- Unit tests: `src/Tests/GameEngine/`
- Integration tests: `src/IntegrationTests/`
- Test app: `src/TestApp/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, documentation updates, and basic validation

- [x] T001 Verify clean build of Nexus.GameEngine.sln with configuration Debug
- [x] T002 [P] Update `.docs/Deferred Property Generation System.md` with property binding system architecture
- [x] T003 [P] Update `.github/copilot-instructions.md` with PropertyBinding usage patterns and lifecycle integration

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Create `PropertyChangedEventArgs.cs` with generic event args in `src/GameEngine/Events/PropertyChangedEventArgs.cs`
- [x] T005 [P] Create `ILookupStrategy.cs` interface in `src/GameEngine/Components/Lookups/ILookupStrategy.cs`
- [x] T006 [P] Create `IValueConverter.cs` interface in `src/GameEngine/Data/IValueConverter.cs`
- [x] T007 [P] Create `IBidirectionalConverter.cs` interface in `src/GameEngine/Data/IBidirectionalConverter.cs`
- [x] T008 [P] Create `BindingMode.cs` enum in `src/GameEngine/Components/BindingMode.cs`
- [x] T009 Create `PropertyBindings.cs` base class in `src/GameEngine/Components/PropertyBindings.cs`
- [x] T010 Create `PropertyBinding.cs` core class with fluent API in `src/GameEngine/Components/PropertyBinding.cs`

**Checkpoint**: Foundation ready - source generator and user story implementation can now begin in parallel

---

## Phase 3: User Story 6 - Source Generator Integration (Priority: P1) 🎯 MVP Infrastructure

**Goal**: Automatically generate PropertyBindings configuration classes and PropertyChanged events for components

**Independent Test**: Compiling a component with `[ComponentProperty]` attributes generates a corresponding `{ComponentName}PropertyBindings` class and `{PropertyName}Changed` events

### Unit Tests for User Story 6

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T011 [P] [US6] Create unit tests for ComponentPropertyGenerator modifications in `src/Tests/SourceGenerators/ComponentPropertyGenerator.Tests.cs`
- [x] T012 [P] [US6] Create unit tests for TemplateGenerator modifications in `src/Tests/SourceGenerators/TemplateGenerator.Tests.cs`
- [ ] T013 [P] [US6] Create unit tests for PropertyBindingsGenerator in `src/Tests/SourceGenerators/PropertyBindingsGenerator.Tests.cs`

### Implementation for User Story 6

- [x] T014 [US6] Modify `ComponentPropertyGenerator.cs` to add `NotifyChange` parameter support in `src/SourceGenerators/ComponentPropertyGenerator.cs`
- [x] T015 [US6] Generate `{PropertyName}Changed` events for properties with `NotifyChange = true` in `src/SourceGenerators/ComponentPropertyGenerator.cs`
- [x] T016 [US6] Generate `partial void On{PropertyName}Changed(T oldValue)` methods in `src/SourceGenerators/ComponentPropertyGenerator.cs`
- [x] T017 [US6] Modify `TemplateGenerator.cs` to add `Bindings` property to all generated templates in `src/SourceGenerators/TemplateGenerator.cs`
- [x] T018 [US6] Create `PropertyBindingsGenerator.cs` to generate `{ComponentName}PropertyBindings` classes in `src/SourceGenerators/PropertyBindingsGenerator.cs`
- [x] T019 [US6] Implement `IEnumerable<(string, PropertyBinding)>` in generated PropertyBindings classes in `src/SourceGenerators/PropertyBindingsGenerator.cs`
- [x] T020 [US6] Run unit tests and verify all source generator tests pass

**Checkpoint**: Source generators now create PropertyBindings classes and PropertyChanged events

---

## Phase 4: User Story 1 - Basic Parent-to-Child Property Binding (Priority: P1) 🎯 MVP Core

**Goal**: Enable basic parent-to-child bindings with FromParent lookup strategy

**Independent Test**: Create a parent with a `[ComponentProperty]` health value and a child HealthBar with bound `CurrentHealth` - changing parent health automatically updates child

### Unit Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T021 [P] [US1] Create unit tests for ParentLookup strategy in `src/Tests/GameEngine/Components/Lookups/ParentLookup.Tests.cs`
- [x] T022 [P] [US1] Create unit tests for PropertyBinding.Activate() in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`
- [x] T023 [P] [US1] Create unit tests for PropertyBinding.Deactivate() in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`
- [x] T024 [P] [US1] Create unit tests for PropertyBinding event subscription in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`
- [x] T025 [P] [US1] Create unit tests for PropertyBinding initial sync in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`

### Implementation for User Story 1

- [x] T026 [P] [US1] Implement `ParentLookup<T>` strategy in `src/GameEngine/Components/Lookups/ParentLookup.cs`
- [x] T027 [US1] Implement `PropertyBinding.FromParent<T>()` method in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T028 [US1] Implement `PropertyBinding.GetPropertyValue(string)` method in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T029 [US1] Implement `PropertyBinding.Activate()` with source resolution and event subscription in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T030 [US1] Implement initial value synchronization in `PropertyBinding.Activate()` in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T031 [US1] Implement `PropertyBinding.Deactivate()` with event cleanup in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T032 [US1] Add binding lifecycle integration to `Component.OnLoad()` in `src/GameEngine/Components/Component.cs`
- [x] T033 [US1] Add binding activation to `Component.OnActivate()` in `src/GameEngine/Components/Component.cs`
- [x] T034 [US1] Add binding deactivation to `Component.OnDeactivate()` in `src/GameEngine/Components/Component.cs`
- [x] T035 [US1] Run unit tests and verify all User Story 1 tests pass

### Integration Tests for User Story 1

- [x] T036 [US1] Create integration test for parent-child health bar scenario in `src/IntegrationTests/PropertyBinding/ParentChildBinding.Tests.cs`
- [x] T037 [US1] Create integration test for binding cleanup and memory leak prevention in `src/IntegrationTests/PropertyBinding/BindingLifecycle.Tests.cs`

**Checkpoint**: Basic parent-to-child bindings work end-to-end with automatic cleanup

---

## Phase 5: User Story 2 - Property Bindings with Type Conversion (Priority: P2)

**Goal**: Support value converters for transforming values during binding updates (float → string, etc.)

**Independent Test**: Bind a float health value to a string text property with a format converter, verify formatted output

### Unit Tests for User Story 2

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T038 [P] [US2] Create unit tests for StringFormatConverter in `src/Tests/GameEngine/Data/StringFormatConverter.Tests.cs`
- [x] T039 [P] [US2] Create unit tests for MultiplyConverter in `src/Tests/GameEngine/Data/MultiplyConverter.Tests.cs`
- [x] T040 [P] [US2] Create unit tests for PercentageConverter in `src/Tests/GameEngine/Data/PercentageConverter.Tests.cs`
- [x] T041 [P] [US2] Create unit tests for PropertyBinding.WithConverter() in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`
- [x] T042 [P] [US2] Create unit tests for PropertyBinding.AsFormattedString() in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`
- [x] T043 [P] [US2] Create unit tests for converter null handling in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`

### Implementation for User Story 2

- [x] T044 [P] [US2] Implement `StringFormatConverter` in `src/GameEngine/Data/StringFormatConverter.cs`
- [x] T045 [P] [US2] Implement `MultiplyConverter` in `src/GameEngine/Data/MultiplyConverter.cs`
- [x] T046 [P] [US2] Implement `PercentageConverter` (bidirectional) in `src/GameEngine/Data/PercentageConverter.cs`
- [x] T047 [US2] Implement `PropertyBinding.WithConverter()` method in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T048 [US2] Implement `PropertyBinding.AsFormattedString()` helper method in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T049 [US2] Add converter invocation logic to PropertyBinding event handlers in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T050 [US2] Add null result handling for converter failures in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T051 [US2] Run unit tests and verify all User Story 2 tests pass

### Integration Tests for User Story 2

- [x] T052 [US2] Create integration test for float-to-string formatted binding in `src/IntegrationTests/PropertyBinding/TypeConversion.Tests.cs`
- [x] T053 [US2] Create integration test for custom converter pipeline in `src/IntegrationTests/PropertyBinding/TypeConversion.Tests.cs`

**Checkpoint**: Value converters work correctly with bindings for type transformation

---

## Phase 6: User Story 3 - Named Component Lookup (Priority: P2)

**Goal**: Bind to specific named components anywhere in the tree (not just parent/child)

**Independent Test**: Create two sibling components where one binds to another by name, verify binding works

### Unit Tests for User Story 3

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T054 [P] [US3] Create unit tests for NamedObjectLookup strategy in `src/Tests/GameEngine/Components/Lookups/NamedObjectLookup.Tests.cs`
- [x] T055 [P] [US3] Create unit tests for PropertyBinding.FromNamedObject() in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`
- [x] T056 [P] [US3] Create unit tests for named lookup with missing component in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`

### Implementation for User Story 3

- [x] T057 [US3] Implement `NamedObjectLookup` strategy in `src/GameEngine/Components/Lookups/NamedObjectLookup.cs`
- [x] T058 [US3] Implement `PropertyBinding.FromNamedObject(string)` method in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T059 [US3] Implement tree search algorithm for FindComponentByName in `src/GameEngine/Components/Lookups/NamedObjectLookup.cs`
- [x] T060 [US3] Run unit tests and verify all User Story 3 tests pass

### Integration Tests for User Story 3

- [x] T061 [US3] Create integration test for named component binding in `src/IntegrationTests/PropertyBinding/NamedBinding.Tests.cs`
- [x] T062 [US3] Create integration test for cross-tree named binding in `src/IntegrationTests/PropertyBinding/CrossTreeBinding.Tests.cs`

**Checkpoint**: Named component lookups work correctly for flexible component relationships

---

## Phase 7: User Story 4 - Sibling and Context-Based Lookups (Priority: P3)

**Goal**: Support sibling and context lookup strategies for advanced component relationships

**Independent Test**: Create sibling components binding with FromSibling, and context provider with FromContext

### Unit Tests for User Story 4

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T063 [P] [US4] Create unit tests for SiblingLookup strategy in `src/Tests/GameEngine/Components/Lookups/SiblingLookup.Tests.cs`
- [x] T064 [P] [US4] Create unit tests for ChildLookup strategy in `src/Tests/GameEngine/Components/Lookups/ChildLookup.Tests.cs`
- [x] T065 [P] [US4] Create unit tests for ContextLookup strategy in `src/Tests/GameEngine/Components/Lookups/ContextLookup.Tests.cs`
- [x] T066 [P] [US4] Create unit tests for PropertyBinding.FromSibling() in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`
- [x] T067 [P] [US4] Create unit tests for PropertyBinding.FromChild() in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`
- [x] T068 [P] [US4] Create unit tests for PropertyBinding.FromContext() in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`

### Implementation for User Story 4

- [x] T069 [P] [US4] Implement `SiblingLookup<T>` strategy in `src/GameEngine/Components/Lookups/SiblingLookup.cs`
- [x] T070 [P] [US4] Implement `ChildLookup<T>` strategy in `src/GameEngine/Components/Lookups/ChildLookup.cs`
- [x] T071 [P] [US4] Implement `ContextLookup<T>` strategy with recursive ancestor search in `src/GameEngine/Components/Lookups/ContextLookup.cs`
- [x] T072 [US4] Implement `PropertyBinding.FromSibling<T>()` method in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T073 [US4] Implement `PropertyBinding.FromChild<T>()` method in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T074 [US4] Implement `PropertyBinding.FromContext<T>()` method in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T075 [US4] Run unit tests and verify all User Story 4 tests pass

### Integration Tests for User Story 4

- [x] T076 [US4] Create integration test for sibling binding scenario in `src/IntegrationTests/PropertyBinding/SiblingBinding.Tests.cs`
- [x] T077 [US4] Create integration test for theme context provider scenario in `src/IntegrationTests/PropertyBinding/ContextBinding.Tests.cs`

**Checkpoint**: All lookup strategies (parent, sibling, child, context, named) work correctly

---

## Phase 8: User Story 5 - Two-Way Property Bindings (Priority: P3)

**Goal**: Support bidirectional bindings where changes flow both source → target AND target → source

**Independent Test**: Create slider bound two-way to volume setting, verify dragging updates setting AND external changes update slider

### Unit Tests for User Story 5

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T078 [P] [US5] Create unit tests for PropertyBinding.TwoWay() in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`
- [x] T079 [P] [US5] Create unit tests for bidirectional event subscription in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`
- [x] T080 [P] [US5] Create unit tests for infinite loop prevention in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`
- [x] T081 [P] [US5] Create unit tests for ConvertBack with bidirectional converters in `src/Tests/GameEngine/Components/PropertyBinding.Tests.cs`

### Implementation for User Story 5

- [x] T082 [US5] Implement `PropertyBinding.TwoWay()` method in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T083 [US5] Add target event subscription logic in `PropertyBinding.Activate()` for TwoWay mode in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T084 [US5] Implement re-entry detection with `_isUpdating` flag in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T085 [US5] Add target-to-source update handler with ConvertBack support in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T086 [US5] Add target event cleanup in `PropertyBinding.Deactivate()` for TwoWay mode in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T087 [US5] Run unit tests and verify all User Story 5 tests pass

### Integration Tests for User Story 5

- [x] T088 [US5] Create integration test for two-way slider/volume binding in `src/IntegrationTests/PropertyBinding/TwoWayBinding.Tests.cs`
- [x] T089 [US5] Create integration test for bidirectional converter pipeline in `src/IntegrationTests/PropertyBinding/TwoWayBinding.Tests.cs`

**Checkpoint**: Two-way bindings work correctly with cycle prevention and bidirectional converters

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T090 [P] Create PropertyBinding usage examples in `src/TestApp/Examples/PropertyBindingExample.cs`
- [x] T091 [P] Add comprehensive XML documentation to all public APIs in `src/GameEngine/Components/PropertyBinding.cs`
- [x] T092 [P] Verify 80% code coverage target for all PropertyBinding classes across unit tests
- [ ] T093 Create developer guide section in `.docs/Property Binding System.md` with common patterns
- [ ] T094 Run all quickstart.md scenarios manually in TestApp and verify outputs
- [ ] T095 Performance benchmark: verify <5% overhead vs direct property assignment in `src/IntegrationTests/Performance/BindingBenchmark.Tests.cs`
- [ ] T096 Memory leak test: verify zero leaks after 1000 activate/deactivate cycles in `src/IntegrationTests/PropertyBinding/MemoryLeak.Tests.cs`
- [ ] T097 Rebuild Nexus.GameEngine.sln with configuration Debug and verify no warnings/errors
- [ ] T098 Run full test suite and verify all tests pass

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 6 (Phase 3)**: Depends on Foundational - Source generators needed for all other stories
- **User Stories 1, 2, 3 (Phases 4-6)**: All depend on Foundational + US6 completion
  - Can proceed in parallel after Phase 3
  - Or sequentially in priority order (US1 → US2 → US3)
- **User Story 4 (Phase 7)**: Depends on Foundational + US6 - Independent of US1-US3
- **User Story 5 (Phase 8)**: Depends on US1 completion (uses OneWay binding infrastructure)
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 6 (P1)**: Infrastructure - No dependencies on other user stories
- **User Story 1 (P1)**: Depends on US6 - Core binding functionality
- **User Story 2 (P2)**: Depends on US1 - Extends with converters
- **User Story 3 (P2)**: Depends on US1 - Adds named lookup (independent of US2)
- **User Story 4 (P3)**: Depends on US1 - Adds sibling/context lookups (independent of US2, US3)
- **User Story 5 (P3)**: Depends on US1, US2 - Uses OneWay + converters for TwoWay

### Within Each User Story

- Unit tests MUST be written and FAIL before implementation
- Foundational classes (interfaces, base classes) before implementations
- Core functionality before integration
- All tests pass before moving to next story

### Parallel Opportunities

**Phase 1 - Setup**: All tasks can run in parallel

**Phase 2 - Foundational**: Tasks T005-T008 can run in parallel (all interfaces/enums)

**Phase 3 - US6**: 
- Tests T011-T013 can run in parallel
- Implementation tasks are sequential (generator modifications)

**Phase 4 - US1**:
- Tests T021-T025 can run in parallel
- T026 (ParentLookup) can run while T027-T031 are in progress (different files)

**Phase 5 - US2**:
- Tests T038-T043 can run in parallel
- Converters T044-T046 can run in parallel

**Phase 6 - US3**:
- Tests T054-T056 can run in parallel

**Phase 7 - US4**:
- Tests T063-T068 can run in parallel
- Lookup strategies T069-T071 can run in parallel

**Phase 8 - US5**:
- Tests T078-T081 can run in parallel

**Phase 9 - Polish**:
- Tasks T090-T093 can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all unit tests for User Story 1 together:
Task T021: "Create unit tests for ParentLookup strategy"
Task T022: "Create unit tests for PropertyBinding.Activate()"
Task T023: "Create unit tests for PropertyBinding.Deactivate()"
Task T024: "Create unit tests for PropertyBinding event subscription"
Task T025: "Create unit tests for PropertyBinding initial sync"

# Then launch model implementation (after tests written):
Task T026: "Implement ParentLookup<T> strategy"
# Can proceed with T027-T031 while T026 is in progress (different files)
```

---

## Implementation Strategy

### MVP First (User Stories 6 + 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 6 (Source generators - infrastructure)
4. Complete Phase 4: User Story 1 (Basic parent-child bindings)
5. **STOP and VALIDATE**: Test parent-child bindings independently
6. Deploy/demo if ready

**MVP Deliverable**: Basic parent-to-child property bindings with source-generated type safety

### Incremental Delivery

1. Complete Setup + Foundational + US6 → Infrastructure ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo (Converters added)
4. Add User Story 3 → Test independently → Deploy/Demo (Named lookups)
5. Add User Story 4 → Test independently → Deploy/Demo (Advanced lookups)
6. Add User Story 5 → Test independently → Deploy/Demo (Two-way bindings)
7. Polish phase → Final refinements

**Each story adds value without breaking previous stories**

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational + US6 together
2. Once US6 is done:
   - Developer A: User Story 1 (core binding)
   - Developer B: User Story 2 (converters - depends on US1 tests passing first)
   - Developer C: User Story 3 (named lookup - depends on US1 tests passing first)
3. After US1, US2, US3 complete:
   - Developer D: User Story 4 (sibling/context lookups)
   - Developer E: User Story 5 (two-way bindings)

---

## Summary

**Total Tasks**: 98 tasks across 9 phases

**Task Distribution by User Story**:
- Setup (Phase 1): 3 tasks
- Foundational (Phase 2): 7 tasks
- User Story 6 - Source Generators (P1): 10 tasks (3 tests, 7 implementation)
- User Story 1 - Basic Binding (P1): 17 tasks (5 unit tests, 10 implementation, 2 integration)
- User Story 2 - Converters (P2): 15 tasks (6 unit tests, 7 implementation, 2 integration)
- User Story 3 - Named Lookup (P2): 9 tasks (3 unit tests, 4 implementation, 2 integration)
- User Story 4 - Sibling/Context (P3): 15 tasks (6 unit tests, 7 implementation, 2 integration)
- User Story 5 - Two-Way (P3): 12 tasks (4 unit tests, 6 implementation, 2 integration)
- Polish (Phase 9): 9 tasks

**Parallel Opportunities**: 35+ tasks marked [P] can run in parallel within their phases

**Independent Test Criteria**:
- US6: Compiling components generates PropertyBindings classes
- US1: Parent health changes automatically update child health bar
- US2: Float values display as formatted strings
- US3: Components bind to named components across the tree
- US4: Siblings and context providers resolve correctly
- US5: Two-way slider updates volume AND volume updates slider

**MVP Scope**: Phases 1-4 (Setup + Foundational + US6 + US1) = 37 tasks = Basic property binding system

**Format Validation**: ✅ All tasks follow checklist format with checkboxes, IDs, labels, and file paths
