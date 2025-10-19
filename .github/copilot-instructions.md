DO NOT change any code without explicit instructions to do so. If not sure, ask first.

This project follows a documentation-first test-driven-development approach. See `src/GameEngine/Testing/README.md` for comprehensive testing guidelines.

## Development Workflow

Before updating any code:

1. Build the solution and verify that the build succeeds without any errors
   - If there are any errors, confirm:
     - does the code need to be fixed, or
     - do the tests need to be revised
   - Fix the errors before proceeding
2. Update any documentation related to the change:
   - `.docs/Project Structure.md`
   - `.docs/Vulkan Architecture.md`
   - `.docs/Deferred Property Generation System.md`
   - `README.md`
   - `src/GameEngine/Testing/README.md`
3. Generate unit tests to cover all new behaviors
4. (Red Phase) Confirm that the new tests fail as expected
5. Update the code to implement the changes
6. (Green Phase) Confirm that the tests now pass
7. Rebuild the project and address any warnings and errors
8. Provide a summary (via chat) of the changes made and instructions for manual testing required

Use the `.temp/agent` folder for temporary files:

- Work instructions or plans
- Checklists
- Temporary scripts
- Progress tracking
- Output files

Use dotnet commands for editing CSPROJ and SLN files.
All classes and interfaces should be defined in separate files.

## Build Commands

```bash
dotnet build Nexus.GameEngine.sln --configuration Debug
dotnet test Tests/Tests.csproj  # Unit tests
dotnet run --project TestApp/TestApp.csproj # Integration tests (frame-based testing)
```

## Shader Compilation

Shaders must be compiled to SPIR-V before building:

```bash
cd src/GameEngine/Shaders
.\compile.bat  # Requires VulkanSDK with glslc compiler
```

The build-testapp task has a dependency on compile-shaders task.

## Architecture Overview

### Core Systems

**Component-Based Architecture**: Everything is an `IRuntimeComponent` that can contain other components via a `Children` property. Components implement only the behavior interfaces they need.

**Vulkan Rendering System**:

- **IGraphicsContext**: Core Vulkan infrastructure (instance, device, queues)
- **ISwapChain**: Swap chain management for presentation
- **IPipelineManager**: Graphics pipeline creation and caching with fluent builder API
- **ICommandPoolManager**: Command buffer allocation and management
- **ISyncManager**: Synchronization primitives (semaphores, fences)
- **IDescriptorManager**: Descriptor pool, layout, and set management for binding resources (UBOs, textures) to shaders
- **IRenderer**: High-level rendering orchestration
- **IViewport**: Screen region and content management

**Resource Management**: 

- **IResourceManager**: Unified resource management interface
- **IGeometryResourceManager**: Vertex/index buffer management
- **IShaderResourceManager**: SPIR-V shader module management
- **IBufferManager**: Vulkan buffer allocation and lifecycle (vertex buffers, uniform buffers)

See `src/GameEngine/Graphics/` and `src/GameEngine/Resources/` for implementations.

**Testing Infrastructure**: Frame-based integration testing using TestApp. See `src/GameEngine/Testing/README.md` and `TestApp/` for details.

### Key Architectural Principles

**Application Startup Pattern:**

```csharp
// Create services manually (no AddGameEngineServices helper exists)
var services = new ServiceCollection()
    .AddSingleton<ILoggerFactory>(loggerFactory)
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
    .AddSingleton<IContentManager, ContentManager>()
    .AddSingleton<IComponentFactory, ComponentFactory>()
    .AddSingleton<IActionFactory, ActionFactory>()
    .AddPixelSampling()  // Testing only
    .AddDiscoveredServices<IRuntimeComponent>()
    .AddDiscoveredServices<IAction>()
    .BuildServiceProvider();

// Create and run application with startup template
var application = new Application(services);
var windowOptions = WindowOptions.DefaultVulkan with
{
    Size = new Vector2D<int>(1920, 1080),
    Title = "My App",
    VSync = true
};
application.Run(windowOptions, Templates.MainMenu);
```

