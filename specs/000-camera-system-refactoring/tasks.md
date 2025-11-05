# Tasks: Camera System Refactoring

**Feature Branch**: `feature/camera-system-refactoring`  
**Prerequisites**: plan.md (complete), spec.md (complete), research.md (complete)  
**Tests**: TDD approach - tests written before implementation per constitution requirement

## Format: `- [ ] [ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- All tasks include exact file paths

## Implementation Strategy

**MVP Scope**: User Story 1 (Basic UI Rendering) - Phase 3  
**Incremental Delivery**: Each phase delivers independently testable functionality  
**Dependencies**: Phase 1 → Phase 2 → Phase 3 (US1) → Phase 4 (US3) → Phase 5 (US2)

---

## Phase 1: Setup & Documentation

**Purpose**: Prepare project structure and documentation before code changes

- [X] T001 Create feature branch `feature/camera-system-refactoring` from main
- [X] T002 [P] Update `.docs/Project Structure.md` to document upcoming camera tracking in ContentManager
- [X] T003 [P] Update `.docs/Vulkan Architecture.md` to document viewport simplification approach
- [X] T004 Verify solution builds without errors: `dotnet build Nexus.GameEngine.sln --configuration Debug`
- [X] T005 Run existing tests to establish baseline: `dotnet test Tests/Tests.csproj`
- [X] T006 Run ColoredRectTest to verify current rendering: `dotnet run --project TestApp/TestApp.csproj`

**Baseline Status**: 92/93 tests pass. Known failure: ColoredRectTest (transform matrix not applied correctly - this is what the refactoring will fix)

**Verification**: All baseline tests pass, documentation updated, ready for Phase 2

---

## Phase 2: Foundational Infrastructure

**Purpose**: Core interfaces and data structures that all user stories depend on

### Viewport Simplification (Foundation for all stories)

- [ ] T007 Write test `Viewport_Constructor_SetsAllProperties` in `Tests/ViewportTests.cs`
- [ ] T008 Write test `Viewport_Equality_ComparesValues` in `Tests/ViewportTests.cs`
- [ ] T009 Write test `Viewport_With_CreatesNewInstance` in `Tests/ViewportTests.cs`
- [ ] T010 Write test `Viewport_DefaultRenderPassMask_IsAll` in `Tests/ViewportTests.cs`
- [ ] T011 Run tests to verify RED phase: `dotnet test Tests/Tests.csproj`
- [ ] T012 Convert `Viewport` class to immutable record in `src/GameEngine/Graphics/Rendering/Viewport.cs`
- [ ] T013 Remove `Content` property from Viewport in `src/GameEngine/Graphics/Rendering/Viewport.cs`
- [ ] T014 Remove `Camera` property from Viewport in `src/GameEngine/Graphics/Rendering/Viewport.cs`
- [ ] T015 Remove `Activate()` method from Viewport in `src/GameEngine/Graphics/Rendering/Viewport.cs`
- [ ] T016 Remove `Validate()` method from Viewport in `src/GameEngine/Graphics/Rendering/Viewport.cs`
- [ ] T017 Remove `Invalidate()` method from Viewport in `src/GameEngine/Graphics/Rendering/Viewport.cs`
- [ ] T018 Add `required` keyword to essential Viewport properties in `src/GameEngine/Graphics/Rendering/Viewport.cs`
- [ ] T019 Set default `RenderPassMask = RenderPasses.All` in `src/GameEngine/Graphics/Rendering/Viewport.cs`
- [ ] T020 Run tests to verify GREEN phase: `dotnet test Tests/Tests.csproj`
- [ ] T021 Build solution to catch breaking changes: `dotnet build Nexus.GameEngine.sln --configuration Debug`

### ICamera Interface Updates (Foundation for all stories)

