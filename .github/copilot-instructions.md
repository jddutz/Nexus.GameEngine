DO NOT change any code without explicit instructions to do so.

This project follows a documentation-first test-driven-development approach. See `Tests\README.md` for comprehensive testing guidelines.

## Development Workflow

Before updating any code:

1. Build the solution and verify that the build succeeds without any errors
   - If there are any errors, confirm:
     - does the code need to be fixed, or
     - do the tests need to be revised
   - Fix the errors before proceeding
2. Update any documentation related to the change:
   - `.docs\Project Structure.md`
   - `.docs\Silk dotNET Setup.md`
   - `README.md`
3. Generate unit tests to cover all new behaviors
4. (Red Phase) Confirm that the new tests fail as expected
5. Update the code to implement the changes
6. (Green Phase) Confirm that the tests now pass
7. Rebuild the project and address any warnings and errors
8. Provide a summary (via chat) of the changes made and instructions for manual testing required

Use the `.github/agent/` folder for temporary files:

- Work instructions or plans
- Checklists
- Temporary scripts
- Progress tracking
- Output files

Use dotnet command for editing CSPROJ and SLN files.
All classes and interfaces should be defined in separate files.

## Build Commands

```bash
dotnet build Nexus.GameEngine.sln --configuration Debug
dotnet test  # Regular unit tests
dotnet run --project TestApp # Integration tests
```

## Architecture Overview

### Core Systems

**Component-Based Architecture**: Everything is an `IRuntimeComponent` that can contain other components via a `Children` property. Components implement only the behavior interfaces they need.

**Viewport + Renderer System**:

- **Viewport**: Manages screen regions with cameras and content assignment
- **Renderer**: Renders viewport content trees with efficient batching and state management
- **Middleware**: Extensible pre/post render hooks for debugging and state capture

**Resource Management**: Attribute-based system for OpenGL resources with automatic lifecycle management. See `GameEngine/Graphics/Resources/` for implementations.

**Testing Infrastructure**: Comprehensive unit and integration testing with OpenGL context management. See `Tests/README.md` for details.

### Key Architectural Principles

**Application Startup Pattern:**

```csharp
// CORRECT: Use StartupTemplate for deferred component creation
var app = services.GetRequiredService<IApplication>();
app.StartupTemplate = Templates.MainMenu;  // Created after window initialization
await app.RunAsync();
```

**IMPORTANT**: Do not create InputContext-dependent components (KeyBinding, MouseBinding, InputMap) before calling `app.RunAsync()`. Use the StartupTemplate property instead of directly calling content managers in startup code.

**Runtime Content Changes:**

```csharp
// Use renderer viewport for content changes after startup
var renderer = services.GetRequiredService<IRenderer>();
renderer.Viewport.Content = newContent;
```

### RuntimeComponent System

**Deferred Property Updates**: Components use lambda-based deferred updates via `QueueUpdate()` to maintain temporal consistency during frame processing. Properties have `private set` and interface methods queue changes for execution at frame boundaries.

**Declarative Templates**: Components use strongly-typed template records for configuration. See `TestApp/MainMenu.cs` for example declarative syntax.

**Lifecycle Management**: Components support activation, validation, updating, and disposal with automatic child propagation.

### Resource Management System

**Attribute-Based Resources**: Shared resources declared using `[SharedResource]` attributes on static definitions. See `GameEngine/Graphics/Resources/` for implementation.

**Type-Safe Access**: Components access resources through `IResourceManager.GetOrCreateResource<T>()` with compile-time checking.

**Automatic Lifecycle**: Resources are automatically created, cached, and cleaned up based on component scope.

### Graphics and Rendering

**Batch Rendering**: Renderer uses `IBatchStrategy` to minimize OpenGL state changes by grouping compatible render states.

**Middleware System**: Extensible `IRenderMiddleware` for debugging, profiling, and state capture. See `GameEngine/Graphics/Middleware/`.

**OpenGL State Management**: Renderer queries actual GL state before updates to avoid redundant API calls.

### Testing Infrastructure

**OpenGL Testing**: Shared context with automatic state reset between tests. Use `[Collection("OpenGL")]` and inherit from `OpenGLTestBase`.

**Integration Testing**: Frame-based test execution using fire-and-forget pattern:

- **Update** = Arrange (set up test state synchronously)
- **Renderer** = Act (execute during render phase)
- **PostRender middleware** = Assert (validate results)
  See `TestApp/Testing/` for implementation.

**Test Types**: Unit tests (Tests project), OpenGL integration tests (OpenGLTests project), and frame-based integration tests (TestApp).

### Logging

For RuntimeComponents, ComponentFactory sets the Logger - do not inject:

```csharp
Logger?.LogDebug("Message {ComponentType}", GetType().Name);
```

For services, inject ILoggerFactory:

```csharp
private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(Service));
```