**IMPORTANT**: Do not create InputContext-dependent components (KeyBinding, MouseBinding, InputMap) before the window is initialized. Pass the startup template to `application.Run()` instead.

### RuntimeComponent System

**Animated Property System**: Components use source-generated animated properties. All properties marked with `[ComponentProperty]` automatically support:
- Deferred updates (changes apply during `UpdateAnimations()` call)
- Optional interpolation over time (specify Duration and InterpolationMode)
- PropertyChanged notifications
- Animation lifecycle events (AnimationStarted, AnimationEnded)

**Source Generator Architecture**: The `AnimatedPropertyGenerator` (in `src/GameEngine.SourceGenerators/`) generates optimized code at compile-time:
- **Zero runtime overhead**: Type-specific interpolation code (no boxing, no reflection, no dynamic dispatch)
- **Array animation support**: Animated arrays interpolate each element individually with inline loops
- **Built-in type support**: float, double, int, Silk.NET vectors/matrices automatically supported
- **Extensibility**: Custom types can implement `IInterpolatable<T>` for specialized interpolation

Example:
```csharp
// Developer writes:
[ComponentProperty(Duration = 0.4f, Interpolation = InterpolationMode.Linear)]
private Vector4D<float>[] _vertexColors = [...];

// Generator produces optimized per-element interpolation:
for (int i = 0; i < newArray.Length; i++)
    newArray[i] = startValue[i] + (endValue[i] - startValue[i]) * t;
```

See `.docs/Deferred Property Generation System.md` for complete details.

**Legacy Deferred Updates**: `QueueUpdate()` method still exists for complex multi-property updates but should be rarely needed. Most properties now use auto-generated animated properties instead.

**Declarative Templates**: Components use strongly-typed template records for configuration. See `TestApp/MainMenu.cs` for example declarative syntax.

**Lifecycle Management**: Components support activation, validation, updating, and disposal with automatic child propagation.

### Resource Management System

**Unified Resource Management**: 
- `IResourceManager` provides centralized access to all resource types
- `IGeometryResourceManager` handles Vulkan vertex/index buffers
- `IShaderResourceManager` manages SPIR-V shader modules
- `IBufferManager` provides low-level Vulkan buffer allocation

**Resource Lifecycle**: Resources are cached and reused across components. Disposal is handled by the resource managers.

See `src/GameEngine/Resources/` for implementations.

### Graphics and Rendering

**Pipeline Management**: 
- Fluent builder API for creating Vulkan graphics pipelines (`IPipelineBuilder`)
- Pipeline caching and reuse via `IPipelineManager`
- Support for multiple render passes (opaque, transparent, UI, etc.)

**Command Buffer Management**:
- Pool-based command buffer allocation via `ICommandPoolManager`
- Automatic command buffer recording and submission

**Synchronization**:
- Frame-based synchronization with fences and semaphores via `ISyncManager`
- Proper image acquisition and presentation coordination

**Batch Rendering**: Renderer uses `IBatchStrategy` to group compatible render commands and minimize state changes.

### Testing Infrastructure

**Integration Testing**: Frame-based test execution using fire-and-forget pattern:

- **Update** = Arrange (set up test state synchronously)
- **Renderer** = Act (execute during render phase)
- **PostRender middleware** = Assert (validate results)
  See `TestApp/Testing/` for implementation.

**Test Types**: Unit tests (Tests project), and frame-based integration tests (TestApp).

**Pixel Sampling**: VulkanPixelSampler service (currently a stub) will allow reading rendered pixel colors for validation. Enable with `.AddPixelSampling()` in test configuration. See `src/GameEngine/Testing/README.md` for details.

### Logging

For RuntimeComponents, ComponentFactory sets the Logger - do not inject:

```csharp
Logger?.LogDebug("Message {ComponentType}", GetType().Name);
```

For services, inject ILoggerFactory:

```csharp
private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(Service));
```
