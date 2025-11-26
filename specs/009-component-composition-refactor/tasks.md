# Tasks: Component Composition Refactor

**Feature Branch**: `009-component-composition-refactor`
**Spec**: [spec.md](./spec.md)

## Phase 1: Setup
- [x] T001 Verify clean build of solution

## Phase 2: Foundational
- [x] T002 Modify `src/GameEngine/Components/Component.cs` to expose `Parent` property
- [x] T003 Modify `src/GameEngine/Components/RuntimeComponent.cs` to restrict inheritance

## Phase 3: User Story 1 (Standard UI Elements)
- [x] T004 [US1] Implement `UserInterfaceElement` in `src/GameEngine/GUI/UserInterfaceElement.cs` implementing `ITransformable`
- [x] T005 [US1] Create `SpriteRenderer` in `src/GameEngine/GUI/SpriteRenderer.cs` implementing `IDrawable`
- [x] T006 [US1] Create `TextRenderer` in `src/GameEngine/GUI/TextRenderer.cs` implementing `IDrawable`
- [x] T007 [US1] Create `UserInterfaceElementTemplate` in `src/GameEngine/GUI/UserInterfaceElement.cs`
- [x] T008 [US1] Create integration test for Button composition in `src/TestApp/Testing/CompositionTests.cs`

## Phase 4: User Story 2 (Custom Visual Components)
- [ ] T009 [US2] Create unit test for custom renderer in `src/Tests/GameEngine/GUI/CustomRendererTests.cs`

## Phase 5: User Story 3 (Non-Visual UI Elements)
- [ ] T010 [US3] Create unit test for container element in `src/Tests/GameEngine/GUI/ContainerTests.cs`

## Phase 6: Polish
- [ ] T011 Update `src/GameEngine/GUI/README.md` with composition examples

## Dependencies
- US1 blocks US2 and US3
- Foundational tasks block all User Stories

## Implementation Strategy
- Implement foundational changes first to enable composition.
- Implement `UserInterfaceElement` as the core container.
- Implement Renderers (`SpriteRenderer`, `TextRenderer`) to replace inheritance-based rendering.
- Verify with integration tests.
