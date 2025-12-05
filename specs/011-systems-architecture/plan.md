# Implementation Plan: Systems Architecture Refactoring

**Branch**: `011-systems-architecture` | **Date**: November 30, 2025 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/011-systems-architecture/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Refactor component architecture to eliminate constructor injection bloat for framework services. Introduce Systems pattern where components access framework capabilities (Graphics, Resources, Window, Input, Content) through strongly-typed property accessors automatically initialized by ComponentFactory. Consolidate component hierarchy from four classes (Entity, Configurable, Component, RuntimeComponent) into single `Component` base class organized with partial files. This improves developer experience through IntelliSense discoverability, simplifies testing with mockable system properties, and reduces cognitive load while maintaining zero breaking changes to component creation API.

## Technical Context

**Language/Version**: C# 9.0+ with nullable reference types enabled  
**Primary Dependencies**: 
- .NET 9.0
- Silk.NET (Vulkan bindings)
- Microsoft.Extensions.DependencyInjection
- Roslyn source generators

**Storage**: N/A (game engine framework)  
**Testing**: 
- xUnit for unit tests (`Tests/Tests.csproj`)
- Frame-based integration tests via TestApp (`TestApp/TestApp.csproj`)
- Moq for mocking dependencies

**Target Platform**: Cross-platform .NET 9.0 (Windows, Linux, macOS)  
**Project Type**: Game engine framework library with test applications  
**Performance Goals**: 
- Zero allocation paths in rendering loops
- System property initialization overhead must not exceed current ActivatorUtilities overhead
- Component creation performance neutral or improved vs current approach

**Constraints**: 
- Maintain backward compatibility for component creation API
- Support incremental migration (components can coexist with old/new patterns)
- Must work with existing source generator infrastructure
- Cannot break existing component lifecycle (Activate, Update, Deactivate)

**Scale/Scope**: 
- 5 core system interfaces (IResourceSystem, IGraphicsSystem, IContentSystem, IWindowSystem, IInputSystem)
- Consolidate 4 base classes (Entity, Configurable, Component, RuntimeComponent) into 1
- ~20-30 drawable components will migrate from constructor injection to system properties
- All new components created post-refactoring

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Documentation-First TDD ✅

**Status**: PASS  
**Compliance**: This plan follows the required workflow:
1. ✅ Build verification before proceeding
2. ✅ Documentation updates planned in Phase 1 (data-model.md, contracts/, quickstart.md)
3. ✅ Test generation planned (unit tests for system initialization, component creation)
4. ✅ Red/Green/Rebuild phases explicit in task workflow
5. ✅ Manual testing instructions will be provided in summary

### II. Component-Based Architecture ✅

**Status**: PASS  
**Compliance**: 
- ✅ All components remain `IRuntimeComponent` implementations
- ✅ Lifecycle preserved (Activate, Update, Deactivate)
- ✅ ComponentFactory continues handling instantiation
- ✅ ContentManager continues managing lifecycle (caching, activation, updates, disposal)
- ✅ Components continue using ContentManager to create children
- **Enhancement**: Systems pattern adds framework service access without changing core architecture

### III. Source-Generated Properties ✅

**Status**: PASS  
**Compliance**:
- ✅ No changes to existing `[ComponentProperty]` or `[TemplateProperty]` attributes
- ✅ Source generators continue working as-is
- ✅ Animated property system unaffected
- **Enhancement**: Systems accessed via properties but not source-generated (manually defined on base class)

### IV. Vulkan Resource Management ✅

**Status**: PASS  
**Compliance**:
- ✅ No changes to resource manager hierarchy
- ✅ IResourceManager, IGeometryResourceManager, IShaderResourceManager, IBufferManager remain unchanged
- ✅ Graphics pipeline (IGraphicsContext, ISwapChain, IPipelineManager, etc.) unaffected
- **Enhancement**: Components access these services via `this.Graphics` and `this.Resources` system properties instead of constructor injection

### V. Explicit Approval Required ✅

**Status**: PASS  
**Compliance**:
- ✅ This plan explicitly requests approval before implementation
- ✅ All code changes will be documented and approved
- ✅ `.temp/agent/` folder will be used for temporary planning artifacts
- ✅ Separate files for each class/interface will be maintained

### Summary

**Overall Status**: ✅ **PASS - No Constitution Violations**

All core principles are preserved. The systems architecture is an additive enhancement that:
- Does not alter component-based architecture fundamentals
- Maintains all existing lifecycle and validation patterns
- Preserves source generator infrastructure
- Keeps resource management architecture intact
- Follows documentation-first TDD workflow

No complexity tracking needed - this is a refactoring that simplifies the existing architecture.

## Project Structure

### Documentation (this feature)

```text
specs/011-systems-architecture/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/GameEngine/
├── Components/
│   ├── Component.cs              # Consolidated base class (was Entity/Configurable/Component/RuntimeComponent)
│   ├── Component.Identity.cs     # Partial: Id, Name properties
│   ├── Component.Configuration.cs # Partial: Load, Configure, Validate
│   ├── Component.Hierarchy.cs    # Partial: Parent, Children management
│   ├── Component.Lifecycle.cs    # Partial: Activate, Update, Deactivate
│   ├── Component.Systems.cs      # NEW: System property declarations
│   ├── ComponentFactory.cs       # Enhanced: Initialize system properties
│   ├── IComponent.cs
│   ├── IRuntimeComponent.cs
│   └── [existing component files]
├── Runtime/
│   ├── Systems/                  # NEW: System interfaces and implementations
│   │   ├── IResourceSystem.cs
│   │   ├── IGraphicsSystem.cs
│   │   ├── IContentSystem.cs
│   │   ├── IWindowSystem.cs
│   │   ├── IInputSystem.cs
│   │   ├── ResourceSystem.cs     # Internal implementation
│   │   ├── GraphicsSystem.cs     # Internal implementation
│   │   ├── ContentSystem.cs      # Internal implementation
│   │   ├── WindowSystem.cs       # Internal implementation
│   │   └── InputSystem.cs        # Internal implementation
│   ├── Extensions/               # NEW: System extension methods
│   │   ├── ResourceSystemExtensions.cs
│   │   ├── GraphicsSystemExtensions.cs
│   │   ├── ContentSystemExtensions.cs
│   │   ├── WindowSystemExtensions.cs
│   │   └── InputSystemExtensions.cs
│   └── ContentManager.cs         # Unchanged
├── GlobalUsings.cs               # Updated: Add system extension namespaces
└── [existing directories]

Tests/GameEngine/
├── Components/
│   ├── Component.Tests.cs        # NEW: Tests for consolidated base class
│   └── ComponentFactory.Tests.cs # Enhanced: Test system initialization
└── Runtime/
    └── Systems/                  # NEW: System tests
        ├── ResourceSystem.Tests.cs
        ├── GraphicsSystem.Tests.cs
        ├── ContentSystem.Tests.cs
        ├── WindowSystem.Tests.cs
        └── InputSystem.Tests.cs

TestApp/
└── [integration test scenarios for system usage]
```

**Structure Decision**: Single project structure maintained. New `Runtime/Systems/` directory introduced for system interfaces and implementations. Extension methods organized in `Runtime/Extensions/`. Component base class split into partial files by concern (identity, configuration, hierarchy, lifecycle, systems) to improve maintainability while consolidating the inheritance hierarchy.

## Complexity Tracking

> **No Constitution Violations - This section intentionally left empty**

This refactoring simplifies the existing architecture and does not introduce complexity that violates constitution principles. All changes are additive and maintain backward compatibility.
