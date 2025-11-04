<!--
Sync Impact Report:
- Version change: INITIAL → 1.0.0
- New constitution created from existing copilot-instructions.md
- Principles extracted:
  1. Documentation-First TDD (NEW)
  2. Component-Based Architecture (NEW)
  3. Source-Generated Properties (NEW)
  4. Resource Management (NEW)
  5. Explicit Approval Required (NEW)
- Templates requiring updates:
  ✅ Updated: All templates inherit these principles
- Follow-up TODOs: None
-->

# Nexus.GameEngine Constitution

## Core Principles

### I. Documentation-First Test-Driven Development (NON-NEGOTIABLE)

Before implementing any feature or change, the development workflow MUST follow this exact sequence:

1. **Build Verification**: Build the solution and verify no compilation errors exist
   - If errors exist, determine whether code or tests need revision
   - Fix all errors before proceeding
2. **Documentation Updates**: Update all relevant documentation BEFORE writing code:
   - `.docs/Project Structure.md`
   - `.docs/Vulkan Architecture.md`
   - `.docs/Deferred Property Generation System.md`
   - `README.md`
   - `src/GameEngine/Testing/README.md`
3. **Test Generation**: Generate unit tests covering all new behaviors
4. **Red Phase**: Confirm new tests fail as expected
5. **Implementation**: Update code to implement changes
6. **Green Phase**: Confirm tests now pass
7. **Rebuild & Verify**: Rebuild project and address all warnings/errors
8. **Summary**: Provide summary of changes and manual testing instructions

**Rationale**: Documentation-first ensures design clarity before implementation. Test-first validates requirements understanding. This sequence prevents technical debt and ensures maintainability.

### II. Component-Based Architecture

All game engine systems MUST follow the component-based architecture pattern:

- Everything is an `IRuntimeComponent` that can contain other components via `Children` property
- Components implement only the behavior interfaces they need (Interface Segregation Principle)
- **Declarative Templates**: Components use strongly-typed template records for configuration
- **Lifecycle Management**: Components support activation, validation, updating, and disposal with automatic child propagation
- **Separation of Concerns**:
  - `IComponentFactory`: Responsible for component instantiation via DI and configuration from templates
  - `IContentManager`: Responsible for content lifecycle (caching, activation, updates, disposal)
- Components MUST use `ContentManager` to create children (never `ComponentFactory` directly)

**Rationale**: Component-based architecture provides modularity, testability, and clear separation of concerns. ContentManager usage ensures proper lifecycle management.

### III. Source-Generated Animated Properties

Properties that require animation or deferred updates MUST use the `[ComponentProperty]` attribute system:

- Mark properties with `[ComponentProperty]` for automatic source generation
- Generated properties support:
  - Deferred updates (changes apply during `ApplyUpdates()` call)
  - Optional interpolation over time (Duration and InterpolationMode parameters)
  - PropertyChanged notifications
  - Animation lifecycle events (AnimationStarted, AnimationEnded)
- **Zero Runtime Overhead**: Type-specific interpolation code generated at compile-time
- **Built-in Type Support**: float, double, int, Silk.NET vectors/matrices automatically supported
- **Array Animation**: Arrays interpolate each element individually with inline loops
- **Extensibility**: Custom types implement `IInterpolatable<T>` for specialized interpolation

**Legacy `QueueUpdate()` method**: Still exists for complex multi-property updates but should be rarely needed.

**Rationale**: Source generation provides type-safe, performant property animation without reflection or boxing. Compile-time code generation eliminates runtime overhead.

### IV. Vulkan Resource Management

All Vulkan resources MUST be managed through centralized resource managers:

- **IResourceManager**: Unified access to all resource types
- **IGeometryResourceManager**: Handles Vulkan vertex/index buffers
- **IShaderResourceManager**: Manages SPIR-V shader modules
- **IBufferManager**: Provides low-level Vulkan buffer allocation
- Resources are cached and reused across components
- Disposal is handled by resource managers (never manual)
- Shaders MUST be compiled to SPIR-V before building (via `compile.bat`)

**Vulkan Rendering Pipeline**:
- `IGraphicsContext`: Core Vulkan infrastructure (instance, device, queues)
- `ISwapChain`: Swap chain management for presentation
- `IPipelineManager`: Graphics pipeline creation and caching with fluent builder API
- `ICommandPoolManager`: Command buffer allocation and management
- `ISyncManager`: Synchronization primitives (semaphores, fences)
- `IDescriptorManager`: Descriptor pool, layout, and set management
- `IRenderer`: High-level rendering orchestration

**Rationale**: Centralized resource management prevents leaks, enables caching, and ensures proper Vulkan object lifecycle. Clear separation of rendering concerns improves maintainability.

### V. Explicit Approval Required (NON-NEGOTIABLE)

**DO NOT change any code without explicit instructions to do so. If not sure, ask first.**

All code modifications require explicit approval before implementation. When uncertain:
- Present options and await user decision
- Explain implications of each approach
- Never assume desired behavior

**File Organization**:
- Use `.temp/agent` folder for temporary files (work instructions, checklists, scripts, progress tracking)
- All classes and interfaces MUST be defined in separate files
- Use dotnet commands for editing CSPROJ and SLN files

**Rationale**: Explicit approval prevents unintended changes and ensures alignment with project goals. Clear file organization improves navigability and maintainability.

## Architecture Constraints

### Technology Stack

- **Language**: C# 9.0+ with nullable reference types enabled
- **Framework**: .NET 9.0
- **Graphics API**: Vulkan (via Silk.NET bindings)
- **Build System**: dotnet CLI
- **Testing**: xUnit for unit tests, frame-based integration tests via TestApp
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Source Generators**: Roslyn source generators for compile-time code generation

