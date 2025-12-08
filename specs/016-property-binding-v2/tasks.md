# Tasks: Property Binding Framework Revision

**Feature**: `016-property-binding-v2`  
**Input**: Design documents from `/specs/016-property-binding-v2/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Key Architectural Decision**: NO SOURCE GENERATORS - Framework uses explicit setter delegates, simple `IPropertyBinding[]` arrays, and static lookup helpers instead of generated code.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup & Cleanup

**Purpose**: Remove old implementations and prepare project structure

- [ ] T001 [P] Identify and document existing PropertyBinding-related classes in `src/GameEngine/Components/`
- [ ] T002 [P] Identify and document PropertyBindingsGenerator in `src/SourceGenerators/`
- [ ] T003 [P] Identify generated `{Component}PropertyBindings.g.cs` files across solution
- [ ] T004 Create backup branch of current 013-property-binding implementation
- [ ] T005 Remove `src/SourceGenerators/PropertyBindingsGenerator.cs` if exists
- [ ] T006 [P] Remove all generated `{Component}PropertyBindings.g.cs` files
- [ ] T007 [P] Remove `src/GameEngine/Components/PropertyBindings.cs` abstract base class if exists
- [ ] T008 [P] Remove `src/GameEngine/Components/IPropertyBindingDefinition.cs` if exists and not needed
- [ ] T009 Create `.temp/agent/` directory for temporary work files if not exists

---

## Phase 2: Foundational Infrastructure

**Purpose**: Core interfaces and helper classes that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T010 Create/update `src/GameEngine/Components/IPropertyBinding.cs` with Activate/Deactivate methods per contract
- [ ] T011 [P] Create `src/GameEngine/Components/Lookups/` directory for lookup strategy classes
- [ ] T012 [P] Create `src/GameEngine/Components/Lookups/ParentLookup.cs` with generic FindParent implementation
- [ ] T013 [P] Create `src/GameEngine/Components/Lookups/SiblingLookup.cs` with sibling resolution logic
- [ ] T014 [P] Create `src/GameEngine/Components/Lookups/ChildLookup.cs` with child search implementation
- [ ] T015 [P] Create `src/GameEngine/Components/Lookups/NamedObjectLookup.cs` with recursive tree search
- [ ] T016 [P] Create `src/GameEngine/Components/Lookups/ILookupStrategy.cs` interface with Resolve method
- [ ] T017 [P] Update `src/GameEngine/Data/IValueConverter.cs` interface (verify exists, update if needed)
- [ ] T018 [P] Update `src/GameEngine/Data/StringFormatConverter.cs` implementation (verify exists, update if needed)
- [ ] T019 Update `src/GameEngine/Components/Component.PropertyBindings.cs` to add PropertyBindings List<IPropertyBinding> property
- [ ] T020 Update `src/GameEngine/Components/Component.PropertyBindings.cs` to add LoadPropertyBindings method
- [ ] T021 Update `src/GameEngine/Components/Component.PropertyBindings.cs` to add ActivatePropertyBindings method
- [ ] T022 Update `src/GameEngine/Components/Component.PropertyBindings.cs` to add DeactivatePropertyBindings method
- [ ] T023 Update `src/GameEngine/Components/Template.cs` to add `IPropertyBinding[] Bindings` property
- [ ] T024 Integrate LoadPropertyBindings call into Component.OnLoad lifecycle method
- [ ] T025 Integrate ActivatePropertyBindings call into Component.OnActivate lifecycle method
- [ ] T026 Integrate DeactivatePropertyBindings call into Component.OnDeactivate lifecycle method

**Checkpoint**: Foundation ready - PropertyBinding core class can now be implemented

---

## Phase 3: User Story 1 - Basic Parent Property Binding (Priority: P1) 🎯 MVP

**Goal**: Implement core PropertyBinding<TSource, TValue> class with fluent API for parent-to-child property synchronization

**Independent Test**: Create a parent component with a health property and a child component with text display. Configure the child's template with binding. Verify that changing parent health updates child text.

### Implementation for User Story 1

- [ ] T027 [US1] Create `src/GameEngine/Components/PropertyBinding.cs` with generic class definition and type parameters
- [ ] T028 [US1] Implement PropertyBinding private fields (lookup func, property getter, converter, event info, source/target refs, isUpdating flag)
- [ ] T029 [US1] Implement PropertyBinding.FindParent() method that sets lookup delegate to ParentLookup.FindParent
- [ ] T030 [US1] Implement PropertyBinding.GetPropertyValue() method with Expression compilation and type transformation
- [ ] T031 [US1] Implement PropertyBinding.Set() method that stores target setter delegate and returns IPropertyBinding
- [ ] T032 [US1] Implement PropertyBinding.Activate() method with source resolution and event subscription logic
- [ ] T033 [US1] Implement PropertyBinding.Deactivate() method with event unsubscription and reference cleanup
- [ ] T034 [US1] Implement PropertyBinding.OnSourcePropertyChanged event handler with recursion guard
- [ ] T035 [US1] Implement PropertyBinding initial value sync logic in Activate method
- [ ] T036 [US1] Add error handling for configuration errors (fail fast) in fluent methods
- [ ] T037 [US1] Add error handling for runtime resolution failures (log and skip) in Activate method
- [ ] T038 [US1] Create `Tests/GameEngine/Components/PropertyBindingTests.cs` for core binding functionality
- [ ] T039 [US1] Write unit test: PropertyBinding resolves parent source component correctly
- [ ] T040 [US1] Write unit test: PropertyBinding subscribes to source event on activation
- [ ] T041 [US1] Write unit test: PropertyBinding updates target property when source event fires
- [ ] T042 [US1] Write unit test: PropertyBinding unsubscribes from event on deactivation
- [ ] T043 [US1] Write unit test: PropertyBinding handles source component not found gracefully
- [ ] T044 [US1] Write unit test: PropertyBinding prevents recursive updates with isUpdating flag
- [ ] T045 [US1] Create `Tests/GameEngine/Components/PropertyBindingLifecycleTests.cs` for activation/deactivation tests
- [ ] T046 [US1] Write lifecycle test: Component.Load copies template bindings to PropertyBindings collection
- [ ] T047 [US1] Write lifecycle test: Component.Activate calls Activate on all bindings
- [ ] T048 [US1] Write lifecycle test: Component.Deactivate calls Deactivate on all bindings
- [ ] T049 [US1] Write lifecycle test: Repeated activation/deactivation cycles work correctly without memory leaks
- [ ] T050 [US1] Create `src/TestApp/Testing/PropertyBindingIntegrationTests.cs` for frame-based integration tests
- [ ] T051 [US1] Write integration test: Parent health changes propagate to child text display in single frame
- [ ] T052 [US1] Write integration test: Binding works across component tree depth >5 levels
- [ ] T053 [US1] Rebuild solution and verify all User Story 1 tests pass

**Checkpoint**: User Story 1 complete - Basic parent-to-child property binding fully functional

---

## Phase 4: User Story 2 - Type Conversion and Formatting (Priority: P1)

**Goal**: Add string formatting and custom value conversion to property bindings

**Independent Test**: Bind a float health property to a string text property with `.AsFormattedString("Health: {0:F0}")`. Verify text displays "Health: 75" when health is 75.0.

### Implementation for User Story 2

- [ ] T054 [US2] Implement PropertyBinding.AsFormattedString() method that creates StringFormatConverter internally
- [ ] T055 [US2] Implement PropertyBinding.WithConverter() method that stores IValueConverter instance
- [ ] T056 [US2] Update PropertyBinding.OnSourcePropertyChanged to apply converter.Convert() before setting target
- [ ] T057 [US2] Implement type transformation logic in GetPropertyValue to support converter chaining
- [ ] T058 [US2] Create `Tests/GameEngine/Components/PropertyBindingConverterTests.cs` for conversion tests
- [ ] T059 [US2] Write converter test: AsFormattedString converts float to formatted string correctly
- [ ] T060 [US2] Write converter test: WithConverter applies custom IValueConverter to values
- [ ] T061 [US2] Write converter test: Converter handles null values gracefully
- [ ] T062 [US2] Write converter test: StringFormatConverter handles FormatException and returns fallback
- [ ] T063 [US2] Write integration test: Health bar displays "Health: 100" with AsFormattedString binding
- [ ] T064 [US2] Write integration test: Custom percentage converter transforms 0.75 to "75%"
- [ ] T065 [US2] Rebuild solution and verify all User Story 2 tests pass

**Checkpoint**: User Story 2 complete - Type conversion and formatting working with User Story 1

---

## Phase 5: User Story 3 - Custom Event Subscription (Priority: P2)

**Goal**: Enable property-specific event subscription via `.On(s => s.HealthChanged)` for performance

**Independent Test**: Create component with both HealthChanged and ManaChanged events. Bind only to HealthChanged. Verify only health changes trigger the binding.

### Implementation for User Story 3

- [ ] T066 [US3] Implement PropertyBinding.On() method that extracts EventInfo from Expression
- [ ] T067 [US3] Add Expression validation in On() method to ensure it references an event (not property/method)
- [ ] T068 [US3] Update PropertyBinding.Activate() to use configured event or fall back to PropertyChanged
- [ ] T069 [US3] Implement CreateEventHandler() helper method to create typed delegate for event subscription
- [ ] T070 [US3] Add unit test: On() method extracts correct EventInfo from expression
- [ ] T071 [US3] Add unit test: On() throws ArgumentException for non-event expressions
- [ ] T072 [US3] Add unit test: Binding subscribes to specific event when On() is configured
- [ ] T073 [US3] Add unit test: Binding falls back to PropertyChanged event when On() not called
- [ ] T074 [US3] Add unit test: Binding only triggers on HealthChanged, not on ManaChanged
- [ ] T075 [US3] Add integration test: Multiple bindings to different events on same source work independently
- [ ] T076 [US3] Rebuild solution and verify all User Story 3 tests pass

**Checkpoint**: User Story 3 complete - Custom event subscription working with previous stories

---

## Phase 6: User Story 4 - Source Object Resolution Strategies (Priority: P2)

**Goal**: Support FindSibling, FindChild, FindNamed lookup strategies beyond default FindParent

**Independent Test**: Create sibling components and bind between them using `.FindSibling<T>()`. Create named component and bind using `.FindNamed("Name")`. Verify both strategies work.

### Implementation for User Story 4

- [ ] T077 [US4] Implement PropertyBinding.FindSibling() method that sets lookup to SiblingLookup.FindSibling
- [ ] T078 [US4] Implement PropertyBinding.FindChild() method that sets lookup to ChildLookup.FindChild
- [ ] T079 [US4] Implement PropertyBinding.FindNamed() method that sets lookup to NamedObjectLookup.Resolve
- [ ] T080 [US4] Create `Tests/GameEngine/Components/Lookups/ParentLookupTests.cs` for parent resolution tests
- [ ] T081 [US4] Write lookup test: ParentLookup finds nearest parent of specified type
- [ ] T082 [US4] Write lookup test: ParentLookup returns null when no parent matches
- [ ] T083 [US4] Write lookup test: ParentLookup traverses multiple levels correctly
- [ ] T084 [US4] Create `Tests/GameEngine/Components/Lookups/SiblingLookupTests.cs` for sibling resolution tests
- [ ] T085 [US4] Write lookup test: SiblingLookup finds sibling of specified type
- [ ] T086 [US4] Write lookup test: SiblingLookup returns null when no sibling matches
- [ ] T087 [US4] Write lookup test: SiblingLookup excludes target component from results
- [ ] T088 [US4] Create `Tests/GameEngine/Components/Lookups/NamedObjectLookupTests.cs` for named lookup tests
- [ ] T089 [US4] Write lookup test: NamedObjectLookup finds component by name in tree
- [ ] T090 [US4] Write lookup test: NamedObjectLookup searches recursively from root
- [ ] T091 [US4] Write lookup test: NamedObjectLookup returns null when name not found
- [ ] T092 [US4] Add integration test: Sibling binding between two UI panels works correctly
- [ ] T093 [US4] Add integration test: Named lookup finds global GameState component
- [ ] T094 [US4] Add integration test: Child binding from parent to specific child component works
- [ ] T095 [US4] Rebuild solution and verify all User Story 4 tests pass

**Checkpoint**: User Story 4 complete - All lookup strategies functional and tested

---

## Phase 7: User Story 5 - Binding Without Source Generators (Priority: P1)

**Goal**: Validate that framework operates without source generators using explicit setter delegates

**Independent Test**: Create property binding with `.Set(SetText)` without any generated code. Verify binding updates property at runtime.

### Implementation for User Story 5

- [ ] T096 [US5] Verify PropertyBinding.Set() stores Action<TValue> delegate correctly
- [ ] T097 [US5] Verify PropertyBinding without .Set() cannot be assigned to IPropertyBinding (type safety test)
- [ ] T098 [US5] Verify templates define bindings with explicit setter methods, not generated properties
- [ ] T099 [US5] Add unit test: PropertyBinding.Set() makes binding valid and activatable
- [ ] T100 [US5] Add unit test: PropertyBinding calls target setter delegate when source changes
- [ ] T101 [US5] Add unit test: Template.Bindings array contains IPropertyBinding instances from fluent API
- [ ] T102 [US5] Add documentation test: Create sample template with 5 different bindings, all using explicit setters
- [ ] T103 [US5] Verify solution builds with NO source generator warnings or errors
- [ ] T104 [US5] Verify no `{Component}PropertyBindings.g.cs` files generated during build
- [ ] T105 [US5] Rebuild solution and verify all User Story 5 tests pass

**Checkpoint**: User Story 5 complete - Source generator independence validated

---

## Phase 8: User Story 6 - Default Behavior and Minimal Configuration (Priority: P2)

**Goal**: Implement sensible defaults so minimal bindings work with just `.Set(setter)`

**Independent Test**: Create binding `new PropertyBinding<Parent, string>().Set(SetText)` with no explicit FindParent, On, or conversion. Verify defaults work correctly.

### Implementation for User Story 6

- [ ] T106 [US6] Implement default FindParent lookup when no Find method called before Activate
- [ ] T107 [US6] Implement default PropertyChanged event subscription when On() not called
- [ ] T108 [US6] Update PropertyBinding constructor to initialize with sensible default state
- [ ] T109 [US6] Add unit test: Binding without FindParent() defaults to parent lookup
- [ ] T110 [US6] Add unit test: Binding without On() subscribes to PropertyChanged event
- [ ] T111 [US6] Add unit test: Minimal binding `new PropertyBinding<T, V>().Set(setter)` works end-to-end
- [ ] T112 [US6] Add integration test: Simplest possible binding syntax updates correctly at runtime
- [ ] T113 [US6] Update quickstart.md examples to show both explicit and minimal syntax
- [ ] T114 [US6] Rebuild solution and verify all User Story 6 tests pass

**Checkpoint**: User Story 6 complete - All user stories now functional with sensible defaults

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, code quality, and validation across all user stories

- [ ] T115 [P] Add XML documentation comments to all public methods in PropertyBinding.cs
- [ ] T116 [P] Add XML documentation comments to IPropertyBinding.cs interface
- [ ] T117 [P] Add XML documentation comments to all ILookupStrategy implementations
- [ ] T118 [P] Add XML documentation comments to IValueConverter and StringFormatConverter
- [ ] T119 Review code coverage report - verify minimum 80% coverage achieved
- [ ] T120 [P] Update `specs/016-property-binding-v2/quickstart.md` with final examples from all user stories
- [ ] T121 [P] Create sample templates in TestApp demonstrating all binding patterns
- [ ] T122 Validate quickstart.md code examples compile and run correctly
- [ ] T123 Run integration tests with 100 components, 10 bindings each (stress test)
- [ ] T124 [P] Profile binding activation time - verify <1ms per component target met
- [ ] T125 [P] Profile event handling overhead - verify <0.01ms per property change target met
- [ ] T126 Memory leak test: Activate/deactivate component 1000 times, verify no leaks
- [ ] T127 [P] Review all error messages for clarity and actionability
- [ ] T128 [P] Add logging infrastructure integration for binding warnings (if ILogger available)
- [ ] T129 Code cleanup: Remove commented-out code and debug statements
- [ ] T130 Code cleanup: Ensure consistent formatting and naming conventions
- [ ] T131 Final build: Compile solution with zero warnings
- [ ] T132 Final test run: Execute all unit and integration tests - 100% pass rate required

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational - Core binding implementation (MVP)
- **User Story 2 (Phase 4)**: Depends on User Story 1 - Extends binding with conversion
- **User Story 3 (Phase 5)**: Depends on User Story 1 - Extends binding with custom events (can run parallel to US2)
- **User Story 4 (Phase 6)**: Depends on Foundational - Extends lookup strategies (can run parallel to US1-3)
- **User Story 5 (Phase 7)**: Depends on User Story 1 - Validation of no-generator approach
- **User Story 6 (Phase 8)**: Depends on User Stories 1, 3, 4 - Validates defaults work
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

**Critical Path (MVP)**:
1. Foundational (Phase 2) - MUST complete first
2. User Story 1 (Phase 3) - Core binding (MVP baseline)
3. User Story 2 (Phase 4) - Type conversion (required for real-world use)

**Parallel Opportunities**:
- User Story 3 (custom events) and User Story 2 (conversion) can develop in parallel after US1
- User Story 4 (lookup strategies) can develop in parallel after Foundational
- Setup tasks (T001-T009) can all run in parallel

**Suggested MVP Scope**: 
- Foundational (Phase 2)
- User Story 1 (Phase 3) - Basic parent binding
- User Story 2 (Phase 4) - Type conversion
- Validation from Phase 9 (T119, T131-T132)

This delivers immediately usable property binding with parent lookup and string formatting.

### Within Each User Story

- Implementation tasks before test tasks when following TDD (Red → Green → Refactor)
- Unit tests can run in parallel (marked [P]) for different aspects
- Integration tests depend on corresponding unit tests passing
- Rebuild verification task is last in each phase

### Parallel Opportunities

**Phase 1 - Setup**: All tasks T001-T008 can run in parallel (documentation and file removal)

**Phase 2 - Foundational**: 
- T011-T018 (all lookup and converter files) can run in parallel
- T010 (IPropertyBinding) must complete before T019-T023
- T019-T026 (Component integration) must be sequential

**Phase 3 - User Story 1**:
- T027-T037 (implementation) should be sequential (building up the class)
- T038-T044 (unit tests) can run in parallel after T027-T037 complete
- T045-T049 (lifecycle tests) can run in parallel with T038-T044
- T050-T052 (integration tests) depend on implementation and unit tests

**Phase 4-8**: Similar pattern - implementation sequential, tests parallel

**Phase 9 - Polish**: T115-T118, T120-T121, T124-T125, T127-T128 can all run in parallel

---

## Parallel Example: User Story 1 Implementation

```bash
# Sequential implementation (build up PropertyBinding class)
Task T027: Create class skeleton
Task T028: Add private fields
Task T029: Implement FindParent()
...

