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

**Running Tests with Output**: Use PowerShell terminal to capture test output:
```powershell
# Run all integration tests
dotnet run --project TestApp/TestApp.csproj --configuration Debug

# Run filtered integration tests (case-insensitive substring match)
dotnet run --project TestApp/TestApp.csproj --configuration Debug -- --filter=ColoredRect
dotnet run --project TestApp/TestApp.csproj --configuration Debug -- --filter=Lifecycle
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
- **ICamera**: Camera interface with viewport properties and UBO management
- **Viewport**: Immutable record containing Vulkan rendering state (extent, clear color, render pass mask)

**Resource Management**: 

- **IResourceManager**: Unified resource management interface
- **IGeometryResourceManager**: Vertex/index buffer management
- **IShaderResourceManager**: SPIR-V shader module management
- **IBufferManager**: Vulkan buffer allocation and lifecycle (vertex buffers, uniform buffers)

See `src/GameEngine/Graphics/` and `src/GameEngine/Resources/` for implementations.

**Testing Infrastructure**: Frame-based integration testing using TestApp. See `src/GameEngine/Testing/README.md` and `TestApp/` for details.

**Test Filtering**: TestApp supports filtering tests by name using the `--filter` argument:
- Tests are discovered by scanning for `[Test]` attributes on static Template fields
- Filter performs case-insensitive substring match on test names
- Useful for focusing on specific failing tests during debugging
- Example: `--filter=ColoredRect` runs only tests with "ColoredRect" in the name

### Key Architectural Principles

**Application Startup Pattern:**

```csharp
// Create services manually (no AddGameEngineServices helper exists)
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

**Property Attribute System**: Components use two separate attributes for different concerns:

- **`[TemplateProperty]`**: Marks fields for template generation only
  - Generates properties in auto-generated `{ClassName}Template` records
  - Assigned in `OnLoad()` method when component created from template
  - Optional `Name` parameter to customize template property name
  - Does NOT generate runtime public properties
  
- **`[ComponentProperty]`**: Marks fields for runtime behavior only
  - Generates public properties with deferred updates
  - Supports interpolation over time (specify Duration and InterpolationMode at call site)
  - PropertyChanged notifications and animation lifecycle events
  - Optional `Name` parameter to customize property name
  - Does NOT appear in templates unless also marked with `[TemplateProperty]`

- **Both attributes together**: Use when field needs both template configuration AND runtime behavior
  ```csharp
  [TemplateProperty]
  [ComponentProperty]
  private Vector2D<float> _anchorPoint = new(-1, -1);
  // Generates:
  //   - ElementTemplate.AnchorPoint property (settable from template)
  //   - Element.AnchorPoint runtime property (deferred updates, animations)
  //   - OnLoad assigns template value to _anchorPoint backing field
  ```

**Definition vs Resource Pattern**: Separate template-time definitions from runtime resources:
```csharp
// Template-only definition (configuration blueprint)
[TemplateProperty(Name = "Texture")]
private TextureDefinition? _textureDefinition = null;

// Runtime-only resource (GPU instance)
[ComponentProperty]
private TextureResource? _texture;

// OnActivate converts definition → resource
public override void OnActivate() {
    if (_textureDefinition != null)
        _texture = ResourceManager.Textures.GetOrCreate(_textureDefinition);
}
```

**Source Generator Architecture**: Three generators work together:
- **`TemplateGenerator`**: Processes `[TemplateProperty]` → generates template records and `OnLoad()` methods
- **`ComponentPropertyGenerator`**: Processes `[ComponentProperty]` → generates runtime properties with deferred updates
- **`AnimatedPropertyGenerator`**: Generates type-specific interpolation code at compile-time
  - **Zero runtime overhead**: No boxing, reflection, or dynamic dispatch
  - **Array animation support**: Element-by-element interpolation with inline loops
  - **Built-in type support**: float, double, int, Silk.NET vectors/matrices
  - **Extensibility**: Custom types implement `IInterpolatable<T>`

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

### Camera and Viewport System

**Camera Architecture**:
- `ICamera`: Interface for all cameras (StaticCamera, OrthoCamera, PerspectiveCamera)
- Cameras manage their own ViewProjection UBO (Uniform Buffer Object) lifecycle
- Each camera creates descriptor sets containing the ViewProjection matrix (64 bytes)
- UBO bound at descriptor set=0, binding=0 in shaders

**Camera Lifecycle**:
- **OnActivate()**: Initialize UBO buffer, descriptor set, and descriptor layout
- **SetViewportSize()**: Update projection matrix when viewport dimensions change
- **UpdateViewProjectionUBO()**: Write matrix data to UBO when it changes
- **OnDeactivate()**: Cleanup UBO resources (buffer, descriptor set, layout)
- **GetViewProjectionDescriptorSet()**: Return descriptor set for renderer binding

**ContentManager Camera Tracking**:
- `ContentManager.Initialize()` creates default StaticCamera (full screen, RenderPriority=100)
- `ActiveCameras` property provides sorted list of active cameras (by RenderPriority)
- Cameras discovered automatically during content Load/Unload
- Default camera always available if no other cameras exist

**Viewport Management**:
- `Viewport` is immutable record (Extent, ClearColor, RenderPassMask)
- Cameras create Viewport instances via `GetViewport()` method
- Renderer iterates cameras, gets viewports, renders in priority order

**Push Constants Optimization**:
- Old approach: 80 bytes per draw (64-byte matrix + 16-byte color)
- New approach: 16 bytes per draw (color only), matrix in UBO
- **99% bandwidth reduction** for matrix transmission (bound once per viewport vs per draw)

### Testing Infrastructure

**Integration Testing**: Frame-based test execution using fire-and-forget pattern:

- **Update** = Arrange (set up test state synchronously)
- **Renderer** = Act (execute during render phase)
- **PostRender middleware** = Assert (validate results)
  See `TestApp/Testing/` for implementation.

**Test Types**: Unit tests (Tests project), and frame-based integration tests (TestApp).

**Pixel Sampling**: VulkanPixelSampler service (currently a stub) will allow reading rendered pixel colors for validation. Enable with `.AddPixelSampling()` in test configuration. See `src/GameEngine/Testing/README.md` for details.

### Component Creation and Management

**Separation of Concerns**:
- **IComponentFactory**: Responsible for component instantiation via DI, configuration from templates
- **IContentManager**: Responsible for content lifecycle (caching, activation, updates, disposal)

**Component Creation Pattern**: Components use `ContentManager` to create children. ContentManager internally delegates to ComponentFactory for instantiation, then manages the created component's lifecycle:

```csharp
// Components always use ContentManager (never ComponentFactory directly)
public virtual IComponent? CreateChild(Template template)
{
    var component = ContentManager?.CreateInstance(template);
    if (component != null) AddChild(component);
    return component;
}
```

**Why Components Use ContentManager**: Child components need lifecycle management (caching, updates, disposal), not just creation. ContentManager provides this while internally delegating creation to ComponentFactory.