### Application Startup Pattern

Services MUST be registered manually (no `AddGameEngineServices` helper exists):

```csharp
var services = new ServiceCollection()
    .Configure<ApplicationSettings>(configuration.GetSection("Application"))
    .Configure<GraphicsSettings>(configuration.GetSection("Graphics"))
    .Configure<VulkanSettings>(configuration.GetSection("Graphics:Vulkan"))
    .AddSingleton<IWindowService, WindowService>()
    .AddSingleton<IValidation, Validation>()
    .AddSingleton<IGraphicsContext, Context>()
    .AddSingleton<ISwapChain, SwapChain>()
    .AddSingleton<ISyncManager, SyncManager>()
    .AddSingleton<ICommandPoolManager, CommandPoolManager>()
    .AddSingleton<IDescriptorManager, DescriptorManager>()
    .AddSingleton<IPipelineManager, PipelineManager>()
    .AddSingleton<IBatchStrategy, DefaultBatchStrategy>()
    .AddSingleton<IRenderer, Renderer>()
    .AddSingleton<IEventBus, EventBus>()
    .AddSingleton<IAssetService, AssetService>()
    .AddSingleton<IBufferManager, BufferManager>()
    .AddSingleton<IGeometryResourceManager, GeometryResourceManager>()
    .AddSingleton<IShaderResourceManager, ShaderResourceManager>()
    .AddSingleton<IResourceManager, ResourceManager>()
    .AddSingleton<IComponentFactory, ComponentFactory>()
    .AddSingleton<IContentManager, ContentManager>()
    .AddSingleton<IActionFactory, ActionFactory>()
    .AddPixelSampling()  // Testing only
    .AddDiscoveredServices<IRuntimeComponent>()
    .AddDiscoveredServices<IAction>()
    .BuildServiceProvider();
```

**CRITICAL**: Do not create InputContext-dependent components (KeyBinding, MouseBinding, InputMap) before window initialization. Pass the startup template to `application.Run()` instead.

### Performance Standards

- **Batch Rendering**: Renderer uses `IBatchStrategy` to group compatible render commands and minimize state changes
- **Frame-Based Synchronization**: Proper use of fences and semaphores via `ISyncManager`
- **Resource Caching**: Resources reused across components to minimize allocation overhead
- **Zero-Allocation Paths**: Hot paths (rendering loops) must avoid allocations where possible

## Testing Infrastructure

### Test Types

1. **Unit Tests**: Located in `Tests/` project
   - Test individual components in isolation
   - Use Moq for mocking dependencies
   - Run via `dotnet test Tests/Tests.csproj`

2. **Frame-Based Integration Tests**: Located in `TestApp/` project
   - Test rendering and component integration
   - Fire-and-forget pattern:
     - **Update** = Arrange (set up test state synchronously)
     - **Renderer** = Act (execute during render phase)
     - **PostRender middleware** = Assert (validate results)
   - Run via `dotnet run --project TestApp/TestApp.csproj`

### Pixel Sampling (Future)

VulkanPixelSampler service (currently stub) will enable reading rendered pixel colors for validation.
- Enable with `.AddPixelSampling()` in test configuration
- See `src/GameEngine/Testing/README.md` for details

**Rationale**: Multiple test levels ensure both unit-level correctness and end-to-end integration validation.

## Development Workflow

### Build Commands

```bash
dotnet build Nexus.GameEngine.sln --configuration Debug
dotnet test Tests/Tests.csproj  # Unit tests
dotnet run --project TestApp/TestApp.csproj # Integration tests
```

### Shader Compilation

Before building, compile shaders to SPIR-V:

```bash
cd src/GameEngine/Shaders
.\compile.bat  # Requires VulkanSDK with glslc compiler
```

The `build-testapp` task has a dependency on `compile-shaders` task.

### Pre-Implementation Checklist

Before starting any implementation:
- [ ] Solution builds without errors
- [ ] Relevant documentation updated
- [ ] Unit tests written and failing (Red phase)
- [ ] Implementation approach reviewed and approved
- [ ] Architecture constraints verified

### Post-Implementation Checklist

After completing implementation:
- [ ] Tests pass (Green phase)
- [ ] Solution rebuilds without warnings/errors
- [ ] Documentation reflects changes
- [ ] Summary provided with manual testing instructions

## Governance

This constitution supersedes all other development practices. All code changes, architectural decisions, and development processes MUST comply with these principles.

### Amendment Procedure

1. Propose amendment with clear rationale
2. Document impact on existing code and tests
3. Obtain explicit approval before implementation
4. Update constitution version according to semantic versioning:
   - **MAJOR**: Backward incompatible governance/principle removals or redefinitions
   - **MINOR**: New principle/section added or materially expanded guidance
   - **PATCH**: Clarifications, wording, typo fixes, non-semantic refinements
5. Update dependent templates and documentation

### Compliance Review

- All PRs MUST verify compliance with constitution principles
- Complexity deviations MUST be justified with clear rationale
- Use `.github/copilot-instructions.md` for runtime development guidance (inherits from this constitution)

### Template Alignment

All Spec-Kit templates (`.specify/templates/*.md`) MUST align with these principles:
- `spec-template.md`: Requirements must be testable and documented
- `plan-template.md`: Plans must follow TDD workflow and architecture constraints
- `tasks-template.md`: Tasks must include documentation, testing, and verification steps

**Version**: 1.0.0 | **Ratified**: 2025-11-02 | **Last Amended**: 2025-11-02