# Then parallel testing (different test classes)
Parallel {
  Task T038-T044: PropertyBindingTests.cs
  Task T045-T049: PropertyBindingLifecycleTests.cs  
}

# Then integration
Task T050-T052: Integration tests
Task T053: Rebuild and verify
```

---

## Implementation Strategy

**MVP-First Approach**:
1. **Week 1**: Phase 1 (Setup) + Phase 2 (Foundational)
2. **Week 2**: Phase 3 (User Story 1) - Basic binding working
3. **Week 3**: Phase 4 (User Story 2) - Conversion working → **MVP DELIVERY**
4. **Week 4**: Phase 5-6 (User Stories 3-4) - Enhanced features
5. **Week 5**: Phase 7-8 (User Stories 5-6) - Validation and defaults
6. **Week 6**: Phase 9 (Polish) - Documentation and quality

**Incremental Delivery**:
- Each user story delivers independently testable functionality
- MVP (US1 + US2) provides 80% of real-world value
- Advanced features (US3-6) can be delivered iteratively based on feedback

**Quality Gates**:
- 80% minimum code coverage at each phase checkpoint
- Zero build warnings before moving to next phase
- All integration tests passing before phase completion

---

## Task Summary

**Total Tasks**: 132

**Breakdown by Phase**:
- Phase 1 (Setup): 9 tasks
- Phase 2 (Foundational): 17 tasks (BLOCKING)
- Phase 3 (User Story 1): 27 tasks ⭐ MVP Core
- Phase 4 (User Story 2): 12 tasks ⭐ MVP Complete
- Phase 5 (User Story 3): 11 tasks
- Phase 6 (User Story 4): 19 tasks
- Phase 7 (User Story 5): 10 tasks
- Phase 8 (User Story 6): 9 tasks
- Phase 9 (Polish): 18 tasks

**Parallelizable Tasks**: 47 tasks marked [P]

**MVP Task Count**: 65 tasks (Phase 1 + Phase 2 + US1 + US2 + validation subset)

**Test Tasks**: 54 tasks (41% of total) - comprehensive coverage strategy
