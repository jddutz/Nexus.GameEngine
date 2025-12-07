DO NOT change any code without explicit instructions to do so. If not sure, ask first.

This project follows a documentation-first test-driven-development approach. See `src/GameEngine/Testing/README.md` for comprehensive testing guidelines.

## Development Workflow

Standard TDD workflow:
1. Build solution and verify clean build
2. Update relevant documentation (`.docs/`, `README.md`, test docs)
3. Write failing unit tests (Red phase)
4. Implement changes to make tests pass (Green phase)
5. Rebuild and address warnings/errors
6. Provide summary with manual testing instructions

Use `.temp/agent/` for temporary work files. Use dotnet CLI for CSPROJ/SLN edits. Separate files for each class/interface.

## Build Commands

```bash
dotnet build Nexus.GameEngine.sln --configuration Debug
dotnet test Tests/Tests.csproj
dotnet run --project TestApp/TestApp.csproj -- --filter=TestName  # Optional filter
```

## Shader Compilation

Shaders must be compiled to SPIR-V before building. The build-testapp task has a dependency on compile-shaders task.

```bash
cd src/GameEngine/Shaders
.\compile.bat  # Requires VulkanSDK with glslc
```

## Architecture Overview

### Core Systems

**Systems Pattern**: Framework services are exposed via marker interfaces (`IGraphicsSystem`, `IResourceSystem`, `IContentSystem`, `IWindowSystem`, `IInputSystem`) with functionality provided by extension methods.
- **Access**: Framework classes access systems via properties (e.g., `this.Graphics`, `this.Resources`).
- **No Constructor Injection**: Do NOT inject internal managers (e.g., `IPipelineManager`) into framework classes. Use the System properties instead.
- **No Service Locator**: Do NOT use `GetRequiredService<T>()` in framework classes.

**Component-Based Architecture**: Everything is an `IRuntimeComponent` that can contain other components via a `Children` property. Components implement only the behavior interfaces they need.

**Vulkan Rendering**: Core graphics infrastructure. See `src/GameEngine/Graphics/` for implementations. Accessed via `IGraphicsSystem`.

**Resource Management**: Unified resource management via `IResourceSystem`. See `src/GameEngine/Resources/` for implementations.

**Testing Infrastructure**: Frame-based integration testing using TestApp. See `src/GameEngine/Testing/README.md` for details. Tests support `--filter` argument for case-insensitive name filtering.

### Key Architectural Principles

**Application Startup**: Services registered manually via ServiceCollection. Pass startup template to `application.Run(windowOptions, template)`. Do NOT create Window-dependent components (KeyBinding, MouseBinding, InputMap) before window initialization.

### RuntimeComponent System

**Property Attribute System**: 

 `[TemplateProperty]`: Template configuration only, Template record properties, assigned in `OnLoad()`
 `[ComponentProperty]`: Runtime behavior only, Public properties with deferred updates, interpolation, events
 Both together: Template + runtime, Both template properties AND runtime properties |

**Definition vs Resource Pattern**: Separate template-time definitions (`[TemplateProperty]`) from runtime resources (`[ComponentProperty]`). Convert in `OnActivate()` via ResourceManager.

**Source Generators**: 
- `TemplateGenerator`: `[TemplateProperty]` → template records + `OnLoad()`
- `ComponentPropertyGenerator`: `[ComponentProperty]` → runtime properties with deferred updates
- `AnimatedPropertyGenerator`: Compile-time type-specific interpolation (zero overhead, array support)

See `.docs/Deferred Property Generation System.md` for details.

### Resource Management System

Resources are cached and reused. IResourceManager provides centralized access; IBufferManager handles low-level Vulkan allocation. See `src/GameEngine/Resources/`.

### Graphics and Rendering

Renderer uses `IBatchStrategy` to group compatible render commands and minimize state changes. Pipelines created via fluent `IPipelineBuilder` API and cached by `IPipelineManager`.

### Camera and Viewport System

