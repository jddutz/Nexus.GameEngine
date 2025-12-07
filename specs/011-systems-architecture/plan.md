# Implementation Plan: Systems Architecture Refactoring

**Branch**: `feature/011-systems-architecture` | **Date**: December 6, 2025 | **Spec**: [specs/011-systems-architecture/spec.md](../../011-systems-architecture/spec.md)
**Input**: Feature specification from `/specs/011-systems-architecture/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Introduce Systems pattern to eliminate constructor injection bloat in framework classes. Framework services (graphics, resources, content, input, window) will be accessed via strongly-typed system properties instead of constructor parameters. Uses empty marker interfaces with extension methods for zero coupling and excellent IntelliSense discoverability. Reduces constructor parameter counts from 5-9 to 0-2 for framework classes while maintaining compile-time type safety and testability.

## Technical Context

**Language/Version**: C# 9.0+ with nullable reference types enabled  
**Primary Dependencies**: .NET 9.0, Microsoft.Extensions.DependencyInjection, Silk.NET (Vulkan bindings)  
**Storage**: N/A (architectural refactoring only)  
**Testing**: xUnit for unit tests, frame-based integration tests via TestApp  
**Target Platform**: Windows (primary), cross-platform .NET 9.0 compatible  
**Project Type**: Game engine framework (single solution, multiple projects)  
**Performance Goals**: Zero performance degradation from current implementation, 60+ fps maintained  
**Constraints**: Extension method calls must compile to identical IL as instance methods (verified), no boxing/allocation in hot paths  
**Scale/Scope**: ~50 framework classes to refactor, 5 system interfaces, ~100+ extension methods across all systems

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Documentation-First TDD ✅ PASS

- **Requirement**: Update documentation before code, write failing tests, implement to green
- **Status**: This is architectural refactoring - documentation will be updated first (`.docs/Systems Architecture.md`), unit tests will verify behavior preservation, integration tests will validate no functional changes
- **Action Required**: None - standard TDD workflow applies

### II. Component-Based Architecture ✅ PASS

- **Requirement**: All game engine systems follow component-based architecture with IRuntimeComponent
- **Status**: This refactoring targets framework infrastructure classes (Renderer, PipelineManager, etc.), NOT components. Components already use parameterless constructors and don't inject framework services.
- **Action Required**: None - component architecture unchanged

### III. Source-Generated Animated Properties ✅ PASS

- **Requirement**: Properties requiring animation use [ComponentProperty] attribute system
- **Status**: This refactoring doesn't introduce new properties requiring animation. Existing component properties remain unchanged.
- **Action Required**: None - property generation system unchanged

### IV. Vulkan Resource Management ✅ PASS

- **Requirement**: All Vulkan resources managed through IResourceManager, IGeometryResourceManager, etc.
- **Status**: Systems pattern wraps existing resource managers with IResourceSystem. Resource management patterns remain unchanged.
- **Action Required**: None - resource management unchanged, only access pattern changes

### V. Explicit Approval Required ✅ PASS

- **Requirement**: Do not change code without explicit instructions
- **Status**: This spec has explicit approval to refactor framework classes to use systems pattern. Changes are well-scoped and documented.
- **Action Required**: None - spec provides clear authorization

### Architecture Constraints ✅ PASS

- **Technology Stack**: No new technologies introduced - uses existing C# 9.0+, .NET 9.0, Microsoft.Extensions.DependencyInjection
- **Application Startup**: DI registration will add system singletons, but existing service registration remains unchanged
- **Performance Standards**: Extension methods compile to identical IL as instance methods - zero overhead verified

### Testing Infrastructure ✅ PASS

- **Unit Tests**: Will use mocked system interfaces (IGraphicsSystem, etc.) instead of mocking individual services
- **Integration Tests**: Existing frame-based tests should pass unchanged after refactoring
- **Test Coverage**: Target 80% coverage for new system implementation classes

### Post-Design Re-evaluation

**Design artifacts reviewed**:
- ✅ research.md: All technical decisions validated (property initialization, extension organization, testing, performance, migration)
- ✅ data-model.md: Entity definitions follow existing patterns (marker interfaces, internal sealed implementations, extension methods)
- ✅ contracts/: System interfaces are empty markers with clear documentation
- ✅ quickstart.md: Usage patterns align with framework conventions

**Constitution compliance verified**:
- ✅ Systems pattern uses existing DI container (no new dependencies)
- ✅ Testing approach uses standard xUnit patterns with mock implementations
- ✅ Performance verified through IL inspection and benchmarking research
- ✅ Documentation-first approach followed (this planning phase precedes implementation)
- ✅ Component architecture remains unchanged
- ✅ Resource management patterns unchanged
- ✅ Explicit approval documented in spec

**GATE DECISION**: ✅ **APPROVED - READY FOR PHASE 2 TASKING**

All constitution principles align with systems architecture refactoring after design phase. No violations or concerns identified. Design artifacts demonstrate compliance with all constitutional requirements.

## Project Structure

### Documentation (this feature)

```text
specs/011-systems-architecture/
├── spec.md              # Feature specification (already exists)
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   ├── IGraphicsSystem.cs       # Graphics system marker interface
│   ├── IResourceSystem.cs       # Resource system marker interface
│   ├── IContentSystem.cs        # Content system marker interface
│   ├── IWindowSystem.cs         # Window system marker interface
│   └── IInputSystem.cs          # Input system marker interface
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/GameEngine/
├── Systems/                     # NEW - Systems pattern infrastructure
│   ├── IGraphicsSystem.cs       # Graphics system marker interface
│   ├── IResourceSystem.cs       # Resource system marker interface
│   ├── IContentSystem.cs        # Content system marker interface
│   ├── IWindowSystem.cs         # Window system marker interface
│   ├── IInputSystem.cs          # Input system marker interface
│   ├── GraphicsSystem.cs        # Internal sealed implementation wrapping graphics services
│   ├── ResourceSystem.cs        # Internal sealed implementation wrapping resource services
│   ├── ContentSystem.cs         # Internal sealed implementation wrapping content services
│   ├── WindowSystem.cs          # Internal sealed implementation wrapping window services
│   ├── InputSystem.cs           # Internal sealed implementation wrapping input services
│   └── Extensions/              # Extension methods for each system
│       ├── GraphicsSystemExtensions.cs
│       ├── ResourceSystemExtensions.cs
│       ├── ContentSystemExtensions.cs
│       ├── WindowSystemExtensions.cs
│       └── InputSystemExtensions.cs
├── Graphics/
│   ├── Renderer.cs              # REFACTOR - Remove constructor injection, use systems
│   ├── PipelineManager.cs       # REFACTOR - Remove constructor injection, use systems
│   ├── SwapChain.cs             # REFACTOR - Remove constructor injection, use systems
│   ├── CommandPoolManager.cs    # REFACTOR - Remove constructor injection, use systems
│   ├── DescriptorManager.cs     # REFACTOR - Remove constructor injection, use systems
│   └── SyncManager.cs           # REFACTOR - Remove constructor injection, use systems
├── Resources/
│   ├── ResourceManager.cs       # REFACTOR - Remove constructor injection, use systems
│   ├── BufferManager.cs         # REFACTOR - Remove constructor injection, use systems
│   ├── GeometryResourceManager.cs # REFACTOR - Remove constructor injection, use systems
│   └── ShaderResourceManager.cs # REFACTOR - Remove constructor injection, use systems
├── Content/
│   ├── ContentManager.cs        # REFACTOR - Remove constructor injection, use systems
│   └── ComponentFactory.cs      # REFACTOR - Remove constructor injection, use systems
└── GlobalUsings.cs              # UPDATE - Add system extension namespaces

Tests/GameEngine/
└── Systems/                     # NEW - Unit tests for systems
    ├── GraphicsSystemTests.cs
    ├── ResourceSystemTests.cs
    ├── ContentSystemTests.cs
    ├── WindowSystemTests.cs
    └── InputSystemTests.cs
```

**Structure Decision**: This is a single-project game engine framework following existing Nexus.GameEngine structure. The Systems/ directory is added to src/GameEngine/ to contain system interfaces, implementations, and extension methods. Existing framework classes in Graphics/, Resources/, and Content/ will be refactored to remove constructor injection and use system properties instead. Tests mirror the source structure under Tests/GameEngine/Systems/.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations detected - this section intentionally left empty.
