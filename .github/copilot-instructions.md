DO NOT change any code without explicit instructions to do so.

This project follows a documentation-first test-driven-development approach. See Tests\README.md for comprehensive testing guidelines.

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

Use the .github/agent/ folder for temporary files:

- Work instructions or plans
- Checklists
- Temporary scripts
- Progress tracking
- Ouput files

use dotnet command for editing CSPROJ and SLN files.

All classes and interfaces should be defined in separate files.

To build, use the following command:

```bash
dotnet build Nexus.GameEngine.sln --configuration Debug
dotnet test
```

## Architecture Overview

### Content Management System

The engine uses a **ContentManager + Viewport** architecture for UI and content management:

- **ContentManager**: Manages reusable content trees (menus, screens, levels) with caching
- **Viewport**: Manages screen regions with cameras and content assignment
- **Renderer**: Renders viewport content trees via IRenderer.Viewport property

### Key Architectural Principles

**Application Startup Pattern:**

```csharp
// CORRECT: Use StartupTemplate for deferred component creation
var app = services.GetRequiredService<IApplication>();
app.StartupTemplate = Templates.MainMenu;  // Created after window initialization
await app.RunAsync();
```

**IMPORTANT**: Do not create InputContext-dependent components (KeyBinding, MouseBinding, InputMap) before calling `app.RunAsync()`. These components require the window to be initialized first. Use the StartupTemplate property instead of directly calling `contentManager.GetOrCreate()` in startup code.

The Application class automatically defers StartupTemplate instantiation until after the WindowLoaded event fires, ensuring the InputContext is available when input binding components are created.

**Content Creation Pattern (for runtime content changes):**

```csharp
// Use this pattern for content changes after application startup
var contentManager = services.GetRequiredService<IContentManager>();
var newContent = contentManager.GetOrCreate(Templates.SomeContent);

var renderer = services.GetRequiredService<IRenderer>();
renderer.Viewport.Content = newContent;
```

**RuntimeComponent System Design Summary**
Core Principle: Everything is an IRuntimeComponent that can contain other components via a Children property. Components implement only the behavior interfaces they need.

**Deferred Property Updates**

Components use lambda-based deferred updates to maintain temporal consistency during frame processing:

```csharp
public class PhysicsBody : RuntimeComponent, IPhysicsBody
{
    private Vector3D<float> _velocity;
    public Vector3D<float> Velocity
    {
        get => _velocity;
        private set  // Private setter preserves encapsulation
        {
            if (_velocity != value)
            {
                _velocity = value;
                OnPropertyChanged(); // Notifications still work
            }
        }
    }

    // Interface method queues deferred update
    public void ApplyForce(Vector3D<float> force)
    {
        QueueUpdate(() => Velocity += force); // Lambda calls private setter
    }
}
```

Key principles:

- Properties have `private set` to prevent external mutation during Update phase
- Interface methods (like `ApplyForce`) queue changes via `QueueUpdate(lambda)`
- `ApplyUpdates()` is called before rendering to execute all queued changes
- Lambda closures access component instance for property updates
- Change notifications fire normally when lambdas execute

Components implement only the behaviors they need:

**Declarative UI Syntax for Components**

```csharp
public static partial class Templates
{
    public static readonly RuntimeComponent.Template MainMenu = new()
    {
        Name = "MainMenu",
        Subcomponents =
        [
            new BackgroundLayer.Template()
            {
                Name = "BackgroundLayer",
                BackgroundColor = Colors.CornflowerBlue
            },
            new TextElement.Template()
            {
                Name = "TextElement",
                Color = Colors.DarkSlateBlue,
                Text = "Nexus Game Engine Test App"
            },
            new KeyBinding.Template()
            {
                Key = Key.F12,
                ActionId = ActionId.FromType<ToggleFullscreenAction>()
            },
            new KeyBinding.Template()
            {
                Key = Key.Escape,
                ActionId = ActionId.FromType<QuitGameAction>()
            }
        ]
    };
}
```

**Logging**:

All logging should be structured using ILogger.

For RuntimeComponents, ComponentFactory sets the Logger when a component is created. The logger should not be injected. Logging via RuntimeComponent looks like this:

    Logger?.LogDebug("Input context not available {ComponentType}", GetType().Name);

For non-RuntimeComponent services, inject ILoggerFactory to create a logger with the proper context:

    private readonly ILogger _logger = loggerFactory.CreateLogger(nameof(Service));
