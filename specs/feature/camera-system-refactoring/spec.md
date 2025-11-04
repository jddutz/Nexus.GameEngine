# Feature Specification: Camera System Refactoring

**Feature Branch**: `feature/camera-system-refactoring`  
**Created**: 2025-11-02  
**Status**: Draft  
**Input**: Simplify camera and viewport architecture for better performance and maintainability

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic UI Rendering (Priority: P1)

Developers creating simple UI applications should have a working camera system without any explicit configuration. The system should automatically provide a default camera that covers the full screen and renders UI elements.

**Why this priority**: This is the foundation of the entire system. Without this, nothing renders. Every application needs at least one camera, and requiring explicit camera setup adds unnecessary complexity for the 90% use case.

**Independent Test**: Can be fully tested by creating a simple UI element (e.g., colored rectangle) without any camera configuration and verifying it renders to screen.

**Acceptance Scenarios**:

1. **Given** a new application with UI elements, **When** the application starts, **Then** UI elements render without requiring explicit camera setup
2. **Given** the default camera is active, **When** window is resized, **Then** viewport updates automatically to maintain full screen coverage
3. **Given** UI elements with different Z-indexes, **When** frame renders, **Then** elements render in correct depth order

---

### User Story 2 - Multi-Viewport Support (Priority: P2)

Developers creating advanced applications (split-screen, minimap, picture-in-picture) should be able to add additional cameras that render to specific screen regions without interfering with the default camera.

**Why this priority**: This enables advanced use cases like game development with minimaps, split-screen multiplayer, or development tools with multiple views. It's additive and doesn't break existing functionality.

**Independent Test**: Can be tested by adding a second camera with a different screen region and verifying both viewports render independently with correct content filtering.

**Acceptance Scenarios**:

1. **Given** multiple cameras with different screen regions, **When** frame renders, **Then** each camera renders its content to its designated viewport
2. **Given** cameras with different render pass masks, **When** frame renders, **Then** each camera only renders content matching its pass mask
3. **Given** cameras with different priorities, **When** frame renders, **Then** cameras render in priority order (lower priority first)

---

### User Story 3 - Performance Optimization (Priority: P1)

The rendering system should minimize redundant data transmission by binding the ViewProjectionMatrix once per viewport instead of per draw command, reducing memory bandwidth usage by ~99%.

**Why this priority**: Performance is critical for game engines. The current architecture wastes 64KB per 1000 draw commands per frame. This directly impacts frame rate and power consumption.

**Independent Test**: Can be measured by profiling push constant bandwidth before and after the change. Success = ViewProjectionMatrix bound once per viewport, not per DrawCommand.

**Acceptance Scenarios**:

1. **Given** a viewport with 1000 draw commands, **When** rendering a frame, **Then** ViewProjectionMatrix is pushed exactly once (at viewport start)
2. **Given** multiple draw commands in a viewport, **When** recording command buffer, **Then** per-draw constants (color, textures) are pushed at offset 64 (after global matrix)
3. **Given** multiple viewports, **When** rendering a frame, **Then** each viewport binds its own ViewProjectionMatrix once

---

## Technical Requirements

### Architecture Changes

1. **Viewport Simplification**:
   - Remove lifecycle methods (Activate, Validate, Invalidate)
   - Remove Content and Camera properties
   - Convert to immutable record with pure Vulkan state
   - Properties: BackgroundColor, VulkanViewport, VulkanScissor, ViewProjectionMatrix, RenderPriority, RenderPassMask

2. **Camera Responsibilities**:
   - Create and manage Viewport instances
   - Subscribe to window resize events
   - Update Viewport when screen dimensions change
   - Cache Viewport and mark dirty on resize

3. **ContentManager Enhancement**:
   - Auto-create default UI camera on initialization
   - Track all cameras via registration/unregistration
   - Provide `ActiveCameras` property (O(1) access)
   - Register cameras during Load() by walking component tree
   - Unregister cameras during Unload() (except default camera)

4. **Renderer Optimization**:
   - Remove ViewportManager dependency
   - Get viewports from ContentManager.ActiveCameras
   - Bind ViewProjectionMatrix once per viewport (push constant offset 0)
   - DrawCommands only contain per-draw data (push constant offset 64+)

5. **RenderContext Simplification**:
   - Remove Camera and Viewport references
   - Remove ViewProjectionMatrix property
   - Keep only: ScreenSize, AvailableRenderPasses, DeltaTime

### Breaking Changes

1. **Viewport class**: Becomes immutable record, removes lifecycle methods
2. **ICamera interface**: Adds ScreenRegion, ClearColor, RenderPriority, RenderPassMask, GetViewport()
3. **IContentManager interface**: Adds ActiveCameras property
4. **Renderer constructor**: Replaces IViewportManager with IContentManager
5. **RenderContext struct**: Removes Camera, Viewport, ViewProjectionMatrix properties
6. **Element.GetDrawCommands()**: Must use UniformColorPushConstants instead of TransformedColorPushConstants
7. **Application.Run()**: No longer requires explicit viewport creation

### Deleted Components

1. **ViewportManager.cs** and **IViewportManager.cs**: Functionality moved to ContentManager and Camera
2. **TransformedColorPushConstants.cs**: Replaced by UniformColorPushConstants (matrix now global)

### New Components

1. **StaticCameraTemplate.cs**: Template record for declarative camera creation
2. **UniformColorPushConstants.cs**: Push constants structure with only color (no matrix)

## Current System Issues

The existing architecture has the following problems:

1. ❌ StaticCamera matrices are all zeros (not initialized properly)
2. ❌ Camera not being activated because it's not in component tree
3. ❌ ViewProjectionMatrix duplicated in every DrawCommand (wasteful: 64 bytes × N commands)
4. ❌ RenderContext exposes Camera/Viewport unnecessarily (leaky abstraction)
5. ❌ ViewportManager adds unnecessary complexity (extra indirection layer)
6. ❌ Would need to walk component tree each frame to find cameras (O(n) inefficient)

## Success Criteria

1. ✅ Default UI camera auto-created on ContentManager initialization
2. ✅ Simple applications render without explicit camera configuration
3. ✅ Developers can add multiple cameras for advanced scenarios
4. ✅ ViewProjectionMatrix bound once per viewport (not per DrawCommand)
5. ✅ RenderContext only exposes necessary data (ScreenSize, RenderPasses, DeltaTime)
6. ✅ Camera access is O(1) via ContentManager.ActiveCameras
7. ✅ Viewport is immutable with no lifecycle complexity
8. ✅ Window resize properly updates all camera viewports
9. ✅ Tests pass: ColoredRectTest renders correctly
10. ✅ Build completes without errors

## Performance Impact

**Before**:
- ViewProjectionMatrix: 64 bytes × 1000 draw commands = 64 KB per frame
- RenderContext: Camera + Viewport + Matrix passed to every GetDrawCommands() call

**After**:
- ViewProjectionMatrix: 64 bytes × 1 per viewport = 64 bytes per frame
- RenderContext: Just screen size + delta time (16 bytes)
- **~99% reduction in matrix data transmission**