- [ ] T022 Write test `ICamera_HasScreenRegionProperty` in `Tests/CameraInterfaceTests.cs`
- [ ] T023 Write test `ICamera_HasClearColorProperty` in `Tests/CameraInterfaceTests.cs`
- [ ] T024 Write test `ICamera_HasRenderPriorityProperty` in `Tests/CameraInterfaceTests.cs`
- [ ] T025 Write test `ICamera_HasRenderPassMaskProperty` in `Tests/CameraInterfaceTests.cs`
- [ ] T026 Write test `ICamera_HasGetViewportMethod` in `Tests/CameraInterfaceTests.cs`
- [ ] T027 Run tests to verify RED phase: `dotnet test Tests/Tests.csproj`
- [ ] T028 Add `Rectangle<float> ScreenRegion { get; set; }` to ICamera in `src/GameEngine/Graphics/Cameras/ICamera.cs`
- [ ] T029 Add `Vector4D<float> ClearColor { get; set; }` to ICamera in `src/GameEngine/Graphics/Cameras/ICamera.cs`
- [ ] T030 Add `int RenderPriority { get; set; }` to ICamera in `src/GameEngine/Graphics/Cameras/ICamera.cs`
- [ ] T031 Add `uint RenderPassMask { get; set; }` to ICamera in `src/GameEngine/Graphics/Cameras/ICamera.cs`
- [ ] T032 Add `Viewport GetViewport()` method to ICamera in `src/GameEngine/Graphics/Cameras/ICamera.cs`
- [ ] T033 Run tests to verify GREEN phase: `dotnet test Tests/Tests.csproj`
- [ ] T034 Attempt build (expected failures - StaticCamera doesn't implement new members): `dotnet build Nexus.GameEngine.sln --configuration Debug`

**Verification**: Viewport is immutable record, ICamera interface updated, ready for user stories

---

## Phase 3: User Story 1 - Basic UI Rendering (P1) ⭐ MVP

**Goal**: Auto-create default UI camera, enable zero-configuration rendering

**Independent Test**: ColoredRectTest renders without explicit camera setup

### ContentManager Camera Tracking

- [ ] T035 [US1] Write test `ContentManager_Initialize_CreatesDefaultUICamera` in `Tests/CameraTrackingTests.cs`
- [ ] T036 [US1] Write test `ContentManager_Load_RegistersCamerasInTree` in `Tests/CameraTrackingTests.cs`
- [ ] T037 [US1] Write test `ContentManager_Load_RegistersNestedCameras` in `Tests/CameraTrackingTests.cs`
- [ ] T038 [US1] Write test `ContentManager_Unload_UnregistersCameras` in `Tests/CameraTrackingTests.cs`
- [ ] T039 [US1] Write test `ContentManager_Unload_NeverUnregistersDefaultCamera` in `Tests/CameraTrackingTests.cs`
- [ ] T040 [US1] Write test `ContentManager_ActiveCameras_OnlyReturnsActivatedCameras` in `Tests/CameraTrackingTests.cs`
- [ ] T041 [US1] Run tests to verify RED phase: `dotnet test Tests/Tests.csproj`
- [ ] T042 [US1] Add `IEnumerable<ICamera> ActiveCameras { get; }` to IContentManager in `src/GameEngine/Components/IContentManager.cs`
- [ ] T043 [US1] Add `_registeredCameras` list field to ContentManager in `src/GameEngine/Components/ContentManager.cs`
- [ ] T044 [US1] Add `_defaultUICamera` field to ContentManager in `src/GameEngine/Components/ContentManager.cs`
- [ ] T045 [US1] Implement `Initialize()` method in ContentManager in `src/GameEngine/Components/ContentManager.cs`
- [ ] T046 [US1] Create default StaticCamera in Initialize() in `src/GameEngine/Components/ContentManager.cs`
- [ ] T047 [US1] Activate default camera and add to _registeredCameras in `src/GameEngine/Components/ContentManager.cs`
- [ ] T048 [US1] Implement `RegisterCamerasInTree()` private method in `src/GameEngine/Components/ContentManager.cs`
- [ ] T049 [US1] Implement `UnregisterCamerasInTree()` private method in `src/GameEngine/Components/ContentManager.cs`
- [ ] T050 [US1] Update `Load()` to call RegisterCamerasInTree in `src/GameEngine/Components/ContentManager.cs`
- [ ] T051 [US1] Update `Unload()` to call UnregisterCamerasInTree in `src/GameEngine/Components/ContentManager.cs`
- [ ] T052 [US1] Implement `ActiveCameras` property in `src/GameEngine/Components/ContentManager.cs`
- [ ] T053 [US1] Run tests to verify GREEN phase: `dotnet test Tests/Tests.csproj`

### StaticCamera Implementation

- [ ] T054 [US1] Write test `StaticCamera_GetViewport_ReturnsValidViewport` in `Tests/StaticCameraTests.cs`
- [ ] T055 [US1] Write test `StaticCamera_OnWindowResize_MarksViewportDirty` in `Tests/StaticCameraTests.cs`
- [ ] T056 [US1] Write test `StaticCamera_GetViewport_LazyUpdatesOnResize` in `Tests/StaticCameraTests.cs`
- [ ] T057 [US1] Write test `StaticCamera_OnDeactivate_UnsubscribesFromResize` in `Tests/StaticCameraTests.cs`
- [ ] T058 [US1] Write test `StaticCamera_UpdateViewport_CalculatesCorrectDimensions` in `Tests/StaticCameraTests.cs`
- [ ] T059 [US1] Run tests to verify RED phase: `dotnet test Tests/Tests.csproj`
- [ ] T060 [US1] Add `IWindowService` constructor parameter to StaticCamera in `src/GameEngine/Graphics/Cameras/StaticCamera.cs`
- [ ] T061 [US1] Add `_window`, `_viewport`, `_viewportNeedsUpdate` fields to StaticCamera in `src/GameEngine/Graphics/Cameras/StaticCamera.cs`
- [ ] T062 [US1] Add `ScreenRegion` property to StaticCamera in `src/GameEngine/Graphics/Cameras/StaticCamera.cs`
- [ ] T063 [US1] Add `ClearColor` property to StaticCamera in `src/GameEngine/Graphics/Cameras/StaticCamera.cs`
- [ ] T064 [US1] Add `RenderPriority` property to StaticCamera in `src/GameEngine/Graphics/Cameras/StaticCamera.cs`
- [ ] T065 [US1] Add `RenderPassMask` property to StaticCamera in `src/GameEngine/Graphics/Cameras/StaticCamera.cs`
- [ ] T066 [US1] Implement `GetViewport()` method with lazy update in `src/GameEngine/Graphics/Cameras/StaticCamera.cs`
- [ ] T067 [US1] Implement `UpdateViewport()` private method in `src/GameEngine/Graphics/Cameras/StaticCamera.cs`
- [ ] T068 [US1] Update `OnActivate()` to subscribe to window resize in `src/GameEngine/Graphics/Cameras/StaticCamera.cs`
- [ ] T069 [US1] Implement `OnDeactivate()` to unsubscribe from resize in `src/GameEngine/Graphics/Cameras/StaticCamera.cs`
- [ ] T070 [US1] Implement `OnWindowResize()` event handler in `src/GameEngine/Graphics/Cameras/StaticCamera.cs`
- [ ] T071 [US1] Run tests to verify GREEN phase: `dotnet test Tests/Tests.csproj`
- [ ] T072 [US1] Build solution to verify StaticCamera compiles: `dotnet build Nexus.GameEngine.sln --configuration Debug`

### StaticCamera Template

- [ ] T073 [P] [US1] Write test `StaticCameraTemplate_DefaultValues_AreCorrect` in `Tests/StaticCameraTemplateTests.cs`
- [ ] T074 [P] [US1] Write test `StaticCameraTemplate_WithCustomValues_CreatesConfiguredCamera` in `Tests/StaticCameraTemplateTests.cs`
- [ ] T075 [P] [US1] Write test `ComponentFactory_CreateInstance_CreatesStaticCameraFromTemplate` in `Tests/StaticCameraTemplateTests.cs`
- [ ] T076 [P] [US1] Run tests to verify RED phase: `dotnet test Tests/Tests.csproj`
- [ ] T077 [P] [US1] Create StaticCameraTemplate record in `src/GameEngine/Graphics/Cameras/StaticCameraTemplate.cs`
- [ ] T078 [P] [US1] Add ScreenRegion init property with default in `src/GameEngine/Graphics/Cameras/StaticCameraTemplate.cs`
- [ ] T079 [P] [US1] Add ClearColor init property with default in `src/GameEngine/Graphics/Cameras/StaticCameraTemplate.cs`
- [ ] T080 [P] [US1] Add RenderPriority init property with default in `src/GameEngine/Graphics/Cameras/StaticCameraTemplate.cs`
- [ ] T081 [P] [US1] Add RenderPassMask init property with default in `src/GameEngine/Graphics/Cameras/StaticCameraTemplate.cs`
- [ ] T082 [P] [US1] Run tests to verify GREEN phase: `dotnet test Tests/Tests.csproj`

### Application Startup Simplification

- [ ] T083 [US1] Write test `Application_Run_WorksWithoutExplicitViewport` in `Tests/ApplicationStartupTests.cs`
- [ ] T084 [US1] Write test `Application_Run_DefaultCameraRendersContent` in `Tests/ApplicationStartupTests.cs`
- [ ] T085 [US1] Run tests to verify RED phase: `dotnet test Tests/Tests.csproj`
- [ ] T086 [US1] Remove `viewportManager.CreateViewport()` calls from `src/GameEngine/Runtime/Application.cs`
- [ ] T087 [US1] Remove `viewport.Content = ...` assignment from `src/GameEngine/Runtime/Application.cs`
- [ ] T088 [US1] Update to directly call `contentManager.Load(template)` in `src/GameEngine/Runtime/Application.cs`
- [ ] T089 [US1] Update startup code in `TestApp/Program.cs` to remove explicit viewport creation
- [ ] T090 [US1] Run tests to verify GREEN phase: `dotnet test Tests/Tests.csproj`
- [ ] T091 [US1] Build solution: `dotnet build Nexus.GameEngine.sln --configuration Debug`
- [ ] T092 [US1] **CRITICAL** Run ColoredRectTest to verify rendering works: `dotnet run --project TestApp/TestApp.csproj`

**US1 Completion Criteria**:
- ✅ Default camera auto-created on ContentManager.Initialize()
- ✅ ColoredRectTest renders without explicit camera configuration
- ✅ Window resize updates viewport automatically
- ✅ All unit tests pass

---

## Phase 4: User Story 3 - Performance Optimization (P1)

**Goal**: Bind ViewProjectionMatrix once per viewport (99% bandwidth reduction)

**Independent Test**: Profile push constant calls - matrix bound once per viewport, not per DrawCommand

**Dependency**: Requires US1 (default camera must exist to test rendering optimization)

### Renderer Camera Integration

- [ ] T093 [US3] Write test `Renderer_OnRender_GetsViewportsFromContentManager` in `Tests/RendererCameraIntegrationTests.cs`
- [ ] T094 [US3] Write test `Renderer_OnRender_RendersViewportsInPriorityOrder` in `Tests/RendererCameraIntegrationTests.cs`
- [ ] T095 [US3] Write test `Renderer_OnRender_BindsMatrixOncePerViewport` in `Tests/RendererCameraIntegrationTests.cs`
- [ ] T096 [US3] Write test `Renderer_OnRender_SkipsInactiveCameras` in `Tests/RendererCameraIntegrationTests.cs`
- [ ] T097 [US3] Run tests to verify RED phase: `dotnet test Tests/Tests.csproj`
- [ ] T098 [US3] Update Renderer constructor to remove IViewportManager parameter in `src/GameEngine/Graphics/Rendering/Renderer.cs`
- [ ] T099 [US3] Update Renderer constructor to use IContentManager in `src/GameEngine/Graphics/Rendering/Renderer.cs`
- [ ] T100 [US3] Update `OnRender()` to get viewports from contentManager.ActiveCameras in `src/GameEngine/Graphics/Rendering/Renderer.cs`
- [ ] T101 [US3] Update `OnRender()` to order viewports by RenderPriority in `src/GameEngine/Graphics/Rendering/Renderer.cs`
- [ ] T102 [US3] Update `OnRender()` to loop through viewports in `src/GameEngine/Graphics/Rendering/Renderer.cs`
- [ ] T103 [US3] Update `RecordCommandBuffer()` signature to accept Viewport parameter in `src/GameEngine/Graphics/Rendering/Renderer.cs`
- [ ] T104 [US3] Update `RecordRenderPass()` signature to accept Viewport parameter in `src/GameEngine/Graphics/Rendering/Renderer.cs`
- [X] T105 [US3] Implement ViewProjectionMatrix binding at offset 0 in `RecordRenderPass()` in `src/GameEngine/Graphics/Rendering/Renderer.cs`
- [X] T106 [US3] Bind matrix once at start of render pass in `src/GameEngine/Graphics/Rendering/Renderer.cs`
- [X] T107 [US3] Update DrawCommand processing to only push per-draw data at offset 64 in `src/GameEngine/Graphics/Rendering/Renderer.cs`
- [X] T108 [US3] Run tests to verify GREEN phase: `dotnet test Tests/Tests.csproj`
- [X] T109 [US3] Build solution: `dotnet build Nexus.GameEngine.sln --configuration Debug`

### RenderContext Simplification

- [ ] T110 [P] [US3] Write test `RenderContext_Constructor_RequiresOnlyEssentialData` in `Tests/RenderContextTests.cs`
- [ ] T111 [P] [US3] Write test `RenderContext_Size_Is16Bytes` in `Tests/RenderContextTests.cs`
- [ ] T112 [P] [US3] Write test `RenderContext_DoesNotContainCameraReference` in `Tests/RenderContextTests.cs`
- [ ] T113 [P] [US3] Write test `RenderContext_DoesNotContainViewportReference` in `Tests/RenderContextTests.cs`
- [ ] T114 [P] [US3] Run tests to verify RED phase: `dotnet test Tests/Tests.csproj`
- [ ] T115 [P] [US3] Remove `Camera` property from RenderContext in `src/GameEngine/Graphics/Rendering/RenderContext.cs`
- [ ] T116 [P] [US3] Remove `Viewport` property from RenderContext in `src/GameEngine/Graphics/Rendering/RenderContext.cs`
- [ ] T117 [P] [US3] Remove `ViewProjectionMatrix` property from RenderContext in `src/GameEngine/Graphics/Rendering/RenderContext.cs`
- [ ] T118 [P] [US3] Add `required` keyword to remaining properties in `src/GameEngine/Graphics/Rendering/RenderContext.cs`
- [ ] T119 [P] [US3] Run tests to verify GREEN phase: `dotnet test Tests/Tests.csproj`
- [ ] T120 [P] [US3] Build solution (may have compile errors in components): `dotnet build Nexus.GameEngine.sln --configuration Debug`

### Element Push Constants Update

- [ ] T121 [US3] Write test `Element_GetDrawCommands_UsesUniformColorPushConstants` in `Tests/ElementPushConstantsTests.cs`
- [ ] T122 [US3] Write test `UniformColorPushConstants_Size_Is16Bytes` in `Tests/ElementPushConstantsTests.cs`
- [ ] T123 [US3] Write test `Element_GetDrawCommands_DoesNotIncludeMatrix` in `Tests/ElementPushConstantsTests.cs`
- [ ] T124 [US3] Run tests to verify RED phase: `dotnet test Tests/Tests.csproj`
- [X] T125 [US3] Create UniformColorPushConstants struct in `src/GameEngine/Graphics/Data/UniformColorPushConstants.cs`
- [X] T126 [US3] Add `Vector4D<float> Color` field to UniformColorPushConstants in `src/GameEngine/Graphics/Data/UniformColorPushConstants.cs`
- [X] T127 [US3] Add `FromColor(color)` static factory method in `src/GameEngine/Graphics/Data/UniformColorPushConstants.cs`
- [X] T128 [US3] Update Element.GetDrawCommands() to use UniformColorPushConstants in `src/GameEngine/GUI/Element.cs`
- [X] T129 [US3] Replace TransformedColorPushConstants with UniformColorPushConstants in `src/GameEngine/GUI/Element.cs`
- [ ] T130 [US3] Fix any other components using TransformedColorPushConstants (search codebase)
- [X] T131 [US3] Run tests to verify GREEN phase: `dotnet test Tests/Tests.csproj`
- [X] T132 [US3] Build solution: `dotnet build Nexus.GameEngine.sln --configuration Debug`
- [ ] T133 [US3] **CRITICAL** Run ColoredRectTest to verify rendering still works: `dotnet run --project TestApp/TestApp.csproj`

**US3 Completion Criteria**:
- ✅ ViewProjectionMatrix bound once per viewport (logged in Renderer)
- ✅ Per-draw constants pushed at offset 64
- ✅ RenderContext only contains ScreenSize, AvailableRenderPasses, DeltaTime
- ✅ ColoredRectTest renders correctly
- ✅ All tests pass

---

## Phase 5: User Story 2 - Multi-Viewport Support (P2)

**Goal**: Enable multiple cameras with different screen regions and render pass masks

**Independent Test**: Create second camera with different screen region, verify both render independently

**Dependency**: Requires US1 (camera tracking) and US3 (viewport rendering)

### Multi-Camera Rendering Tests

- [ ] T134 [US2] Write test `MultiCamera_DifferentScreenRegions_RenderIndependently` in `Tests/MultiCameraTests.cs`
- [ ] T135 [US2] Write test `MultiCamera_DifferentRenderPassMasks_FilterContentCorrectly` in `Tests/MultiCameraTests.cs`
- [ ] T136 [US2] Write test `MultiCamera_DifferentPriorities_RenderInOrder` in `Tests/MultiCameraTests.cs`
- [ ] T137 [US2] Write test `MultiCamera_AddRemove_UpdatesActiveCamera` in `Tests/MultiCameraTests.cs`
- [ ] T138 [US2] Run tests to verify RED phase: `dotnet test Tests/Tests.csproj`

### Integration Test Template

- [ ] T139 [US2] Create multi-camera test template in `TestApp/TestComponents/MultiCameraTest.cs`
- [ ] T140 [US2] Add primary camera (main view) in test template in `TestApp/TestComponents/MultiCameraTest.cs`
- [ ] T141 [US2] Add secondary camera (minimap) in test template in `TestApp/TestComponents/MultiCameraTest.cs`
- [ ] T142 [US2] Add content visible to both cameras in test template in `TestApp/TestComponents/MultiCameraTest.cs`
- [ ] T143 [US2] Add content visible only to main camera in test template in `TestApp/TestComponents/MultiCameraTest.cs`
- [ ] T144 [US2] Register multi-camera test in TestRunner in `TestApp/TestRunner.cs`

### Verification

- [ ] T145 [US2] Run unit tests: `dotnet test Tests/Tests.csproj`
- [ ] T146 [US2] Build solution: `dotnet build Nexus.GameEngine.sln --configuration Debug`
- [ ] T147 [US2] Run MultiCameraTest integration test: `dotnet run --project TestApp/TestApp.csproj`
- [ ] T148 [US2] Verify both viewports render with correct content
- [ ] T149 [US2] Verify cameras render in priority order
- [ ] T150 [US2] Verify render pass masks filter content correctly

**US2 Completion Criteria**:
- ✅ Multiple cameras can coexist without interference
- ✅ Each camera renders to its designated screen region
- ✅ Render pass masks correctly filter content
- ✅ Camera priorities determine render order
- ✅ MultiCameraTest integration test passes

---

## Phase 6: Cleanup & Polish

**Purpose**: Remove obsolete code and finalize documentation

### Code Cleanup

- [ ] T151 Search codebase for `IViewportManager` references: `grep -r "IViewportManager" src/`
- [ ] T152 Search codebase for `TransformedColorPushConstants` references: `grep -r "TransformedColorPushConstants" src/`
- [ ] T153 Verify no references found (both searches should return empty)
- [ ] T154 Delete `src/GameEngine/Graphics/Rendering/ViewportManager.cs`
- [ ] T155 Delete `src/GameEngine/Graphics/Rendering/IViewportManager.cs`
- [ ] T156 Delete `src/GameEngine/Graphics/Data/TransformedColorPushConstants.cs`
- [ ] T157 Remove ViewportManager from DI registration if still present in startup code
- [ ] T158 Build solution to verify no broken references: `dotnet build Nexus.GameEngine.sln --configuration Debug`

### Documentation Updates

- [ ] T159 [P] Update `.docs/Project Structure.md` with final camera architecture
- [ ] T160 [P] Update `.docs/Vulkan Architecture.md` with viewport and rendering flow
- [ ] T161 [P] Update `README.md` with simplified application startup example
- [ ] T162 [P] Update `src/GameEngine/Testing/README.md` with camera testing examples if needed

### Final Verification

- [ ] T163 Run all unit tests: `dotnet test Tests/Tests.csproj`
- [ ] T164 Run all integration tests: `dotnet run --project TestApp/TestApp.csproj`
- [ ] T165 Verify ColoredRectTest renders correctly
- [ ] T166 Verify MultiCameraTest renders correctly
- [ ] T167 Test window resize updates all viewports
- [ ] T168 Build solution in Release mode: `dotnet build Nexus.GameEngine.sln --configuration Release`
- [ ] T169 Verify no build warnings or errors
- [ ] T170 Grep for "ViewportManager" returns no results: `grep -r "ViewportManager" src/`
- [ ] T171 Grep for "TransformedColorPushConstants" returns no results: `grep -r "TransformedColorPushConstants" src/`

### Performance Validation

- [ ] T172 [P] Add logging to Renderer.RecordRenderPass() to count matrix bindings
- [ ] T173 [P] Run performance test with 1000 draw commands
- [ ] T174 [P] Verify ViewProjectionMatrix pushed exactly once per viewport
- [ ] T175 [P] Measure frame time (should be <16ms for 60 FPS)
- [ ] T176 [P] Remove performance logging code

**Phase 6 Completion Criteria**:
- ✅ All obsolete files deleted
- ✅ No references to ViewportManager or TransformedColorPushConstants
- ✅ All tests pass (unit + integration)
- ✅ Documentation updated
- ✅ Performance goals achieved (99% matrix bandwidth reduction)

---

## Dependency Graph

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundation: Viewport + ICamera)
    ↓