**Camera Architecture**: `ICamera` implementations (StaticCamera, OrthoCamera, PerspectiveCamera) manage their own ViewProjection UBO lifecycle. Each camera creates descriptor sets (64-byte matrix) bound at descriptor set=0, binding=0 in shaders.

**ContentManager Tracking**: `ContentManager.Initialize()` creates default StaticCamera. `ActiveCameras` property provides sorted list by RenderPriority. Cameras discovered automatically during Load/Unload.

**Viewport Management**: Cameras create immutable `Viewport` records (Extent, ClearColor, RenderPassMask) via `GetViewport()`. Renderer iterates cameras in priority order.

### Layout System

**Directional Layout Components**: `VerticalLayout` and `HorizontalLayout` extend `Container` to provide automatic child positioning along a single axis.

**Layout Properties**:
- `ItemHeight` / `ItemWidth` (uint?): Fixed size for child elements (null = use child's natural size)
- `ItemSpacing` (uint?): Gap between adjacent children (null = no spacing)
- `Spacing` (SpacingMode): Distribution strategy - `Stacked` (default, stack directly), `Justified` (space-between), `Distributed` (space-evenly)
- `AlignContent` (float): Cross-axis alignment from -1.0 (top/left) to 1.0 (bottom/right), default 0.0 (center)

**SpacingMode Behavior**:
- `Stacked` (0): Children positioned sequentially with ItemSpacing gap, aligned using AlignContent
- `Justified` (1): First child at start, last at end, remaining space distributed evenly between
- `Distributed` (2): Equal spacing before, between, and after all children

**Zero-Size Handling**: Children with zero height (VerticalLayout) or zero width (HorizontalLayout) are excluded from layout calculations to prevent spacing artifacts.

**Edge Cases**: Single-child layouts with Justified/Distributed modes delegate to Stacked behavior using AlignContent for positioning.

**Template Usage**:
```csharp
new VerticalLayoutTemplate()
{
    ItemHeight = 50,        // Fixed 50px height per item
    ItemSpacing = 10,       // 10px gap between items
    Spacing = SpacingMode.Stacked,
    AlignContent = -1.0f,   // Align to top
    Subcomponents = [ /* children */ ]
}
```

### Testing Infrastructure

**Unit Testing**: per-class test framework using xUnit

Tests are executed from src\Tests\GameEngine. The Tests/GameEngine directory and file structure mirrors src/GameEngine. Specific tests for each class are defined in separate cs files using naming conventions: {ClassName}.Tests.cs.

Only the System Under Test should be instatiated using concrete implementations. All dependencies should be mocked using Moq. Methods used to create mocks must be defined in the test class itself. Do not create custom test classes.

Code coverage target is 80% for all types.

**Integration Testing**: Frame-based test execution using fire-and-forget pattern:

- **Update** = Arrange (set up test state synchronously)
- **Renderer** = Act (execute during render phase)
- **PostRender middleware** = Assert (validate results)
  See `TestApp/Testing/` for implementation.

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

### Property Binding System

**Declarative Bindings**: Use `PropertyBindings` in templates to link properties between components.
- **One-Way**: `Bindings = { TargetProp = Binding.FromParent<SourceType>(s => s.SourceProp) }`
- **Two-Way**: `Bindings = { TargetProp = Binding.TwoWay<SourceType>(s => s.SourceProp) }`
- **Converters**: `.WithConverter(new StringFormatConverter("Health: {0:F0}"))`

**Lifecycle Integration**:
- Bindings are configured in `OnLoad` via template.
- Bindings are **activated** in `OnActivate` (subscriptions created).
- Bindings are **deactivated** in `OnDeactivate` (subscriptions removed).
- **Memory Safety**: Automatic cleanup prevents leaks.

**Source Generators**:
- `PropertyBindingsGenerator`: Creates `{Component}PropertyBindings` class.
- `ComponentPropertyGenerator`: Adds `NotifyChange` support and `PropertyChanged` events.

