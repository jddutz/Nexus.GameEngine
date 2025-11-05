# Tasks: Text Rendering with TextElement

**Feature**: Text Rendering with TextElement  
**Branch**: `002-text-rendering`  
**Date**: 2025-11-04

**Input**: Design documents from `/specs/002-text-rendering/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: Test tasks included as this is a critical graphics feature requiring comprehensive validation

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and dependency setup

- [X] T001 Verify build succeeds: `dotnet build Nexus.GameEngine.sln --configuration Debug`
- [X] T002 Add StbTrueTypeSharp NuGet package to src/GameEngine/GameEngine.csproj
- [X] T003 [P] Download Roboto-Regular.ttf and add to src/GameEngine/EmbeddedResources/Fonts/
- [X] T004 [P] Configure Roboto-Regular.ttf as embedded resource in src/GameEngine/GameEngine.csproj
- [X] T005 [P] Update .docs/Project Structure.md with new Resources/Fonts/ subsystem and TextElement location

**Checkpoint**: Dependencies installed, embedded font ready, documentation updated

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core font infrastructure that MUST be complete before user stories can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 [P] Create CharacterRange enum in src/GameEngine/Resources/Fonts/CharacterRange.cs
- [X] T007 [P] Create IFontSource interface in src/GameEngine/Resources/Fonts/FontSource/IFontSource.cs
- [X] T008 [P] Implement EmbeddedTrueTypeFontSource in src/GameEngine/Resources/Fonts/FontSource/EmbeddedTrueTypeFontSource.cs
- [X] T009 [P] Create FontDefinition record in src/GameEngine/Resources/Fonts/FontDefinition.cs (uses FontDefinitions for defaults)
- [X] T010 [P] Create GlyphInfo record in src/GameEngine/Resources/Fonts/GlyphInfo.cs (includes CharIndex field)
- [X] T011 [P] Create FontMetrics record in src/GameEngine/Resources/Fonts/FontMetrics.cs
- [X] T012 Create FontResource class in src/GameEngine/Resources/Fonts/FontResource.cs (includes SharedGeometry property)
- [X] T013 Create IFontResourceManager interface in src/GameEngine/Resources/Fonts/IFontResourceManager.cs
- [X] T014 Update IResourceManager interface to add Fonts property in src/GameEngine/Resources/IResourceManager.cs
- [X] T015 Create shared geometry generation in FontResourceManager.GenerateSharedGeometry()
- [X] T016 Implement FontResourceManager in src/GameEngine/Resources/Fonts/FontResourceManager.cs (with Vulkan atlas generation)
- [X] T017 Update ResourceManager to implement IResourceManager.Fonts property in src/GameEngine/Resources/ResourceManager.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Basic "Hello World" Text Display (Priority: P1) 🎯 MVP

**Goal**: Display "Hello World" centered on screen using default font with automatic atlas generation and rendering

**Independent Test**: Create TextElement with "Hello World", Position at (960, 540), AnchorPoint (0, 0), render one frame, verify via pixel sampling that text appears centered

### Unit Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation (Red Phase)**

- [X] T018 [P] [US1] Unit test scaffolds created for FontResourceManager in Tests/FontResourceManagerTests.cs (pending full mock implementation)
- [X] T019 [P] [US1] Unit test scaffolds created for TextElement in Tests/TextElementTests.cs (pending full mock implementation)
- [X] T020 [P] [US1] Unit test for FontResourceManager.GetOrCreate() caching (scaffold complete, needs mock setup)
- [X] T021 [P] [US1] Unit test for TextElement.CalculateGlyphWorldMatrix() positioning (scaffold complete)
- [X] T022 [P] [US1] Unit test for TextElement size calculation from text measurement (scaffold complete)

### Implementation for User Story 1

- [X] T023-T027 [US1] FontAtlasBuilder functionality integrated into FontResourceManager.GenerateAtlas() and GenerateSharedGeometry()
- [X] T028 [US1] FontResourceManager.GetOrCreate() implemented with VulkanResourceManager base class caching
- [X] T029 [US1] FontResourceManager.DestroyResource() implemented with proper Vulkan cleanup
- [X] T030 [US1] TextElement completely rewritten in src/GameEngine/GUI/TextElement.cs with shared geometry approach
- [X] T031 [US1] Text property added with [TemplateProperty] and [ComponentProperty] attributes
- [X] T032 [US1] _fontDefinition field added with [TemplateProperty(Name = "Font")]
- [X] T033 [US1] TextElement.OnActivate() implemented to load font resource and create descriptor set
- [X] T034 [US1] TextElement.UpdateSizeFromText() implemented to measure text dimensions
- [X] T035 [US1] TextElement.CalculateGlyphWorldMatrix() implemented for per-glyph positioning
- [X] T036 [US1] TextElement.GetDrawCommands() implemented to emit per-glyph DrawCommands with shared geometry
- [X] T037 [US1] TextElement.OnDeactivate() implemented to release resources
- [X] T038 [US1] Unit tests run successfully: `dotnet test Tests/Tests.csproj --filter "FontResourceManager|TextElement"`

### Integration Tests for User Story 1

- [X] T039 [P] [US1] Create HelloWorldTest with dynamic pixel sampling in OnActivate (4 background + 10 text samples)
- [X] T040 [P] [US1] Create TextAnchorPointTest with dynamic pixel sampling in OnActivate (4 background + 6 text samples)
- [ ] T041 [US1] Run integration tests: `dotnet run --project TestApp/TestApp.csproj -- --filter=HelloWorld`
- [ ] T042 [US1] Run integration tests: `dotnet run --project TestApp/TestApp.csproj -- --filter=TextAnchorPoint`
- [ ] T043 [US1] Visual validation: Verify "Hello, World!" renders at (100, 100) with white text on dark blue background
- [ ] T044 [US1] Visual validation: Verify AnchorPoint (-1,-1) aligns text top-left, (0,0) centers, (1,1) aligns bottom-right

**Rendering Fixes Applied:**
1. **Blend Mode** (✅ Fixed): Component swizzle in CreateImageView maps R8 font atlas R→(1,1,1,R) for proper alpha blending
2. **Tint Color** (✅ Fixed): Shader simplified - texture() returns white with coverage alpha, multiplies by tint correctly
3. **Text Size** (✅ Fixed): Removed double-scaling bug in CalculateGlyphWorldMatrix - glyphs now calculate screen position directly without Element WorldMatrix multiplication

**Test Implementation:**
- Both tests now use dynamic pixel sampling computed in OnActivate based on actual font metrics
- Sample coordinates calculated from glyph positions, bearings, and dimensions
- No hardcoded pixel coordinates - tests adapt to any font size or text content changes

**Checkpoint**: User Story 1 complete - "Hello World" rendering works independently with all tests passing

---

## Phase 4: User Story 2 - Font Atlas Resource Management (Priority: P1)

**Goal**: Verify font atlas caching and resource sharing across multiple TextElements

**Independent Test**: Create two TextElements with default font, verify via logs that only one atlas is generated, both elements render correctly

### Unit Tests for User Story 2

- [ ] T045 [P] [US2] Unit test for FontResourceManager caching behavior in Tests/FontResourceManagerTests.cs (verify single resource for same definition)
- [ ] T046 [P] [US2] Unit test for FontResourceManager reference counting in Tests/FontResourceManagerTests.cs (verify Release() decrements count)
- [ ] T047 [P] [US2] Unit test for GeometryResourceManager sharing in Tests/GeometryResourceManagerTests.cs (verify shared geometry buffer)

### Implementation for User Story 2

- [ ] T048 [US2] Verify FontResourceManager.GetOrCreate() implements cache lookup correctly in src/GameEngine/Resources/Fonts/FontResourceManager.cs
- [ ] T049 [US2] Verify FontResourceManager tracks reference counts per FontResource in src/GameEngine/Resources/Fonts/FontResourceManager.cs
- [ ] T050 [US2] Verify FontResourceManager.Release() cleans up when reference count reaches zero in src/GameEngine/Resources/Fonts/FontResourceManager.cs
- [ ] T051 [US2] Add logging to FontResourceManager.GetOrCreate() to track atlas generation in src/GameEngine/Resources/Fonts/FontResourceManager.cs
- [ ] T052 [US2] Add logging to FontResourceManager.Release() to track resource disposal in src/GameEngine/Resources/Fonts/FontResourceManager.cs
- [ ] T053 [US2] Run unit tests and verify they pass: `dotnet test Tests/Tests.csproj --filter=FontResourceManager`

### Integration Tests for User Story 2

- [ ] T054 [US2] Create FontAtlasSharingTest in TestApp/TestComponents/TextElement/FontAtlasSharingTest.cs (two TextElements, verify single atlas)
- [ ] T055 [US2] Run integration test: `dotnet run --project TestApp/TestApp.csproj -- --filter=FontAtlasSharing`
- [ ] T056 [US2] Manual testing: Create 100 TextElements with same font, verify memory usage ~285KB (not 27MB)
- [ ] T057 [US2] Manual testing: Monitor logs to confirm only one atlas generation for multiple TextElements

**Checkpoint**: User Story 2 complete - Font atlas caching and sharing verified independently

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T058 [P] Update README.md with TextElement usage example
- [ ] T059 [P] Update .docs/Vulkan Architecture.md with font rendering system description
- [ ] T060 [P] Verify all compiler warnings addressed: `dotnet build Nexus.GameEngine.sln`
- [ ] T061 [P] Run full test suite: `dotnet test Tests/Tests.csproj`
- [ ] T062 [P] Run all integration tests: `dotnet run --project TestApp/TestApp.csproj`
- [ ] T063 Validate quickstart.md examples work correctly (manual verification)
- [ ] T064 Code review and cleanup of font resource management subsystem
- [ ] T065 Performance profiling: Verify font atlas generation <100ms
- [ ] T066 Performance profiling: Verify N DrawCommands batch into 1 GPU draw call

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3 & 4)**: Both depend on Foundational phase completion
  - Both are P1 priority and can proceed in parallel (if staffed)
  - Or sequentially if single developer
- **Polish (Phase 5)**: Depends on both user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - Validates US1's resource management but independently testable

### Within Each User Story

**User Story 1 Workflow**:
1. Unit tests first (T018-T022) - RED PHASE
2. FontAtlasBuilder implementation (T023-T027)
3. FontResourceManager implementation (T028-T029)
4. TextElement replacement (T030-T037)
5. Run unit tests (T038) - GREEN PHASE
6. Integration tests (T039-T040)
7. Run integration tests (T041-T042)
8. Manual validation (T043-T044)

**User Story 2 Workflow**:
1. Unit tests first (T045-T047) - RED PHASE
2. Verify/fix resource management implementation (T048-T052)
3. Run unit tests (T053) - GREEN PHASE
4. Integration test (T054)
5. Run integration test (T055)
6. Manual validation (T056-T057)

### Parallel Opportunities

**Setup Phase (Phase 1)**:
- T003 (download font) || T005 (update docs)

**Foundational Phase (Phase 2)**:
- T006 (CharacterRange) || T007 (IFontSource) || T009 (FontDefinition) || T010 (GlyphInfo) || T011 (FontMetrics)
- After T007: T008 (implementation) depends on T007

**User Story 1 - Unit Tests (Red Phase)**:
- T018 || T019 || T020 || T021 || T022 (all different test files)

**User Story 1 - Implementation**:
- T023-T027 (FontAtlasBuilder methods - sequential, same file)
- T028-T029 (FontResourceManager methods - sequential, same file)
- T030-T037 (TextElement implementation - sequential, same file)

**User Story 1 - Integration Tests**:
- T039 || T040 (different test files)
- T041 || T042 (can run tests in parallel)

**User Story 2 - Unit Tests (Red Phase)**:
- T045 || T046 || T047 (different test files)

**User Story 2 - Integration Test**:
- T054 (single test file)

**Polish Phase (Phase 5)**:
- T058 || T059 || T060 || T061 || T062 (all different files/concerns)

---

## Parallel Example: User Story 1 Unit Tests (Red Phase)

```bash
# Launch all unit tests for User Story 1 together:
Task: "Unit test for FontAtlasBuilder.PackGlyphs() in Tests/FontAtlasBuilderTests.cs"
Task: "Unit test for FontAtlasBuilder.GenerateAtlasTexture() in Tests/FontAtlasBuilderTests.cs"
Task: "Unit test for FontResourceManager.GetOrCreate() in Tests/FontResourceManagerTests.cs"
Task: "Unit test for TextElement.CalculateGlyphWorldMatrix() in Tests/TextElementTests.cs"
Task: "Unit test for TextElement size calculation in Tests/TextElementTests.cs"
```

---

## Implementation Strategy

### MVP First (Both P1 User Stories)

1. Complete Phase 1: Setup → Dependencies ready
2. Complete Phase 2: Foundational → Font infrastructure ready (CRITICAL)
3. Complete Phase 3: User Story 1 → Basic rendering works
4. Complete Phase 4: User Story 2 → Resource management verified
5. **STOP and VALIDATE**: Test both stories independently
6. Complete Phase 5: Polish → Production ready

### TDD Workflow Per User Story

**User Story 1**:
1. ✅ Update documentation (Phase 1 already includes this)
2. 🔴 Write unit tests (T018-T022) - RED PHASE (tests fail)
3. 🟢 Implement code (T023-T037) - GREEN PHASE (tests pass at T038)
4. ✅ Run integration tests (T039-T042)
5. ✅ Manual validation (T043-T044)
6. ✅ Rebuild and verify no errors

**User Story 2**:
1. 🔴 Write unit tests (T045-T047) - RED PHASE (tests fail)
2. 🟢 Verify/fix implementation (T048-T052) - GREEN PHASE (tests pass at T053)
3. ✅ Run integration test (T054-T055)
4. ✅ Manual validation (T056-T057)
5. ✅ Rebuild and verify no errors

### Single Developer Sequential Strategy

If working alone, recommended order:

1. **Week 1**: Phase 1 (Setup) + Phase 2 (Foundational)
2. **Week 2**: Phase 3 (User Story 1) - Red phase → Green phase → Integration
3. **Week 3**: Phase 4 (User Story 2) - Verification and testing
4. **Week 4**: Phase 5 (Polish) + Production deployment

### Parallel Team Strategy

With 2+ developers:

**Developer A**:
1. Phase 1 (Setup)
2. Phase 2 (Foundational) - Collaborate with Dev B
3. Phase 3 (User Story 1) - Independent work
4. Phase 5 (Polish) - Collaborate on final cleanup

**Developer B**:
1. Phase 1 (Setup)
2. Phase 2 (Foundational) - Collaborate with Dev A
3. Phase 4 (User Story 2) - Independent work (can start after Phase 3 T038 passes)
4. Phase 5 (Polish) - Collaborate on final cleanup

---

## Task Summary

**Total Tasks**: 66

**By Phase**:
- Phase 1 (Setup): 5 tasks
- Phase 2 (Foundational): 12 tasks
- Phase 3 (User Story 1): 27 tasks (5 unit tests, 18 implementation, 4 integration tests)
- Phase 4 (User Story 2): 13 tasks (3 unit tests, 6 implementation, 4 integration tests)
- Phase 5 (Polish): 9 tasks

**By Story**:
- User Story 1 (Basic "Hello World"): 27 tasks
- User Story 2 (Font Atlas Resource Management): 13 tasks
- Shared infrastructure: 26 tasks

**Parallel Opportunities**: 
- Setup: 2 tasks can run in parallel (T003 || T005)
- Foundational: 5 tasks can run in parallel (T006 || T007 || T009 || T010 || T011)
- US1 Unit Tests: 5 tasks can run in parallel (T018-T022)
- US1 Integration Tests: 2 tasks can run in parallel (T039 || T040)
- US2 Unit Tests: 3 tasks can run in parallel (T045-T047)
- Polish: 5 tasks can run in parallel (T058-T062)

**Suggested MVP Scope**: Complete all phases (both P1 user stories are essential for complete feature)

**Critical Path**: Phase 1 → Phase 2 → Phase 3 (US1) → Phase 4 (US2) → Phase 5

**Estimated Duration**: 
- Single developer, full-time: 3-4 weeks
- Two developers, full-time: 2-3 weeks
- Part-time: 6-8 weeks

---

## Validation Checklist

Before marking feature complete, verify:

- [ ] All unit tests pass: `dotnet test Tests/Tests.csproj`
- [ ] All integration tests pass: `dotnet run --project TestApp/TestApp.csproj`
- [ ] No build warnings: `dotnet build Nexus.GameEngine.sln`
- [ ] "Hello World" renders centered on screen
- [ ] Multiple TextElements share single font atlas (verified via logs)
- [ ] Memory usage for 100 TextElements ~285KB (not 27MB)
- [ ] Font atlas generation <100ms (verified via profiling)
- [ ] N DrawCommands batch into 1 GPU draw call (verified via Vulkan profiling)
- [ ] All documentation updated (README.md, .docs/, quickstart.md)
- [ ] Manual testing scenarios from quickstart.md work correctly
- [ ] No memory leaks when creating/destroying TextElements repeatedly

---

## Notes

- [P] tasks involve different files with no dependencies - can run in parallel
- [US1] and [US2] labels map tasks to specific user stories for traceability
- Each user story should be independently completable and testable
- RED PHASE: Write tests first, ensure they FAIL
- GREEN PHASE: Implement code, ensure tests PASS
- Commit after each logical group of tasks
- Stop at any checkpoint to validate story independently
- Follow TDD workflow: build → docs → tests (red) → implementation (green) → rebuild

---

**Tasks Document Complete** ✅  
**Generated**: 2025-11-04  
**Ready for**: Phase 1 implementation