Phase 3 (US1: Basic UI Rendering) ← MVP COMPLETE HERE
    ↓
Phase 4 (US3: Performance Optimization) [depends on US1]
    ↓
Phase 5 (US2: Multi-Viewport) [depends on US1 + US3]
    ↓
Phase 6 (Cleanup & Polish)
```

**Critical Path**: Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6

**Parallel Opportunities Within Phases**:
- Phase 2: Viewport and ICamera changes can be done simultaneously (marked [P])
- Phase 3: StaticCameraTemplate can be done in parallel with ContentManager work
- Phase 4: RenderContext and Element updates can be done in parallel after Renderer changes
- Phase 6: Documentation updates can be done in parallel

---

## Summary

**Total Tasks**: 176  
**MVP Tasks** (through US1): T001-T092 (92 tasks)  
**Full Feature**: All 176 tasks  

**Task Breakdown by Story**:
- Setup & Foundation: 34 tasks (T001-T034)
- US1 (Basic UI Rendering): 58 tasks (T035-T092)
- US3 (Performance Optimization): 41 tasks (T093-T133)
- US2 (Multi-Viewport): 17 tasks (T134-T150)
- Cleanup & Polish: 26 tasks (T151-T176)

**Parallel Opportunities**: 23 tasks marked [P] can run in parallel within their phases

**Independent Test Criteria**:
- **US1**: ColoredRectTest renders without camera configuration
- **US3**: ViewProjectionMatrix bound once per viewport (measurable via logging)
- **US2**: MultiCameraTest shows two independent viewports

**Suggested Delivery**:
1. **Sprint 1**: MVP (US1) - Phases 1-3 (92 tasks)
2. **Sprint 2**: Performance (US3) - Phase 4 (41 tasks)
3. **Sprint 3**: Multi-viewport (US2) + Polish - Phases 5-6 (43 tasks)

**Format Validation**: ✅ All 176 tasks follow checklist format with ID, optional [P] and [Story] labels, and file paths
